using CliTool;

namespace MongoObject.CliTool.Processors
{
    internal interface IProcessor
    {
        void Execute(Settings settings);
    }
}