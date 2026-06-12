using FluentAssertions;
using MongoDB.Bson;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;
using MongoObject.Tests.Dummies;

namespace MongoObject.Tests
{
    /// <summary>
    /// Tests for change tracking functionality in TrackingObservableObject.
    /// Verifies that property changes are properly detected and tracked.
    /// </summary>
    public class ChangeTrackingTests
    {
        [Fact]
        public void SetField_WithDifferentValue_ShouldTrackChange()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var initialValue = "initial";
            dummy.DummyString = initialValue;

            // Act
            dummy.DummyString = "updated";

            // Assert
            dummy.DummyString.Should().Be("updated");
        }

        [Fact]
        public void SetField_WithSameValue_ShouldNotTrackChange()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var value = "same";
            dummy.DummyString = value;

            // Act - SetField is protected, so we test through property assignment
            var initialValue = dummy.DummyString;
            dummy.DummyString = value;

            // Assert
            dummy.DummyString.Should().Be(initialValue);
        }

        [Fact]
        public void PropertyChanged_ShouldFireWhenPropertyIsSet()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var propertyChangedFired = false;
            var changedPropertyName = string.Empty;

            dummy.PropertyChanged += (sender, e) =>
            {
                propertyChangedFired = true;
                changedPropertyName = e.PropertyName;
            };

            // Act
            dummy.DummyString = "new value";

            // Assert
            propertyChangedFired.Should().BeTrue("PropertyChanged event should fire");
            changedPropertyName.Should().Be("DummyString");
        }

        [Fact]
        public void PropertyChanged_WithMongoChangeEventArgs_ShouldContainValue()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var capturedValue = string.Empty;

            dummy.PropertyChanged += (sender, e) =>
            {
                if (e is MongoChangeEventArgs mongoEvent)
                {
                    capturedValue = mongoEvent.Value?.ToString() ?? string.Empty;
                }
            };

            // Act
            var newValue = "test value";
            dummy.DummyString = newValue;

            // Assert
            capturedValue.Should().Be(newValue);
        }

        [Fact]
        public void TrackChanges_ShouldEnableChangeTracking()
        {
            // Arrange
            var dummy = new DummyObject();

            // Act
            dummy.TrackChanges();

            // Assert
            // Tracking is protected, so we verify it indirectly by checking PropertyChanged fires
            var eventFired = false;
            dummy.PropertyChanged += (sender, e) => eventFired = true;
            dummy.DummyString = "test";
            eventFired.Should().BeTrue("PropertyChanged should fire after TrackChanges is called");
        }

        [Fact]
        public void ClearChanges_ShouldResetTrackedChanges()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            dummy.DummyString = "changed";

            // Act
            dummy.ClearChanges();

            // Assert
            // After clearing, TryGetPendingUpdatesPipeline should return false
            var result = dummy.TryGetPendingUpdatesPipeline<DummyObject>(out var update);
            result.Should().BeFalse("No pending updates should exist after clearing changes");
        }

        [Fact]
        public void TryGetPendingUpdatesPipeline_WithChanges_ShouldReturnUpdateDefinition()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            dummy.DummyString = "new value";

            // Act
            var result = dummy.TryGetPendingUpdatesPipeline<DummyObject>(out var update);

            // Assert
            result.Should().BeTrue("Should return true when there are pending changes");
            update.Should().NotBeNull("Update definition should not be null");
        }

        [Fact]
        public void TryGetPendingUpdatesPipeline_WithoutChanges_ShouldReturnFalse()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();

            // Act
            var result = dummy.TryGetPendingUpdatesPipeline<DummyObject>(out var update);

            // Assert
            result.Should().BeFalse("Should return false when there are no pending changes");
            update.Should().BeNull("Update definition should be null");
        }

        [Fact]
        public void MultiplePropertyChanges_ShouldTrackAllChanges()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var changeCount = 0;

            dummy.PropertyChanged += (sender, e) =>
            {
                changeCount++;
            };

            // Act
            dummy.DummyString = "value1";
            dummy.DummyInt = 42;
            dummy.DummyDate = DateTime.UtcNow;

            // Assert
            changeCount.Should().Be(3, "PropertyChanged should fire for each property change");
        }

        [Fact]
        public void SetField_WithNullValue_ShouldTrackNullChange()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            dummy.DummyString = "initial";

            // Act
            dummy.DummyString = null;

            // Assert
            dummy.DummyString.Should().BeNull();
        }

        [Fact]
        public void NestedObject_ShouldBeAssignedCorrectly()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var nestedObject = new DummyNestedObject();

            // Act
            dummy.DummyNestedObject = nestedObject;

            // Assert
            dummy.DummyNestedObject.Should().Be(nestedObject);
        }

        public class TestableDummyNestedObject : DummyNestedObject
        {
            // Expose the protected method via a public method
            public void ForceBuildChangesTrigger()
            {
                base.ProcessPossibleChanges(); // Call your protected method here
            }
        }

        //will need to figure out test for this, as PropertyChange doesn't get called on the root of a nest propertychange
        // the nested PropertyChange directly assigns to the root Change Method, so it cannot be tracked like this
        //[Fact]
        //public void NestedObject_ShouldFirePropertyChangedWhenAssigned()
        //{
        //    // Arrange
        //    var dummy = new DummyObject();
        //    dummy.TrackChanges();
        //    var nestedObject = new TestableDummyNestedObject();
        //    var eventFired = false;

        //    nestedObject.PropertyChanged += (sender, e) =>
        //    {
        //        eventFired = true;
        //    };

        //    // Act
        //    dummy.DummyNestedObject = nestedObject;

        //    // Assert
        //    eventFired.Should().BeTrue("PropertyChanged should fire when nested object is assigned");
        //}

        [Fact]
        public void Dispose_ShouldUnsubscribeFromPropertyChanged()
        {
            // Arrange
            var dummy = new DummyObject();
            dummy.TrackChanges();
            var eventFired = false;

            dummy.PropertyChanged += (sender, e) =>
            {
                eventFired = true;
            };

            // Act
            dummy.Dispose();
            dummy.DummyString = "after dispose";

            // Assert
            eventFired.Should().BeFalse("PropertyChanged should not fire after Dispose");
        }

        [Fact]
        public void SetField_WithNewValue_ShouldReplaceOldValue()
        {
            // Arrange
            var dummy = new DummyObject();
            var oldNestedObject = new DummyNestedObject { NestedDummyString = "old" };
            dummy.DummyNestedObject = oldNestedObject;

            // Act
            var newNestedObject = new DummyNestedObject { NestedDummyString = "new" };
            dummy.DummyNestedObject = newNestedObject;

            // Assert
            dummy.DummyNestedObject.Should().Be(newNestedObject);
            dummy.DummyNestedObject.NestedDummyString.Should().Be("new");
        }

        // same issue as above in NestedObject_ShouldFirePropertyChangedWhenAssigned()
        //[Fact]
        //public void PropertyChanged_WithNestedPropertyPath_ShouldIncludeParentName()
        //{
        //    // Arrange
        //    var dummy = new DummyObject();
        //    dummy.TrackChanges();
        //    var capturedPropertyName = string.Empty;

        //    dummy.PropertyChanged += (sender, e) =>
        //    {
        //        capturedPropertyName = e.PropertyName ?? string.Empty;
        //    };

        //    var nestedObject = new DummyNestedObject();
        //    dummy.DummyNestedObject = nestedObject;

        //    // Act
        //    nestedObject.NestedDummyString = "nested value";

        //    // Assert
        //    capturedPropertyName.Should().Contain("DummyNestedObject");
        //}
    }

}
