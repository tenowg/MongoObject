using MongoDB.Driver;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using Progress.Test;

namespace Progress
{
    public class App(IDocumentMonitor<AObject> monitor, IMongoClient client) : IDisposable
    {
        //IMongoLockScope? lockMeta;

        public void Dispose()
        {
            //lockMeta?.DisposeAsync();
        }

        public async Task Run()
        {
            Console.WriteLine("Hello, World!");

            //await monitor.Add(new AObject {Name = "Craig is a cat that likes to eat biskits in the morning, along with himself", Age = 4}, f => f.OwnerId = "123.abc");

            #region QueryExample
            var nameProjection = await monitor.Search()
                //.WithQuery(f =>
                //{
                //    f.Name = "Craig";
                //    f.Age = f.Age.And(
                //        f.Age = f.Age.Lt(40000),
                //        f.Age = f.Age.Gt(2)
                //    );
                //})
                //.WithNameProjection();
                .WithVectorTestVector()
                //.WithEmbedding("Craig is eating biskits in the morning, I wonder if he is a cat")
                .WithMaxReturns(5);
            //.WithListTestProjection()
            //.WithListTestSlice(5, 3)
            //.WithLimit(5);
            #endregion

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
            var model = new global::MongoDB.Driver.CreateVectorSearchIndexModel<global::MongoObject.Core.Data.MongoDocument<AObject>>(
                                x => x.Document.Name,
                                "dddas",
                                VectorSimilarity.Cosine,
                                1024
            // m => m.Runtime, m => m.Year  // Optional filter fields
                                );
        }
    }
}
