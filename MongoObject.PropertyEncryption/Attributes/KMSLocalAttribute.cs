namespace MongoObject.PropertyEncryption.Attributes
{
    /// <summary>
    /// Marks a property to use a local Key Management Service (KMS).
    /// The attribute indicates that the encryption subsystem should obtain the encryption key
    /// from the file identified by <see cref="FilePath"/>.
    /// </summary>
    /// <param name="FilePath">The file path relative to the root of your project that contains the key material.</param>
    [AttributeUsage(AttributeTargets.Property)]
    public class KMSLocalAttribute(string FilePath) : Attribute
    {
        /// <summary>
        /// Optional explicit key value. When set, this may override or supplement the key loaded from
        /// <see cref="FilePath"/> depending on the encryption implementation.
        /// Nullable to indicate the key may be provided by the property name instead.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// The path to the key file relative to the project root.
        /// This value is supplied via the attribute's positional constructor parameter.
        /// </summary>
        public string FilePath { get; set; } = FilePath;
    }
}
