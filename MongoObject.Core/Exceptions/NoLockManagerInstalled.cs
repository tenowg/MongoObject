using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Exceptions
{
    public class NoLockManagerInstalled : Exception
    {
        public NoLockManagerInstalled() : base("No long manager installed")
        {
        }

        public NoLockManagerInstalled(string? message) : base(message)
        {
        }

        public NoLockManagerInstalled(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
