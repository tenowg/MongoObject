namespace MongoObject.CliTool.Helpers
{
    public class FileBuilder
    {
        private IndentedStringBuilder _sb = new();

        public void BuildHeaders(string projectNamespace)
        {
            _sb.AppendLine("using MongoDB.Driver;");
            _sb.AppendLine("using MongoDB.Bson;");
            _sb.AppendLine();
            _sb.AppendLine($"namespace {projectNamespace}.Migration");
            using (_sb.Block())
            {
                _sb.AppendLine("public class Migration(KmsProvidersDictionary kmsProviders) : IMigration");
                using(_sb.Block())
                {
                    _sb.AppendLine(BuildEncryption());
                }
            }
        }

        public string BuildEncryption()
        {
            var sb = new IndentedStringBuilder();

            return sb.ToString();
        }

        public string BuildIndexes()
        {
            var sb = new IndentedStringBuilder();

            return sb.ToString();
        }

        public int SaveFile()
        {
            return 0;
        }
    }
}