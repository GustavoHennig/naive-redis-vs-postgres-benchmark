# Naive Redis vs PostgreSQL Benchmark

This repository contains a simple benchmark to compare **Redis** and **PostgreSQL (unlogged table)** for a cache-like scenario. The benchmark evaluates common operations such as `INSERT`, `SELECT`, `UPDATE`, and `DELETE` using both single-threaded and multi-threaded approaches.

## Features
- Benchmarks **Redis** using the `StackExchange.Redis` library.
- Benchmarks **PostgreSQL unlogged tables** using the `Npgsql` library.
- Configurable number of operations and threads.
- Measures total execution time for each operation type.

## Prerequisites
- **Redis** and **PostgreSQL** servers running and accessible.
- .NET 6 SDK or later installed.

## Configuration
### PostgreSQL Setup
Ensure your PostgreSQL server is running and accessible. The benchmark uses the following table schema:
```sql
CREATE UNLOGGED TABLE benchmark_table_77 (
    key         TEXT PRIMARY KEY NOT NULL,
    value       TEXT NOT NULL,
    changed_at  timestamptz NULL
);
```

Update the connection string in the code to match your PostgreSQL server credentials:
```csharp
private static string PostgresConnectionString = "Server=192.168.0.140;Port=5432;Database=postgres;User Id=postgres;Password=;Maximum Pool Size=100;Minimum Pool Size=11;";
```

### Redis Setup
Ensure your Redis server is running and accessible. Update the connection string in the code to match your Redis server:
```csharp
private static ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect("192.168.0.140");
```

## Usage
1. Clone this repository:
   ```bash
   git clone https://github.com/GustavoHennig/naive-redis-vs-postgres-benchmark.git
   cd naive-redis-vs-postgres-benchmark
   ```

2. Build and run the project:
   ```bash
   dotnet run --configuration Release
   ```

3. The program will execute the benchmarks for both Redis and PostgreSQL in:
   - **Single-threaded mode**: Perform operations sequentially.
   - **Multi-threaded mode**: Perform operations concurrently using multiple threads.

### Configurable Parameters
You can adjust the following parameters in `RedisVsPostgres`:
- **Number of Operations**: Total operations to perform.
- **Number of Threads**: Number of threads for multi-threaded benchmarks (default: number of CPU cores).

Example:
```csharp
RedisVsPostgres.NumberOfThreads = 4;
RedisVsPostgres.NumberOfOperations = 50_000;
```

## Output
The benchmark will print execution times for `INSERT`, `SELECT`, `UPDATE`, and `DELETE` operations for both Redis and PostgreSQL, along with total time taken.

## Benchmark Results

- Redis on Fedora with default settings
- PostgreSQL 16 on Fedora with `shared_buffers = 2028MB`
- Running on a 24-core machine
- The server is running in another machine on the same network

```
Running single-thread...
PostgreSQL Insert: 1707 ms
PostgreSQL Select: 1549 ms
PostgreSQL Update: 1690 ms
PostgreSQL Delete: 1573 ms
PostgreSQL summary
Ops: 10000; Threads: 1; Total time: 6530 ms
Redis Insert: 1491 ms
Redis Read: 1427 ms
Redis Update: 1386 ms
Redis Delete: 1355 ms
Redis summary
Ops: 10000; Threads: 1; Total time: 5662 ms


Running multi-thread...
PostgreSQL
Ops: 100000; Threads: 24; Total time: 2880 ms

Redis
Ops: 100000; Threads: 24; Total time: 2618 ms

```

## Conclusion
I tested several configurations, and the results were consistent. PostgreSQL was occasionally even faster than Redis, but their performance was generally similar.

## Notes
- **PostgreSQL unlogged tables** are used for higher write performance but do not provide crash safety.
- The benchmark is a simple comparison and do not reflect real-world scenarios.
- The benchmark does not account for cache expiration or complex query scenarios.
- Thanks for this beautiful README file, ChatGPT!
