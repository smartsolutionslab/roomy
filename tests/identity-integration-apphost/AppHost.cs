var builder = DistributedApplication.CreateBuilder(args);

// A minimal app host for the identity persistence integration tests: it provisions only a PostgreSQL
// server and the identity database — none of Keycloak, RabbitMQ, or the gateway — so the EF mapping is
// verified against the real provider the app uses (ADR-0011/0012). A throwaway container (no data
// volume, default session lifetime) keeps each test run isolated.
builder.AddPostgres("postgres").AddDatabase("identity");

builder.Build().Run();
