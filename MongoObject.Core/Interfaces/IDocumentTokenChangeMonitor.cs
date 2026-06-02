using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Interfaces
{
    public interface IDocumentTokenChangeMonitor<T>  where T : class, IDocumentFile
    {
        public IChangeToken GetChangeToken(string documentId);

        public void SignalChange(string documentId);
    }
}
