using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Exceptions
{
    [Serializable]
    public class MongoLockAcquisitionException : Exception
    {
        public MongoLockAcquisitionException()
        {
        }

        public MongoLockAcquisitionException(string? message) : base(message)
        {
        }

        public MongoLockAcquisitionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
