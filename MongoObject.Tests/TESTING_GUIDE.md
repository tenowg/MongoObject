# MongoObject.Tests - Testing Guide

## Overview

The `MongoObject.Tests` project contains comprehensive unit tests for the MongoObject library, focusing on change tracking and property change detection functionality.

## Test Structure

### ChangeTrackingTests.cs

This test class validates the core change tracking functionality provided by the `TrackingObservableObject` base class.

#### Test Categories

##### 1. Property Change Detection
- **SetField_WithDifferentValue_ShouldTrackChange** - Verifies that property values can be updated
- **SetField_WithSameValue_ShouldNotTrackChange** - Ensures no unnecessary tracking when value doesn't change
- **PropertyChanged_ShouldFireWhenPropertyIsSet** - Validates that PropertyChanged event fires on property updates
- **PropertyChanged_WithMongoChangeEventArgs_ShouldContainValue** - Confirms MongoChangeEventArgs contains the new value

##### 2. Change Tracking Lifecycle
- **TrackChanges_ShouldEnableChangeTracking** - Verifies tracking can be enabled
- **ClearChanges_ShouldResetTrackedChanges** - Ensures changes can be cleared
- **MultiplePropertyChanges_ShouldTrackAllChanges** - Tests tracking of multiple property changes

##### 3. Update Pipeline Generation
- **TryGetPendingUpdatesPipeline_WithChanges_ShouldReturnUpdateDefinition** - Validates update definition generation when changes exist
- **TryGetPendingUpdatesPipeline_WithoutChanges_ShouldReturnFalse** - Ensures no update definition when no changes

##### 4. Null Value Handling
- **SetField_WithNullValue_ShouldTrackNullChange** - Tests tracking of null assignments

##### 5. Nested Object Tracking
- **NestedObject_ShouldBeAssignedCorrectly** - Verifies nested objects can be assigned
- **NestedObject_ShouldFirePropertyChangedWhenAssigned** - Ensures PropertyChanged fires for nested object assignment
- **SetField_WithNewValue_ShouldReplaceOldValue** - Tests replacement of nested objects
- **PropertyChanged_WithNestedPropertyPath_ShouldIncludeParentName** - Validates property path includes parent name

##### 6. Resource Management
- **Dispose_ShouldUnsubscribeFromPropertyChanged** - Ensures proper cleanup on disposal

## Running the Tests

### Build the Test Project
```bash
dotnet build MongoObject.Tests
```

### Run All Tests
```bash
dotnet test MongoObject.Tests
```

### Run Specific Test Class
```bash
dotnet test MongoObject.Tests --filter "ClassName=MongoObject.Tests.ChangeTrackingTests"
```

### Run Specific Test Method
```bash
dotnet test MongoObject.Tests --filter "Name=SetField_WithDifferentValue_ShouldTrackChange"
```

### Run with Verbose Output
```bash
dotnet test MongoObject.Tests --logger "console;verbosity=detailed"
```

## Test Dependencies

The tests use the following NuGet packages:
- **xunit.v3.mtp-v2** - Testing framework
- **FluentAssertions** - Fluent assertion library for readable assertions
- **FluentAssertions.Analyzers** - Code analyzers for FluentAssertions

## Test Fixtures

### DummyObject
A test document class with the following properties:
- `DummyString` - String property for basic tracking tests
- `DummyInt` - Integer property for multi-property tests
- `DummyDate` - DateTime property for type variety
- `DummyNestedObject` - Nested object for parent-child tracking tests

### DummyNestedObject
A nested test document with:
- `NestedDummyString` - String property for nested tracking tests

## Key Testing Patterns

### Arrange-Act-Assert (AAA)
All tests follow the AAA pattern:
```csharp
[Fact]
public void TestName()
{
    // Arrange - Set up test data
    var dummy = new DummyObject();
    dummy.TrackChanges();
    
    // Act - Perform the action
    dummy.DummyString = "new value";
    
    // Assert - Verify the result
    dummy.DummyString.Should().Be("new value");
}
```

### Event Verification
Tests verify PropertyChanged events fire correctly:
```csharp
var eventFired = false;
dummy.PropertyChanged += (sender, e) => eventFired = true;
dummy.DummyString = "test";
eventFired.Should().BeTrue();
```

### Update Pipeline Testing
Tests validate the update pipeline generation:
```csharp
var result = dummy.TryGetPendingUpdatesPipeline<DummyObject>(out var update);
result.Should().BeTrue();
update.Should().NotBeNull();
```

## Future Test Expansion

### Planned Test Areas
1. **Integration Tests** - MongoDB CRUD operations
2. **Caching Tests** - Memory cache behavior
3. **Locking Tests** - Distributed lock functionality
4. **Search Tests** - Query and search operations
5. **Projection Tests** - Field projection functionality
6. **Metadata Tests** - Metadata tracking and updates

### Test Infrastructure Improvements
- Add test fixtures for common setup
- Create test data builders for complex objects
- Add performance benchmarks
- Add concurrency stress tests

## Troubleshooting

### Tests Not Running
1. Ensure the project builds: `dotnet build MongoObject.Tests`
2. Check that xunit.runner.json is present in the test project
3. Verify test class and method names follow xUnit conventions

### Assertion Failures
- Review the test output for detailed failure messages
- Use `--logger "console;verbosity=detailed"` for more information
- Check that test data is properly initialized

## Contributing Tests

When adding new tests:
1. Follow the AAA pattern (Arrange-Act-Assert)
2. Use descriptive test names following the pattern: `MethodName_Scenario_ExpectedResult`
3. Use FluentAssertions for readable assertions
4. Add XML documentation comments explaining the test purpose
5. Group related tests in the same test class
6. Keep tests focused on a single behavior

## References

- [xUnit.net Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [MongoObject Documentation](../README.md)
