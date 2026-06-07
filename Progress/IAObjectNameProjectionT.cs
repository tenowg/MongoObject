//using MongoDB.Bson.IO;

//public class AObjectVectorTestSerializer : global::MongoDB.Bson.Serialization.Serializers.SerializerBase<global::Progress.Test.AObjectVectorTest>
//{
//    public override void Serialize(global::MongoDB.Bson.Serialization.BsonSerializationContext context, global::MongoDB.Bson.Serialization.BsonSerializationArgs args, global::Progress.Test.AObjectVectorTest value)
//    {
//        var bsonWriter = context.Writer;
//        bsonWriter.WriteStartDocument();
//        // Implementation for serialization
//        bsonWriter.WriteEndDocument();
//    }

//    public override Progress.Test.AObjectVectorTest Deserialize(global::MongoDB.Bson.Serialization.BsonDeserializationContext context, global::MongoDB.Bson.Serialization.BsonDeserializationArgs args)
//    {
//        var bsonReader = context.Reader;
//        bsonReader.ReadStartDocument();

//        var result = new global::Progress.Test.AObjectVectorTest();

//        while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)
//        {
//            var name = bsonReader.ReadName();
//            if (name == "Document") // Handle nested properties safely
//            {
//                bsonReader.ReadStartDocument();
//                while (bsonReader.ReadBsonType() != global::MongoDB.Bson.BsonType.EndOfDocument)
//                {
//                    var subName = bsonReader.ReadName();
//                    if (subName == "Name")
//                    {
//                        // Deserialize the value directly into the property
//                        result.Name = global::MongoDB.Bson.Serialization.BsonSerializer.Deserialize<string>(bsonReader);
//                        continue;
//                    }
//                    bsonReader.SkipValue();
//                }
//                bsonReader.ReadEndDocument();
//            }
//            if (bsonReader.State == BsonReaderState.Type)
//            {
//                bsonReader.ReadBsonType();
//            }
//            if (bsonReader.State == BsonReaderState.Name)
//            {
//                bsonReader.ReadName();
//            }
//            bsonReader.SkipValue(); // Skips fields like _id if they leak through
//        }
//        bsonReader.ReadEndDocument();
//        return result;
//    }
//}