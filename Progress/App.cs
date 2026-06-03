using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using Progress.Test;

namespace Progress
{
    public class App(IDocumentMonitor<AObject> monitor) : IDisposable
    {
        IMongoLockScope? lockMeta;

        public void Dispose()
        {
            lockMeta?.DisposeAsync();
        }

        public async Task Run()
        {
            Console.WriteLine("Hello, World!");
            //var testr = await monitor.Get("Anything");
            //await monitor.Add(new AObject { Name = "CraigR", Age = 500 }, null);

            //var ttt = await monitor.DocumentSearch(f => { f.Name = "CraigR"; f.Age = 10; });
            //var first = ttt.FirstOrDefault();
            var nameProjection = await monitor.Search()
                .WithQuery(f => { f.Name = "CraigR"; f.Age = f.Age.Gt(5); })
                .WithLimit(5)
                .WithNameProjection();

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
