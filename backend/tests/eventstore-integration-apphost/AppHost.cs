var builder = DistributedApplication.CreateBuilder(args);

// A minimal app host for the event store's concurrency-race integration test: it provisions only a
// PostgreSQL server and a database, so the EfCoreEventStore runs against the real provider whose unique
// (stream_id, version) index emits SQLSTATE 23505 on a true write race (#67) — a path SQLite cannot
// reproduce (ADR-0012). A throwaway container (no data volume, default session lifetime) isolates each run.
builder.AddPostgres("postgres").AddDatabase("eventstore");

builder.Build().Run();
