using FluentAssertions;
using MongoDB.Bson;
using MongoObject.Core.Data;
using MongoObject.Tests.Dummies;

namespace MongoObject.Tests
{
    /// <summary>
    /// Tests for MongoDocument<T> wrapper class.
    /// Verifies document structure, ID generation, and metadata handling.
    /// These seemingly pendatic tests are required due to source generation behaviour
    /// </summary>
    public class MongoDocumentTests
    {
        [Fact]
        public void Constructor_ShouldGenerateUniqueId()
        {
            // Arrange & Act
            var doc1 = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };
            var doc2 = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };

            // Assert
            doc1.Id.Should().NotBeNullOrEmpty();
            doc2.Id.Should().NotBeNullOrEmpty();
            doc1.Id.Should().NotBe(doc2.Id, "Each document should have a unique ID");
        }

        [Fact]
        public void Id_ShouldBeValidGuid()
        {
            // Arrange & Act
            var doc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };

            // Assert
            // The ID is generated as Guid.NewGuid().ToString("N")
            doc.Id.Should().HaveLength(32, "GUID without hyphens should be 32 characters");
            doc.Id.Should().MatchRegex(@"^[a-f0-9]{32}$", "ID should be a valid hex string");
        }

        [Fact]
        public void Document_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var mongoDoc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };

            // Assert
            mongoDoc.Document.Should().BeNull();
        }

        [Fact]
        public void Document_ShouldStoreAssignedValue()
        {
            // Arrange
            var mongoDoc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };
            var dummy = new DummyObject { DummyString = "test" };

            // Act
            mongoDoc.Document = dummy;

            // Assert
            mongoDoc.Document.Should().Be(dummy);
            mongoDoc.Document.DummyString.Should().Be("test");
        }

        [Fact]
        public void Metadata_ShouldBeRequired()
        {
            // Arrange & Act
            var dummy = new DummyObject();
            var metadata = new BsonDocument { { "Version", 1 }, { "CreatedAt", DateTime.UtcNow } };

            // Assert - This test verifies the required property constraint
            // The Metadata property is required, so it must be set during construction
            var mongoDoc = new MongoDocument<DummyObject>
            {
                Document = dummy,
                Metadata = metadata
            };

            mongoDoc.Metadata.Should().NotBeNull();
        }

        [Fact]
        public void Metadata_ShouldStoreBsonDocument()
        {
            // Arrange
            var mongoDoc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };
            var metadata = new BsonDocument
            {
                { "Version", 1 },
                { "CreatedAt", DateTime.UtcNow },
                { "OwnerId", "user123" }
            };

            // Act
            mongoDoc.Metadata = metadata;

            // Assert
            mongoDoc.Metadata.Should().NotBeNull();
            mongoDoc.Metadata["Version"].AsInt32.Should().Be(1);
            mongoDoc.Metadata["OwnerId"].AsString.Should().Be("user123");
        }

        [Fact]
        public void Metadata_ShouldSupportCustomFields()
        {
            // Arrange
            var mongoDoc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };
            var metadata = new BsonDocument();

            // Act
            metadata["CustomField1"] = "value1";
            metadata["CustomField2"] = 42;
            metadata["CustomField3"] = new BsonArray { "item1", "item2" };
            mongoDoc.Metadata = metadata;

            // Assert
            mongoDoc.Metadata["CustomField1"].AsString.Should().Be("value1");
            mongoDoc.Metadata["CustomField2"].AsInt32.Should().Be(42);
            mongoDoc.Metadata["CustomField3"].AsBsonArray.Should().HaveCount(2);
        }

        [Fact]
        public void MongoDocument_ShouldWrapDocumentAndMetadata()
        {
            // Arrange
            var dummy = new DummyObject
            {
                DummyString = "test document",
                DummyInt = 42,
                DummyDate = DateTime.UtcNow
            };

            var metadata = new BsonDocument
            {
                { "Version", 1 },
                { "CreatedAt", DateTime.UtcNow },
                { "LastModifiedAt", DateTime.UtcNow }
            };

            // Act
            var mongoDoc = new MongoDocument<DummyObject>
            {
                Document = dummy,
                Metadata = metadata
            };

            // Assert
            mongoDoc.Document.Should().Be(dummy);
            mongoDoc.Metadata.Should().NotBeNull();
            mongoDoc.Id.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void MongoDocument_ShouldAllowIdOverride()
        {
            // Arrange
            var mongoDoc = new MongoDocument<DummyObject> { Metadata = new BsonDocument() };
            var customId = "custom-id-12345";

            // Act
            mongoDoc.Id = customId;

            // Assert
            mongoDoc.Id.Should().Be(customId);
        }

        [Fact]
        public void MongoDocument_ShouldSupportMultipleInstances()
        {
            // Arrange & Act
            var doc1 = new MongoDocument<DummyObject>
            {
                Document = new DummyObject { DummyString = "doc1" },
                Metadata = new BsonDocument { { "Version", 1 } }
            };

            var doc2 = new MongoDocument<DummyObject>
            {
                Document = new DummyObject { DummyString = "doc2" },
                Metadata = new BsonDocument { { "Version", 2 } }
            };

            // Assert
            doc1.Document.DummyString.Should().Be("doc1");
            doc2.Document.DummyString.Should().Be("doc2");
            doc1.Metadata["Version"].AsInt32.Should().Be(1);
            doc2.Metadata["Version"].AsInt32.Should().Be(2);
            doc1.Id.Should().NotBe(doc2.Id);
        }

        [Fact]
        public void Metadata_ShouldBeModifiable()
        {
            // Arrange
            var mongoDoc = new MongoDocument<DummyObject>
            {
                Metadata = new BsonDocument { { "Version", 1 } }
            };

            // Act
            mongoDoc.Metadata["Version"] = 2;
            mongoDoc.Metadata["LastModified"] = DateTime.UtcNow;

            // Assert
            mongoDoc.Metadata["Version"].AsInt32.Should().Be(2);
            mongoDoc.Metadata.Should().HaveCount(2, "Metadata should have Version and LastModified");
        }
    }
}
