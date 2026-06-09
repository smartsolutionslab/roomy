var builder = DistributedApplication.CreateBuilder(args);

// A minimal app host for the attendance persistence integration tests: it provisions only a PostgreSQL
// server and the attendance database — none of RabbitMQ, the gateway, or the other services — so the
// event store is exercised against the real provider the app uses (ADR-0012/0014). A throwaway
// container (no data volume, default session lifetime) keeps each test run isolated.
builder.AddPostgres("postgres").AddDatabase("attendance");

builder.Build().Run();
