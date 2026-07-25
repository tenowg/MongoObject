using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Data
{
    public record SaveChangesResult(bool SuccessResult, string? Message = null, UpdateResult? result = null)
    {
        public static SaveChangesResult Failed(string message, UpdateResult? result = null) => new(false, message, result);
        public static SaveChangesResult Success(UpdateResult? result) => new(true, null, result);
    }
}
