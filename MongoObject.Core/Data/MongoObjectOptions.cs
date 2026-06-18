using System;
using System.Collections.Generic;
using System.Text;

namespace MongoObject.Core.Data
{
    public class MongoObjectOptions
    {
        /// <summary>
        /// The MongoDB connection string used to create your client,
        /// this is optional, and only needed for use with the cli client.
        /// </summary>
        public string? ConnectionString { get; set; }
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// The duration for which document data is considered fresh in the cache.
        /// After this time, the cache will attempt to refresh from the database.
        /// Defaults to 10 minutes.
        /// </summary>
        public TimeSpan CacheSoftDuration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// The maximum duration for which configuration data is cached.
        /// After this time, the cache entry is evicted regardless of freshness.
        /// Defaults to 24 hours.
        /// </summary>
        public TimeSpan CacheHardDuration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// The prefix used for cache keys to avoid collisions.
        /// Defaults to "mongo_cfg_".
        /// </summary>
        public string CachePrefix { get; set; } = "mongo_cfg_";

        public string MongoSystemDatabaseName { get; set; } = "mongo_system";
        public string DistributedLockCollectionName { get; set; } = "distributed_lock";
        public TimeSpan DistributedLockDefaultLockDuration { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan WatchPollInterval { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// Is the database connection an Atlas Server, this will enable some features not available in the Community Additions
        /// </summary>
        public bool IsAtlasMongoDBInstance { get; set; }
    }
}
