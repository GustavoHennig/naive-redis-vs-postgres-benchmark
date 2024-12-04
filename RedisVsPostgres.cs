using Npgsql;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveRedisVsPostgresBenchmark
{
    public class RedisVsPostgres
    {
        public static int NumberOfOperations = 100000;
        public static int NumberOfThreads = 10;

        private static string PostgresConnectionString = $"Server=192.168.0.140;Port=5432;Database=postgres;User Id=postgres;Password=;Maximum Pool Size=100;Minimum Pool Size={Environment.ProcessorCount + 1};";
        private static ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect("192.168.0.140");

        private string[] PrepopulatedRandomStringValues = new string[NumberOfOperations];

        // * You can call TRUNCATE TABLE benchmark_table_77 instead of recreating it.
        // * The _77 suffix is just a random number to avoid accidents.
        // * The field "changed_at" is required for cache expiration.
        private string PostgresPreRunQuery = @"
DROP TABLE IF EXISTS benchmark_table_77;
CREATE UNLOGGED TABLE benchmark_table_77 (
    key         TEXT PRIMARY KEY NOT NULL,
    value       TEXT NOT NULL,
    changed_at  timestamptz null
);
";

        public RedisVsPostgres()
        {
            for (int i = 0; i < PrepopulatedRandomStringValues.Length; i++)
            {
                PrepopulatedRandomStringValues[i] = Guid.NewGuid().ToString();
            }
        }

        public void RunPostgresBenchmarkMultiThread()
        {
            using var conn = new NpgsqlConnection(PostgresConnectionString);

            conn.Open();
            using var cmd = new NpgsqlCommand(PostgresPreRunQuery, conn);

            cmd.ExecuteNonQuery();
            var stopwatch = Stopwatch.StartNew();

            RunInParallel(NumberOfOperations, NumberOfThreads, RunPostgresOperationsSingleThread);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"PostgreSQL summary");
            Console.Write($"Ops: {NumberOfOperations}; Threads: {NumberOfThreads}; Total time: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
        }

        public void RunPostgresBenchmarkSingleThread(int idIni, int idMax)
        {
            using var conn = new NpgsqlConnection(PostgresConnectionString);

            conn.Open();
            using var cmd = new NpgsqlCommand(PostgresPreRunQuery, conn);
            cmd.ExecuteNonQuery();
            var stopwatch = Stopwatch.StartNew();

            RunPostgresOperationsSingleThread(idIni, idMax);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"PostgreSQL summary");
            Console.Write($"Ops: {idMax - idIni}; Threads: 1; Total time: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
        }

        private void RunInParallel(int numberOfOperations, int concurrentOperations, Action<int, int> operationToExecuteInParallel)
        {
            int rangeSize = numberOfOperations / concurrentOperations;
            var actions = new List<Action>();

            for (int i = 0; i < concurrentOperations; i++)
            {
                int start = i * rangeSize;
                int end = start + rangeSize;
                actions.Add(() => operationToExecuteInParallel(start, end));
            }

            Parallel.Invoke(actions.ToArray());
        }

        private void RunPostgresOperationsSingleThread(int idIni, int idMax)
        {
            using var conn = new NpgsqlConnection(PostgresConnectionString);

            conn.Open();

            var stopwatch = Stopwatch.StartNew();

            for (int i = idIni; i < idMax; i++)
            {
                using (var insertCmd = new NpgsqlCommand("INSERT INTO benchmark_table_77 (key, value, changed_at) VALUES (@key, @value, @now)", conn))
                {
                    insertCmd.Parameters.AddWithValue("key", $"key{i}");
                    insertCmd.Parameters.AddWithValue("value", PrepopulatedRandomStringValues[i]);
                    insertCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
                    insertCmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"PostgreSQL Insert: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            for (int i = idIni; i < idMax; i++)
            {
                using (var selectCmd = new NpgsqlCommand("SELECT value FROM benchmark_table_77 WHERE key = @key", conn))
                {
                    selectCmd.Parameters.AddWithValue("key", $"key{i}");
                    selectCmd.ExecuteScalar();
                }
            }

            Console.WriteLine($"PostgreSQL Select: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            for (int i = idIni; i < idMax; i++)
            {
                using (var updateCmd = new NpgsqlCommand("UPDATE benchmark_table_77 SET value = @value, changed_at = @now WHERE key = @key", conn))
                {
                    updateCmd.Parameters.AddWithValue("key", $"key{i}");
                    updateCmd.Parameters.AddWithValue("value", $"updated_value{i}");
                    updateCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
                    updateCmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"PostgreSQL Update: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            for (int i = idIni; i < idMax; i++)
            {
                using (var deleteCmd = new NpgsqlCommand("DELETE FROM benchmark_table_77 WHERE key = @key", conn))
                {
                    deleteCmd.Parameters.AddWithValue("key", $"key{i}");
                    deleteCmd.ExecuteNonQuery();
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"PostgreSQL Delete: {stopwatch.ElapsedMilliseconds} ms");
        }

        public void RunRedisBenchmarkMultiThread()
        {
            var db = RedisConnection.GetDatabase();

            var stopwatch = Stopwatch.StartNew();

            RunInParallel(NumberOfOperations, NumberOfThreads, (min, max) => RunRedisOperationsSingleThread(db, min, max));

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Redis summary");
            Console.Write($"Ops: {NumberOfOperations}; Threads: {NumberOfThreads};");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Total time: {stopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
        }

        public void RunRedisBenchmarkSingleThread(int idIni, int idMax)
        {
            var db = RedisConnection.GetDatabase();

            var stopwatch = Stopwatch.StartNew();

            RunRedisOperationsSingleThread(db, idIni, idMax);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Redis summary");
            Console.Write($"Ops: {idMax - idIni}; Threads: 1;");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Total time: {stopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
        }

        private void RunRedisOperationsSingleThread(IDatabase db, int idIni, int idMax)
        {
            var stopwatch = Stopwatch.StartNew();

            // INSERT
            for (int i = idIni; i < idMax; i++)
            {
                string key = $"key{i}";
                db.StringSet(key, PrepopulatedRandomStringValues[i]);
            }

            Console.WriteLine($"Redis Insert: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // READ
            for (int i = idIni; i < idMax; i++)
            {
                string key = $"key{i}";
                db.StringGet(key);
            }
            Console.WriteLine($"Redis Read: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // UPDATE
            for (int i = idIni; i < idMax; i++)
            {
                string key = $"key{i}";
                db.StringSet(key, $"updated_value{i}");
            }

            Console.WriteLine($"Redis Update: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // DELETE
            for (int i = idIni; i < idMax; i++)
            {
                string key = $"key{i}";
                db.KeyDelete(key);
            }

            stopwatch.Stop();
            Console.WriteLine($"Redis Delete: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}