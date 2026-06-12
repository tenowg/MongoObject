using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Interfaces;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MongoObject.Core.Data
{
    public abstract class TrackingObservableObject : INotifyPropertyChanged, IDocumentFileInternal, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Dictionary<string, object?> _changes = [];
        private readonly Dictionary<string, BsonDocument> _potentialChanges = [];

        protected string ParentName { get; set; } = string.Empty;
        protected bool Tracking { get; set; }

        protected virtual void OnPropertyChanged(object? value, [CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new MongoChangeEventArgs(propertyName, value));
        }

        protected void RegisterPossibleChange<T>(ref T? property, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName is not null && property is not TrackingObservableObject observable)
            {
                if (property is IEnumerable enumerableValues && property is not IDictionary)
                {
                    var bson = new BsonDocument();
                    _potentialChanges.TryAdd(propertyName, bson.Add(propertyName, new BsonArray(enumerableValues)));
                }
                else
                {
                    _potentialChanges.TryAdd(propertyName!, property.ToBsonDocument());
                }
            }
        }

        protected bool SetField<T>(ref T field, T value, bool notify = true, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            if (field is IDisposable disposable) disposable.Dispose();
            field = value;
            if (value is TrackingObservableObject observable)
            {
                observable.ParentName = propertyName ?? string.Empty;
                observable.TrackChanges(this, Tracking, propertyName ?? string.Empty);
                notify = false;
            }
            
            if (notify)
            {
                OnPropertyChanged(value, $"{(string.IsNullOrEmpty(ParentName) ? string.Empty : ParentName + ".")}{propertyName}");
            }
            return true;
        }

        public void SetTracking(bool tracking)
        {
            this.Tracking = tracking;
        }

        public void Test_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e is not MongoChangeEventArgs mongoEvent || e.PropertyName == null) return;

            if (ParentName == string.Empty)
            {
                Console.WriteLine("Changed at root: " + mongoEvent.PropertyName + " to " + mongoEvent.Value?.ToString());
                // this should never happen, but just in case
                if (mongoEvent.PropertyName != null)
                {
                    _changes[mongoEvent.PropertyName] = mongoEvent.Value;
                }
            }
            else
            {
                OnPropertyChanged(mongoEvent.Value, $"{(string.IsNullOrEmpty(ParentName) ? string.Empty : ParentName + ".")}{e.PropertyName}");
            }
        }

        public void TrackChanges()
        {
            PropertyChanged -= Test_PropertyChanged;
            PropertyChanged += Test_PropertyChanged;

            Tracking = true;
        }

        // this will be done with codegen eventually so everything here not going to be used, and this method will be abstract
        public abstract void TrackChanges(TrackingObservableObject observable, bool isTracking, string parentName);
        //{
        //    ParentName = parentName;
        //    // this shouldn't required as this will only be called when a NEW trackable object is placed on a trackable property
        //    PropertyChanged -= observable.Test_PropertyChanged;
        //    PropertyChanged += observable.Test_PropertyChanged;

        //    // tracking is only called after init, so any new objects will be need to be tracked
        //    if (isTracking)
        //    {
        //        // this will be done with codegen eventually so everything here not going to be used
        //        var properties = GetType().GetProperties().Where(x => x.PropertyType.IsPublic && x.Name != "ParentName");
        //        Tracking = true;

        //        foreach(var property in properties)
        //        {
        //            if (!typeof(TrackingObservableObject).IsAssignableFrom(property.PropertyType))
        //            {
        //                var value = property.GetValue(this);
        //                if (value != null)
        //                    OnPropertyChanged(value, $"{ParentName}.{property.Name}");
        //            }
        //            if (typeof(TrackingObservableObject).IsAssignableFrom(property.PropertyType))
        //            {
        //                var value = property.GetValue(this);
        //                if (value is TrackingObservableObject tracker) tracker.TrackChanges(this, this.Tracking, property.Name);
        //            }
        //        }
        //    }
        //}

        public void ClearChanges()
        {
            _changes.Clear();
        }

        protected void ProcessPossibleChanges()
        {
            IEnumerable<PropertyInfo> properties = GetType()?.GetProperties().Where(x => x.PropertyType.IsPublic && x.Name != "ParentName") ?? [];

            foreach (var property in properties)
            {
                if (typeof(TrackingObservableObject).IsAssignableFrom(property.PropertyType))
                {
                    var value = property.GetValue(this);
                    if (value is TrackingObservableObject tracker) tracker.ProcessPossibleChanges();
                }
                else
                {
                    if (_potentialChanges.TryGetValue(property.Name, out var value))
                    {
                        var field = property.GetValue(this);
                        BsonDocument bson = new();
                        if (field is IEnumerable enumerableValues && field is not IDictionary)
                        {
                            bson = bson.Add(property.Name, new BsonArray(enumerableValues));
                        }
                        else
                        {
                            bson = field.ToBsonDocument();
                        }
                            
                        if (bson != value)
                        {
                            OnPropertyChanged(field, $"{(string.IsNullOrEmpty(ParentName) ? string.Empty : ParentName + ".")}{property.Name}");
                        }
                    }
                }
            }
        }

        public bool TryGetPendingUpdatesPipeline<T>(out UpdateDefinition<MongoDocument<T>>? update) where T : class, IDocumentFile, new()
        {
            ProcessPossibleChanges();
            var setFields = new BsonDocument();
            var unsetFields = new BsonArray();

            foreach (var change in _changes)
            {
                string targetPath = $"Document.{change.Key}";

                if (change.Value is null)
                {
                    setFields.Remove(targetPath);
                    unsetFields.Add(targetPath);
                }
                else
                {
                    unsetFields.Remove(targetPath);
                    if (change.Value is BsonValue bsonValue)
                    {
                        setFields[targetPath] = bsonValue;
                    }
                    else if (change.Value.GetType().IsPrimitive || change.Value is string || change.Value is Guid || change.Value is DateTime || change.Value is decimal)
                    {
                        setFields[targetPath] = BsonValue.Create(change.Value);
                    }
                    else if (change.Value is IDictionary dictValue)
                    {
                        setFields[targetPath] = BsonValue.Create(dictValue);
                    }
                    else if (change.Value is IEnumerable enumerableValues && change.Value is not IDictionary)
                    {
                        setFields[targetPath] = new BsonArray(enumerableValues);
                    }
                    else
                    {
                        setFields[targetPath] = change.Value.ToBsonDocument();
                    }
                }
            }

            if (!setFields.Any())
            {
                update = null;
                return false;
            }

            setFields["Metadata.LastModifiedAt"] = "$$NOW";
            setFields["Metadata.Version"] = new BsonDocument("$add", new BsonArray
            {
                new BsonDocument("$ifNull", new BsonArray { "$Metadata.Version", 0 }), 1
            });

            var stages = new List<BsonDocument>();

            if (setFields.Any())
            {
                stages.Add(new BsonDocument("$set", setFields));
            }

            if (unsetFields.Count > 0)
            {
                stages.Add(new BsonDocument("$unset", unsetFields));
            }

            var pipeline = PipelineDefinition<MongoDocument<T>, MongoDocument<T>>.Create(stages);
            update = Builders<MongoDocument<T>>.Update.Pipeline(pipeline);
            return true;
        }


    public UpdateDefinition<MongoDocument<T>> GetPendingUpdates<T>() where T : class, IDocumentFile, new()
        {
            {
                var builder = Builders<MongoDocument<T>>.Update;
                var updates = new List<UpdateDefinition<MongoDocument<T>>>();

                foreach (var change in _changes)
                {
                    {
                        if (change.Value is null)
                        {
                            {
                                updates.Add(builder.Unset($"Document.{ change.Key}"));
                                continue;
                            }
                        }

                        updates.Add(builder.Set($"Document.{change.Key}", change.Value));
                    }
                }

                return builder.Combine(updates);
            }
        }

        public void Dispose()
        {
            PropertyChanged -= Test_PropertyChanged;
            PropertyChanged = null;

            GC.SuppressFinalize(this);
        }
    }

    public class MongoChangeEventArgs(string propertyName, object? value) : PropertyChangedEventArgs(propertyName)
    {
        public object? Value { get; } = value;
    }
}
