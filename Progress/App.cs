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
            #region QueryExample
            var nameProjection = await monitor.Search()
                .WithQuery(f =>
                {
                    //f.Name = "CraigR"; 
                    f.Name = "Craig";
                    f.Age = f.Age.And(
                        f.Age = f.Age.Lt(40),
                        f.Age = f.Age.Gt(5)
                    );
                    f.Nothing(f => f.Age = f.Age.Gt(100));
                })
                .WithLimit(5);
            #endregion
            //lockMeta = await monitor.LockDocument(first);
            //first.Tags = new();
            //first.Tags.Add("Hello");
            //first.Age = 9;
            //first.Nothing.Age = 6;
            //first.test = new();
            //first.Age = 10;
            //first.test.TestString = "hello6";
            //first.Tags = new();
            //first.Tags.Add("Hello44334244");
            //first.Properties = [];
            //first.Properties.Add("tst", "test");
            //first.Nothing.Age = 75;

            //first.Nothing.Nothing.Age = 190;

            //var result = await monitor.SaveChanges(first, lockMeta);
        }
    }
}
