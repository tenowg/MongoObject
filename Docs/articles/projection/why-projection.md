---
uid: why-projections
---

# Understanding Projections and Performance Optimization
When querying a document database like MongoDB, retrieving complete documents by default can introduce hidden performance bottlenecks. As collections scale, fetching unneeded data wastes critical compute, memory, and network assets.

Projections solve this by allowing you to cherry-pick exactly which properties you need before data ever leaves the database instance

## What is a MongoDB Projection?
A projection is a database-level instruction that restricts the fields returned by a query. Think of it as the NoSQL equivalent to a `SQL SELECT field1, field2` statement.

Instead of moving a massive multi-megabyte document across the wire, MongoDB filters the schema internally and streams only the requested key-value pairs back to your application pipeline.

## How Projections Save Infrastructure Resources
Our attribute-based ODM automates database-level projections during compilation. By decorating a source-generated sub-class with projection attributes, you instruct MongoDB to execute micro-payload operations. This saves infrastructure resources across three core vectors:

### 1. Drastic Network Bandwidth Reduction

* **The Problem:** In document databases, a single record often contains embedded metadata, historical log arrays, or large text fields. Moving complete records to your application server saturates the network.
* **The Savings:** Projections shrink the data payload footprint down to bytes. This lowers total network transit times, prevents network interface card (NIC) throttling, and significantly slashes bandwidth costs if your database and application live in different cloud availability zones.

### 2. Reduced Application Heap Memory & GC Pressure

* **The Problem:** When an ODM fetches a standard MongoDB document, it allocates memory to map the entire JSON/BSON graph into a language object. If you only use two properties out of fifty, the rest of that allocated object structure immediately triggers high Garbage Collection (GC) churn.
* **The Savings:** Statically generated projection classes ensure your application runtime only instantiates fields that are actively required. This optimizes heap allocations, flattens memory spikes under high concurrent load, and minimizes application pauses caused by GC cleanup cycles.

### 3. Unlocking "Covered Queries" for Maximum Speed

* **The Problem:** Standard queries force MongoDB to scan an index, find the matching storage location on disk or within cache memory (WiredTiger), and fetch the complete document to return it.
* **The Savings:** If a query's filtering criteria and its requested projection fields are both included within a compound database index, MongoDB satisfies the entire operation directly from the index RAM. It completely skips the document retrieval step. This "Covered Query" pattern bypasses expensive disk I/O operations entirely, resulting in sub-millisecond execution speeds.

## Why Attribute-Driven Source Generation Wins
Traditional ODMs require developers to manually write type-unsafe projection mappings using dynamic strings or expressions. Our source-generated approach eliminates that developer overhead:

|Feature|Manual Projections|Our Source-Gened Projections|
|-------|------------------|----------------------------|
|Type Safety|Fragile string/expression parsing|Strongly typed models enforced at compile time|
|Refactoring|Prone to breaking when database names change|Safe. Renaming properties updates mapping logic|
|Developer Flow|Extra mapping code required for every query|Handled natively by decorating a class attribute|

By defining tailored, focused sub-classes for your application screens or API endpoints, you ensure your app operates at peak resource efficiency without writing complex aggregation or query building scripts.