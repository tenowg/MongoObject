using Microsoft.Extensions.Primitives;
using MongoObject.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Services
{
    /// <summary>
    /// Monitors per-document change tokens and provides CancellationToken instances that are cancelled when a specific
    /// document is signaled as changed.
    /// </summary>
    /// <remarks>Thread-safe. GetChangeToken returns an existing or new CancellationToken for the specified
    /// documentId. SignalChange cancels and removes the token for that documentId. Dispose cancels and disposes all
    /// active tokens.</remarks>
    /// <typeparam name="TDoc">The document file type; must implement IDocumentFile.</typeparam>
    public class DocumentChangeTokenMonitor<TDoc> : IDocumentTokenChangeMonitor<TDoc>, IDisposable where TDoc : class, IDocumentFile
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _signals = new();

        public void Dispose()
        {
            var tokensToDispose = _signals.Values.ToList();

            _signals.Clear();

            foreach (var cts in tokensToDispose)
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
                finally
                {
                    cts.Dispose();
                }
            }
        }

        public IChangeToken GetChangeToken(string documentId)
        {
            var cts = _signals.GetOrAdd(documentId, _ => new CancellationTokenSource());
            _signals[documentId] = cts;
            return new CancellationChangeToken(cts.Token);
        }

        public void SignalChange(string documentId)
        {
            if (_signals.TryRemove(documentId, out var cts))
            { 
                cts.Cancel();
            }
        }
    }
}
