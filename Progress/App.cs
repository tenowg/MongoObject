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

            //await monitor.Add(new AObject {Name = "Craig", Age = 4}, f => f.OwnerId = "123.abc");
            
            #region QueryExample
            var nameProjection = await monitor.Search()
                .WithQuery(f =>
                {
                    f.Name = "Craig";
                    f.Age = f.Age.And(
                        f.Age = f.Age.Lt(40000),
                        f.Age = f.Age.Gt(2)
                    );
                })
                //.WithListTestProjection()
                //.WithListTestSlice(5, 3)
                .WithLimit(5);
            #endregion
await Task.Delay(500);
Console.WriteLine("Commensing Change");
            var first = nameProjection.FirstOrDefault();

            if (first != null)
            {
                monitor.OnChange(first, () => Console.WriteLine("File Changed"));
                first.Age += 50;
                var result = await monitor.SaveChanges(first);
            }
        }
    }
}
