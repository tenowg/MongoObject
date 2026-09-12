# Project Brief: MongoObject

## Overview

**MongoObject** is a modern MongoDB ODM (Object-Document Mapper) for .NET 10 with source generation and automatic change tracking.

**Author:** Craig Russell  
**License:** MIT  
**Repository:** https://github.com/tenowg/MongoObject  
**Documentation:** https://tenowg.github.io/MongoObject  

## Core Requirements

- **Compile-time safety over runtime configuration** — All queries, projections, and update definitions are validated at compile time via Roslyn source generators
- **POCO-first development** — Work with plain C# classes; no base class inheritance required beyond optional partial properties
- **Minimal MongoDB payloads** — Only changed fields are sent to the database ($set/$unset)
- **Zero boilerplate** — Source generation eliminates manual mapping code
- **Source generation instead of reflection** — AoT compatible, zero runtime reflection overhead
- **Native MongoDB features without hiding MongoDB itself** — Leverages MongoDB.Driver directly while providing a higher-level API

## Design Goals

1. Provide an EF Core-like change tracking experience for MongoDB documents
2. Eliminate string-based queries and field name literals
3. Enable type-safe document operations with full IDE support
4. Support modern .NET features (C# 14 partial properties, .NET 10)
5. Maintain AoT compatibility throughout

## Key Features

- 🚀 **Source Generation** — Automatic implementation via `[MongoObject]` attribute
- 📊 **Change Tracking** — Automatic property change detection for efficient updates
- 📝 **Metadata Support** — Separate metadata types for versioning, timestamps, and ownership
- 🔍 **Type-Safe Queries** — Generated search classes for compile-time query validation
- 🎯 **Projections** — Selective field retrieval with `[ProjectValue]` attribute
- 🔒 **Distributed Locking** — Document-level concurrency control (MongoDB & Redis backends)
- ⚡ **Caching** — Built-in memory caching with configurable expiration
- 👁️ **Change Streams** — Real-time MongoDB change monitoring
- 🛠️ **CLI Migration Support** — Build, validate, and rename properties via CLI tool
- 🔗 **Vector Search Integration** — Built-in vector search enabled via attributes

## Versioning

Uses [MinVer](https://github.com/adamralph/minver) for version management based on git tags. Current published versions:
- `Tenowg.MongoObjects`: 0.2.0-alpha.9
- `Tenowg.MongoObjects.CliTool`: 0.3.0-beta.1
- Latest release tags: 0.3.5-beta, 0.3.4-beta
