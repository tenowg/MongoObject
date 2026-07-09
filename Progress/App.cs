using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using Progress.Test;

namespace Progress
{
    public class App(IDocumentMonitor<AObject> monitor) : IDisposable
    {
        //IMongoLockScope? lockMeta;

        public void Dispose()
        {
            //lockMeta?.DisposeAsync();
        }

        public async Task Run()
        {
            Console.WriteLine("Hello, World!");

            await monitor.Add(new AObject
            {
                Name = "Case",
                Age = 16
            }, null);

            //try
            //{
            //    await monitor.AddBuilder(new BObject() { Name = "Andrew" });
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
            //await monitor.Add(new AObject {Name = "Craig is a cat that likes to eat biskits in the morning, along with himself", Age = 4}, f => f.OwnerId = "123.abc");

            //var collection = client.GetDatabase("BObjectsDatabase").GetCollection<MongoDocument<BObject>>("BObject");
            //try
            //{
            //    collection.InsertOne(new MongoDocument<BObject> { Document = new BObject { Name = "John " }, Metadata = new() });
            //} catch(Exception ex)
            //{
            //    var t = 5;
            //}
            #region QueryExample
            var nameProjection = await monitor.Search()
                .WithQuery(f =>
                {
                    f.Name = "Case";
                    f.Age = f.Age.And(
                        f.Age.Lt(40000),
                        f.Age.Gt(5)
                    );
                })
                .WithMeta(meta =>
                {
                    meta.LastModifiedAt = meta.LastModifiedAt.Lt(DateTime.UtcNow);
                });
            //.WithNameProjection();
            //.WithVectorTestVector()
            //.WithEmbedding("Craig is eating biskits in the morning, I wonder if he is a cat")
            //.WithMaxReturns(5);
            //.WithListTestProjection()
            //.WithListTestSlice(5, 3)
            //.WithLimit(5);
            #endregion
            //var t = nameProjection.FirstOrDefault();
            //t.PropertyChanged += (sender, e) =>
            //{
            //    Console.WriteLine($"{e.PropertyName}");
            //};
            //t.Name = "John";

            // try
            // {
            //     var result = await monitor.SaveChanges(t);
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine(ex.ToString());
            // }
            //var vectorProjection = await monitor
            //    .Search()
            //    .WithVectorTestVector()
            //    .WithMaxConsider(100)
            //    .WithMaxReturns(10);

            //await Task.Delay(500);
            //Console.WriteLine("Commensing Change");
            //            var first = nameProjection.FirstOrDefault();

            //            if (first != null)
            //            {
            //                monitor.OnChange(first, () => Console.WriteLine("File Changed"));
            //                first.Age += 50;
            //                var result = await monitor.SaveChanges(first);
            //            }
            //
            //var createCollectionOptions = new CreateCollectionOptions<Patient>
            //{
            //    EncryptedFields = encryptedFields
            //};
            //clientEncryption.CreateEncryptedCollection(patientDatabase,
            //    encryptedCollectionName,
            //    createCollectionOptions,
            //    kmsProviderName,
            //    customerMasterKeyCredentials);

            //var clientEncryption = new ClientEncryption(clientEncryptionOptions);
        }
    }
}
