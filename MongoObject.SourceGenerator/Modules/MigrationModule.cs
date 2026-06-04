using Microsoft.CodeAnalysis;
using MongoObject.SourceGenerator.Interfaces;
using MongoObject.SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.SourceGenerator.Modules
{
    internal class MigrationModule : CodeModule
    {
        public override void Execute(SourceProductionContext context, (CommonModel model, Compilation comp) provider)
        {
        }
    }
}
