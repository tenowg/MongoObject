using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Data
{
    public record SaveChangesResult(bool SuccessResult, string? Message = null)
    {
        public static SaveChangesResult Failed(string message) => new(false, message);
        public static SaveChangesResult Success => new(true);
    }
}
