# Contributing to MongoObject

First off, thank you for considering contributing to MongoObject! It's people like you that make the open-source .NET community so great. 

Our goal is to bring a modern, EF Core-like, and highly performant experience to MongoDB using .NET 10 and Source Generators. We welcome contributions of all kinds: bug reports, feature requests, documentation improvements, and pull requests.

## Table of Contents
1. [Reporting Bugs](#reporting-bugs)
2. [Suggesting Enhancements](#suggesting-enhancements)
3. [Local Development Setup](#local-development-setup)
4. [Project Structure](#project-structure)
5. [Pull Request Process](#pull-request-process)

---

## Reporting Bugs
If you find a bug, please create an issue. Before creating a new issue, please search the existing issues to see if it has already been reported. 

When opening a bug report, please include:
* What version of .NET and the `MongoObject` package you are using.
* A clear and descriptive title.
* Steps to reproduce the issue (a minimal code snippet is highly appreciated).
* What you expected to happen vs. what actually happened.

## Suggesting Enhancements
We are always looking for ways to improve! If you have an idea for a new feature or an improvement to the Source Generator, please open an issue with the tag `enhancement`. 
* Explain **why** this enhancement would be useful to most users.
* Provide examples of the proposed syntax or API design.

## Local Development Setup
To build and contribute to MongoObject locally, you will need the following:

### Prerequisites
1. **.NET 10 SDK**: Make sure you have the latest [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.
2. **IDE**: Visual Studio 2022 (latest preview for C# 14 support), JetBrains Rider, or VS Code with the C# Dev Kit.
3. **MongoDB**: You will need a local MongoDB instance running for integration tests. The easiest way is via Docker:
   ```bash
   docker run --name mongoobject-db -p 27017:27017 -d mongodb/mongodb-community-server:latest
   ```
   
## Building the Project
Clone the repository and build the solution:
``` Bash
git clone https://github.com/tenowg/MongoObject.git
cd MongoObject
dotnet build
```

## Project Structure
To help you navigate the codebase, here is a quick overview of the repository:
* `MongoObject.Core/` - The main library containing base classes, DI registration, and attributes.
* `MongoObject.SourceGenerator/` - The Roslyn Source Generator that creates the boilerplate for change tracking and compile-time queries.
* `MongoObject.Tests/` - Unit and integration tests. Note: Please ensure you write tests for any new features.
* `MongoObject.Examples/` - Sample projects demonstrating how to use the library.

## Pull Request Process
1. **Fork the repository** and create your branch from `master`.
2. **Write tests for your code.** If you are modifying the Source Generator, ensure the output syntax trees are being tested.
3. **Ensure the build passes.** Run dotnet test locally to ensure no existing functionality is broken.
4. **Follow coding standards.** We stick to standard C# coding conventions. If there is an .editorconfig file present, please ensure your IDE respects it.
5. **Open a Pull Request.** Provide a clear description of what you changed, why you changed it, and link to any relevant open issues (e.g., "Fixes #12").

## A Note on Source Generators
Debugging Roslyn Source Generators can be tricky. If you are contributing to MongoObject.SourceGenerator, please refer to the Microsoft documentation on debugging Source Generators and ensure your generated code does not produce compiler warnings.