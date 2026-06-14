# MongoObject.RedisDistributedLock

**Redis-backed distributed locking for [MongoObject](../MongoObject.Core/README.md).**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../LICENSE.txt)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

---

## Overview

`MongoObject.RedisDistributedLock` provides a Redis-backed implementation of `IDistributedLockManager` for the MongoObject ODM. It replaces the default no-op lock manager with a production-ready distributed lock that uses Redis for coordination across multiple application instances.

Locks are stored as Redis hashes with automatic TTL expiration, and all lock acquisition/release operations use Lua scripts for atomicity.

---

## Prerequisites

- **MongoObject.Core** installed and configured
- A **Redis** instance accessible to your application
- An **`IConnectionMultiplexer`** registered in your DI container (this package does **not** register one for you)

---

## Installation

```bash
dotnet add package Tenowg.MongoObjects.RedisDistributedLock
```

---

## Setup

### 1. Register your Redis client

You must register `IConnectionMultiplexer` yourself before adding the Redis lock manager:

```csharp
using StackExchange.Redis;

// Option A: Direct registration
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

// Option B: Using a connection string from configuration
services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
```

### 2. Add the Redis lock manager to MongoObject

```csharp
using MongoObject.Core.Extensions;

services.AddMongoObject(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "MyApp";
})
.AddRedisLockManager()   // <-- replaces the no-op lock manager with Redis
.AddWatchStream()
.RegisterDocumentsFromAssembly();
```

---

## Usage

Once registered, distributed locking works transparently through `IDocumentMonitor<T>`:

```csharp
public class OrderService(IDocumentMonitor<Order> monitor)
{
    // Scoped lock - automatically released when disposed
    public async Task ProcessOrderAsync(Order order)
    {
        await using var lockScope = await monitor.LockDocument(order);

        // Only one instance can be here at a time
        order.Status = "Processing";
        await monitor.SaveChanges(order, lockScope);
    }

    // Manual lock management
    public async Task ManualLockExample(Order order)
    {
        var result = await monitor.LockDocument(order);
        // result is IMongoLockScope - dispose to release
        await using (result)
        {
            order.Status = "Processing";
            await monitor.SaveChanges(order, result);
        }
    }
}
```

---

## How It Works

### Lock Storage

Each lock is stored as a Redis hash with a TTL:

```
Key:    mongolock:{documentKey}
Type:   Hash
Fields:
  holderId   - Unique identifier for the lock holder
  expiresAt  - Unix timestamp (ms) when the lock expires
  acquiredAt - Unix timestamp (ms) when the lock was acquired
TTL:    Set to the lock duration (auto-cleanup)
```

### Atomic Operations

All lock operations use Lua scripts to ensure atomicity:

- **Acquire**: Checks expiry and sets lock in a single atomic operation
- **Release**: Only deletes the lock if the caller holds it
- **Renew**: Only extends the TTL if the caller holds a non-expired lock

### Lock Key Format

The holder ID encodes the machine, process, document key, and a unique GUID:

```
{MachineName}-{ProcessId}-{documentKey}-{guid}
```

This allows the system to identify which machine/process holds a lock.

---

## Configuration

Lock duration is controlled via `MongoObjectOptions`:

```csharp
services.AddMongoObject(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "MyApp";
    options.DistributedLockDefaultLockDuration = TimeSpan.FromSeconds(30); // default
});
```

---

## Comparison with MongoDistributedLock

| Feature | MongoDistributedLock | RedisDistributedLock |
|---------|---------------------|---------------------|
| Backend | MongoDB collection | Redis hash + TTL |
| Atomicity | `findOneAndUpdate` | Lua scripts |
| Auto-cleanup | Manual / query-based | Redis TTL |
| External dependency | None (uses existing MongoDB) | Requires Redis |
| Best for | Simple setups, single MongoDB | High-throughput, Redis already in stack |

---

## License

MIT License - see [LICENSE.txt](../LICENSE.txt) for details.
