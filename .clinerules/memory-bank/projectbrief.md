# MongoObject Project Brief

## Overview
MongoObject is a MongoDB ODM (Object Document Mapper) library for .NET 10 that provides an EF Core-like experience for working with MongoDB documents.

## Package Information
- **Package ID**: Tenowg.MongoObjects
- **Author**: Craig Russell
- **Repository**: https://github.com/tenowg/MongoObjects
- **Target Framework**: .NET 10
- **C# Version**: C# 14

## Project Structure
The solution consists of three main projects:

### 1. MongoObject.Core
The core library providing:
- Attribute-based document definition (`[MongoObject]`)
- Change tracking via `TrackingObservableObject` base class
- Document monitoring and CRUD operations
- Caching layer with memory cache
- Distributed locking support
- MongoDB change stream watching

### 2. MongoObject.SourceGenerator
A Roslyn-based incremental source generator that:
- Generates partial class implementations for `[MongoObject]` decorated classes
- Creates metadata query and record types
- Generates search classes
- Validates document classes at compile time

### 3. Progress (Test/Demo Project)
A console application demonstrating library usage.

## Core Requirements
1. Provide simple, attribute-based MongoDB document mapping
2. Automatic change tracking for efficient updates
3. Type-safe queries and searches
4. Support for projections
5. Distributed locking for concurrent access
6. Caching for improved performance
7. Real-time document change monitoring

## Key Features
- **Document Tracking**: Automatic property change detection via source generation
- **Metadata Support**: Custom metadata types for each document (version, timestamps, etc.)
- **Search Capabilities**: Both document and metadata-based searching
- **Projections**: Selective field retrieval using `[ProjectValue]` attribute
- **Locking**: Distributed lock support for document-level concurrency
- **Caching**: Built-in memory caching with configurable expiration