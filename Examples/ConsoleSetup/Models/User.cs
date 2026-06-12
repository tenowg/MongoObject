namespace ConsoleSetup.Models
{
    #region MetadataExample
    public partial record UserMeta
    {
        public string? CreatedBy { get; set; }
        public string? Department { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? LoginCount { get; set; }
    }
    #endregion
}