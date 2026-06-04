namespace MongoObject.Core.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a MongoDB document. This attribute is used by the source generator to generate necessary code for MongoDB integration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoObjectAttribute : Attribute
    {
        /// <summary>
        /// The name of the MongoDB collection to which the class will be mapped. If not specified, the collection name will default to the class name.
        /// </summary>
        public string? CollectionName { get; set; }
        /// <summary>
        /// The name of the MongoDB database to which the class will be mapped. If not specified, the database name will default to the setting in `MongoObjectOptions`.
        /// </summary>
        public string? DatabaseName { get; set; }
        /// <summary>
        /// The type of the metadata class that will be used for this document. 
        /// This is optional, and if not specified, the source generator will generate a default metadata class with no fields. 
        /// The metadata class is used to define additional information about the document, such as indexes or validation rules, 
        /// and it can also be used to define custom query and record types for the document. 
        /// If you want to use a custom metadata class, you need to implement a class that specifies the properties and types.
        /// </summary>
        public Type? MetadataType { get; set; }
        /// <summary>
        /// Whether to ignore extra elements in the MongoDB document that are not defined in the class.
        /// This is optional and defaults to true. 
        /// If set to true, the source generator will add the [BsonIgnoreExtraElements] attribute to the generated class, 
        /// which tells the MongoDB driver to ignore any extra fields in the document that are not defined in the class.
        /// </summary>
        public bool IgnoreExtraElements { get; set; } = true;
    }
}
