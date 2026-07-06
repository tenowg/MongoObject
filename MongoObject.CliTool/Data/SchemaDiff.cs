namespace MongoObject.CliTool.Data
{
    public class SchemaDiff
    {
        public List<string> AddedFields { get; set; } = [];
        public List<string> RemovedFields { get; set; } = [];
        public List<string> ChangedFields { get; set; } = [];
    }
}