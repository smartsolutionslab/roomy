var builder = DistributedApplication.CreateBuilder(args);

// A full-stack app host for the provisioning-saga e2e (ADR-0025): Postgres (per-service DBs), RabbitMQ
// (the real cross-service transport), Keycloak (the real credential provider with the production realm
// import), the schema runner, and the two saga participants — identity-api and organization-api. It is a
// trimmed copy of apps/apphost: no gateway, web, or attendance-api. Credentials are fixed dev/test values
// so the tests are deterministic; a throwaway run (no data volumes) isolates each test class.

var postgres = builder.AddPostgres(
    "postgres",
    userName: builder.AddParameter("postgres-username", "postgres"),
    password: builder.AddParameter("postgres-password", "saga-test-only-password"));

var identityDatabase = postgres.AddDatabase("identity");
var organizationDatabase = postgres.AddDatabase("organization");
// The migrator migrates all three contexts (it requires every connection string); attendance has no API
// here, but its schema is still rolled out so the runner completes.
var attendanceDatabase = postgres.AddDatabase("attendance");

var rabbitmq = builder.AddRabbitMQ(
    "rabbitmq",
    userName: builder.AddParameter("rabbitmq-username", "guest"),
    password: builder.AddParameter("rabbitmq-password", "saga-test-only-password"));

var keycloakUser = builder.AddParameter("keycloak-username", "admin");
var keycloakPassword = builder.AddParameter("keycloak-password", "admin-test-only-password");
var keycloak = builder.AddKeycloak("keycloak", adminUsername: keycloakUser, adminPassword: keycloakPassword)
    .WithRealmImport("../../apps/gateway/keycloak");

var dbMigrator = builder.AddProject<Projects.Roomy_DbMigrator>("db-migrator")
    .WithReference(identityDatabase).WaitFor(identityDatabase)
    .WithReference(organizationDatabase).WaitFor(organizationDatabase)
    .WithReference(attendanceDatabase).WaitFor(attendanceDatabase);

// Identity provisions Keycloak users and acks the saga; it seeds the DefaultAdmin at startup, whose
// credentials the test uses to authorize the hire.
_ = builder.AddProject<Projects.Roomy_Identity_Api>("identity-api")
    .WithHttpEndpoint()
    .WithReference(identityDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Keycloak__AdminUsername", keycloakUser)
    .WithEnvironment("Keycloak__AdminPassword", keycloakPassword)
    .WithEnvironment("DefaultAdmin__Email", "admin@roomy.local")
    .WithEnvironment("DefaultAdmin__DisplayName", "Default Admin")
    .WithEnvironment("DefaultAdmin__InitialPassword", "DevAdmin.23456");

// Organization owns the hire entry point and the Employee provisioning lifecycle.
_ = builder.AddProject<Projects.Roomy_Organization_Api>("organization-api")
    .WithHttpEndpoint()
    .WithReference(organizationDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Company__Name", "Roomy");

builder.Build().Run();
