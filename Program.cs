// See https://aka.ms/new-console-template for more information
using NaiveRedisVsPostgresBenchmark;
using System.Diagnostics;

Console.WriteLine("Naive Redis vs PostgreSQL benchmark");


RedisVsPostgres.NumberOfThreads = Environment.ProcessorCount;
RedisVsPostgres.NumberOfOperations = 100_000;

var redisVsPostgres = new RedisVsPostgres();

Console.WriteLine($"Running single-thread...");
redisVsPostgres.RunPostgresBenchmarkSingleThread(0, 10000);
redisVsPostgres.RunRedisBenchmarkSingleThread(0, 10000);

Console.WriteLine($"Running multi-thread...");
redisVsPostgres.RunPostgresBenchmarkMultiThread();
redisVsPostgres.RunRedisBenchmarkMultiThread();