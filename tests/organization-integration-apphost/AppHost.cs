var builder = DistributedApplication.CreateBuilder(args);

// A minimal app host for the organization persistence integration tests: it provisions only a
// PostgreSQL server and the organization database — none of the rest of the stack — so the EF mapping
// (value converters, the owned Room collection, unique indexes) is verified against the real provider
// the app uses (ADR-0011/0012). A throwaway container keeps each test run isolated.
builder.AddPostgres("postgres").AddDatabase("organization");

builder.Build().Run();
