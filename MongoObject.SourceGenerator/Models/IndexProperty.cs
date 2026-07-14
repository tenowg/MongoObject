public sealed record IndexProperty
{
    public string IndexName { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string QueryName { get; set; } = string.Empty;
    public string Direction { get; init; } = "Ascending";
}