using MongoDB.Bson;
using MongoDB.Driver;
using MongoObject.Core.Interfaces;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MongoObject.Core.Data
{
    public abstract class TrackingObservableObject : INotifyPropertyChanged, IDocumentFileInternal, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Dictionary<string, object?> _changes = [];
        private readonly Dictionary<string, BsonValue> _potentialChanges = [];

        protected string ParentName { get; set; } = string.Empty;
        protected bool Tracking { get; set; }

        protected virtual void OnPropertyChanged(object? value, [CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new MongoChangeEventArgs(propertyName, value));
        }

        protected void RegisterPossibleChange<T>(ref T? property, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName is not null && property is not TrackingObservableObject)
            {
                _potentialChanges.TryAdd(propertyName, GenerateBsonSnapshot(property));
            }
        }

        protected bool SetField<T>(ref T field, T value, string queryName, bool notify = true, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            if (field is IDisposable disposable) disposable.Dispose();
            field = value;
            if (value is TrackingObservableObject observable)
            {
                observable.ParentName = propertyName ?? string.Empty;
                observable.TrackChanges(this, Tracking, queryName ?? string.Empty);
                notify = false;
            }
            
            if (notify)
            {
                OnPropertyChanged(value, $"{(string.IsNullOrEmpty(ParentName) ? string.Empty : ParentName + ".")}{queryName}");
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

        public abstract void TrackChanges(TrackingObservableObject observable, bool isTracking, string parentName);

        public void ClearChanges()
        {
            _changes.Clear();
        }

        protected void ProcessPossibleChanges()
        {
            IEnumerable<PropertyInfo> properties = GetType()?.GetProperties().Where(x => x.PropertyType.IsPublic && x.Name != "ParentName") ?? [];

            foreach (var property in properties)
            {
                var field = property.GetValue(this);
                if (field is TrackingObservableObject tracker)
                {
                    tracker.ProcessPossibleChanges();
                    continue; // Move to the next property
                }

                if (_potentialChanges.TryGetValue(property.Name, out var oldValue))
                {
                    //var field = property.GetValue(this);
                    var newValue = GenerateBsonSnapshot(field);
                    if (newValue != oldValue)
                    {
                        OnPropertyChanged(field, $"{(string.IsNullOrEmpty(ParentName) ? string.Empty : ParentName + ".")}{property.Name}");
                    }
                }
            }
        }

        private BsonValue GenerateBsonSnapshot(object? field)
        {
            if (field == null) return BsonNull.Value;

            if (BsonTypeMapper.TryMapToBsonValue(field, out var mappedValue))
            {
                return mappedValue;
            }

            if (field is IEnumerable enumerableValues && field is not IDictionary)
            {
                var wrapper = new { Items = enumerableValues };
                return wrapper.ToBsonDocument()["Items"].AsBsonArray;
            }

            return field.ToBsonDocument();
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

            var stages = new List<IPipelineStageDefinition>();

            if (setFields.Any())
            {
                stages.Add(new BsonDocumentPipelineStageDefinition<MongoDocument<T>, MongoDocument<T>>(
                    new BsonDocument("$set", setFields)
                ));
            }

            if (unsetFields.Count > 0)
            {
                stages.Add(new BsonDocumentPipelineStageDefinition<MongoDocument<T>, MongoDocument<T>>(
                    new BsonDocument("$unset", unsetFields)
                ));
            }

            var metadataStage = new BsonDocument("$set", new BsonDocument
            {
                { "Metadata.LastModifiedAt", "$$NOW" },
                { "Metadata.Version", new BsonDocument("$add", new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$Metadata.Version", 0 }),
                        1
                    })
                }
            });

            stages.Add(new BsonDocumentPipelineStageDefinition<MongoDocument<T>, MongoDocument<T>>(metadataStage));

            update = Builders<MongoDocument<T>>.Update.Pipeline(
                new PipelineStagePipelineDefinition<MongoDocument<T>, MongoDocument<T>>(stages)
                );


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

        public bool TryGetPendingUpdates<T>(out UpdateDefinition<MongoDocument<T>>? update) where T : class, IDocumentFile, new()
        {
            ProcessPossibleChanges();

            var builder = Builders<MongoDocument<T>>.Update;
            var updates = new List<UpdateDefinition<MongoDocument<T>>>();

            foreach (var change in _changes)
            {
                string targetPath = $"Document.{change.Key}";

                if (change.Value is null)
                {
                    updates.Add(builder.Unset(targetPath));
                    continue;
                }

                updates.Add(builder.Set(targetPath, change.Value));
            }

            if (!updates.Any())
            {
                update = null;
                return false;
            }
            updates.Add(builder.Set("Metadata.LastModifiedAt", DateTime.UtcNow));

            updates.Add(builder.Inc("Metadata.Version", 1));

            update = builder.Combine(updates);
            return true;
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
