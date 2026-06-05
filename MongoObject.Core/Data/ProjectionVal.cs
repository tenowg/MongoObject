namespace MongoObject.Core.Data
{
    public abstract record ProjectionVal
    {
        // Implicit operator: true maps to simple Include
        public static implicit operator ProjectionVal(bool include) => include ? new Include() : new Exclude();

        // Implicit operator: int maps to an array Slice operation
        //public static implicit operator ProjectionVal(int sliceCount, int skipCount) => new Slice(sliceCount, skipCount);

        public record Include : ProjectionVal;
        public record Exclude : ProjectionVal;
        public record Slice(int Limit, int Skip) : ProjectionVal;
        // You can add more complex operations later (e.g., ElementAt, AsString, etc.)
    }

    public static class ProjectionExtensions
    {
        extension(ProjectionVal? q)
        {
            public static ProjectionVal Include => new ProjectionVal.Include();
            public static ProjectionVal Exclude => new ProjectionVal.Exclude();
            public static ProjectionVal Slice(int limit, int skip) => new ProjectionVal.Slice(limit, skip);
        }
    }
}
