# DistributedLock.MongoDB

This library provides distributed lock implementation using MongoDB as the backing store.

## Installation

```bash
dotnet add package DistributedLock.MongoDB
```

## Usage

### Basic Lock Usage

```csharp
using Medallion.Threading.MongoDB;
using MongoDB.Driver;

// Create MongoDB client and database
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("myDatabase");

// Create a lock
var @lock = new MongoDistributedLock("myLockName", database);

// Acquire the lock
await using (var handle = await @lock.AcquireAsync())
{
    // Critical section protected by the lock
    Console.WriteLine("Lock acquired!");
}
// Lock is automatically released when disposed
```

### Using the Provider

```csharp
using Medallion.Threading.MongoDB;
using MongoDB.Driver;

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("myDatabase");

// Create a provider
var provider = new MongoDistributedSynchronizationProvider(database);

// Use the provider to create locks
var lock1 = provider.CreateLock("lock1");
var lock2 = provider.CreateLock("lock2");

await using (var handle = await lock1.AcquireAsync())
{
    // Do work...
}
```

### Configuration Options

You can customize the lock behavior using the options builder:

```csharp
var @lock = new MongoDistributedLock(
    "myLockName", 
    database,
    options => options
        .Expiry(TimeSpan.FromSeconds(30))      // Lock expiry time
        .ExtensionCadence(TimeSpan.FromSeconds(10)) // How often to extend the lock
        .BusyWaitSleepTime(      // Sleep time between acquire attempts
            min: TimeSpan.FromMilliseconds(10),
            max: TimeSpan.FromMilliseconds(800))
);
```

### Custom Collection Name

By default, locks are stored in a collection named "DistributedLocks". You can specify a custom collection name:

```csharp
var @lock = new MongoDistributedLock("myLockName", database, "MyCustomLocks");
```

## How It Works

The MongoDB distributed lock uses MongoDB's document upsert and update operations to implement distributed locking:

1. **Acquisition**: Attempts to insert or update a document with the lock key and a unique lock ID
2. **Extension**: Automatically extends the lock expiry while held to prevent timeout
3. **Release**: Deletes the lock document when disposed
4. **Expiry**: Locks automatically expire if not extended, allowing recovery from crashed processes

## Features

- ✅ Async/await support
- ✅ Automatic lock extension while held
- ✅ Configurable expiry and extension cadence
- ✅ Lock abandonment protection via expiry
- ✅ `CancellationToken` support
- ✅ Handle lost token notification
- ✅ Multi-target support: .NET 8, .NET Standard 2.1, .NET Framework 4.7.2

## Notes

- The lock collection will have an index on the `expiresAt` field for efficient queries
- Lock extension happens automatically in the background
- If lock extension fails, the `HandleLostToken` will be signaled
- Stale locks (from crashed processes) will automatically expire based on the expiry setting
