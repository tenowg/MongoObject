namespace MongoObject.CliTool.Data
{
    public record CollectionDifferences(Dictionary<string, SchemaObject> ExistingCollections, Dictionary<string, SchemaObject> NewCollections);
}