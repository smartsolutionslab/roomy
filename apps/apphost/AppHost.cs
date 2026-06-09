var builder = DistributedApplication.CreateBuilder(args);

// Local development infrastructure (issue #17). The app host provisions the backing
// services every context needs, so the full stack comes up with one command
// (`dotnet run --project apps/apphost`). No application/context services exist yet —
// they are wired in later issues (#18-#23) and will reference these resources by name.
//
// Credentials are Aspire parameters, never hard-coded secrets: passwords default to a
// generated value persisted to user secrets (stable across runs), usernames to a stable
// default, and any of them can be overridden per environment via configuration / user
// secrets (e.g. `Parameters:postgres-password`). See https://aka.ms/aspire/parameters.

// --- PostgreSQL (ADR-0011, ADR-0012) -----------------------------------------------
// A single Postgres server for local dev. Contexts are database-per-service
// (ADR-0014), so each future context API adds its OWN database off this server rather
// than sharing one — e.g. once the identity API exists:
//
//     var identityDb = postgres.AddDatabase("identity");
//     builder.AddProject<Projects.Roomy_Identity_Api>("identity-api")
//            .WithReference(identityDb).WaitFor(identityDb);
//
// `organization` and `attendance` follow the same pattern. A persistent volume and a
// stable container name keep dev data across restarts.
var postgresUser = builder.AddParameter("postgres-username", "postgres", publishValueAsDefault: true);
var postgresPassword = builder.AddParameter(
    "postgres-password", GeneratedSecret(), secret: true, persist: true);

var postgres = builder.AddPostgres("postgres", userName: postgresUser, password: postgresPassword)
    .WithDataVolume("roomy-postgres-data")
    .WithContainerName("roomy-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

// The identity context's own database on the shared server (database-per-service, ADR-0014). Each
// future context adds its own database the same way.
var identityDatabase = postgres.AddDatabase("identity");

// The organization context's own database on the shared server (database-per-service, ADR-0014).
var organizationDatabase = postgres.AddDatabase("organization");

// --- RabbitMQ (ADR-0015) -----------------------------------------------------------
// The default message broker for cross-service integration events. Aspire runs it
// locally regardless of the deployed transport. The management UI eases local
// debugging; a data volume preserves broker state across restarts.
var rabbitUser = builder.AddParameter("rabbitmq-username", "guest", publishValueAsDefault: true);
var rabbitPassword = builder.AddParameter(
    "rabbitmq-password", GeneratedSecret(), secret: true, persist: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword)
    .WithDataVolume("roomy-rabbitmq-data")
    .WithContainerName("roomy-rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// --- Keycloak (ADR-0013) -----------------------------------------------------------
// The OIDC provider for the BFF security pattern. The dev realm (`roomy`, with roles
// `employee`/`administrator`) and the confidential `roomy-bff` client are imported from
// the gateway's realm file (#21). The gateway is the only OIDC client.
var keycloakUser = builder.AddParameter("keycloak-username", "admin", publishValueAsDefault: true);
var keycloakPassword = builder.AddParameter(
    "keycloak-password", GeneratedSecret(), secret: true, persist: true);

var keycloak = builder.AddKeycloak("keycloak", adminUsername: keycloakUser, adminPassword: keycloakPassword)
    .WithDataVolume("roomy-keycloak-data")
    .WithContainerName("roomy-keycloak")
    .WithRealmImport("../gateway/keycloak")
    .WithLifetime(ContainerLifetime.Persistent);

// --- Angular SPA (ADR-0030) --------------------------------------------------------
// The gateway is the single browser-facing origin; it proxies the non-API routes to this
// Angular dev server (hot reload in dev). Hosted via core Aspire AddExecutable — the NodeJs
// hosting integration still lags the 13.x line (ADR-0030, ADR-0027). A fixed dev port keeps
// the gateway's proxy target and the OIDC redirect URIs stable.
builder.AddExecutable(
        "web",
        OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm",
        "../..",
        "nx", "serve", "web", "--port", "4200", "--host", "127.0.0.1")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false);

// --- Database migration runner (ADR-0033) ------------------------------------------
// Creates each context's database and applies its EF migrations in one pass, then exits. The context
// APIs gate on its completion instead of self-migrating, so the schema is in place before any service
// reads it. Each new context adds its own database reference here.
var dbMigrator = builder.AddProject<Projects.Roomy_DbMigrator>("db-migrator")
    .WithReference(identityDatabase).WaitFor(identityDatabase)
    .WithReference(organizationDatabase).WaitFor(organizationDatabase);

// --- Identity API (001) ------------------------------------------------------------
// The identity context service: it owns its database, provisions Keycloak users, and exposes the
// account/role surface the BFF composes (ADR-0013/0014). It validates the BFF-forwarded token against
// Keycloak and publishes integration events over RabbitMQ. It starts only after the migrator has applied
// the schema (WaitForCompletion, ADR-0033); the DefaultAdmin is then seeded at startup from these dev
// credentials, so the system is administrable from first run (FR-004); the admin REST calls reuse the
// Keycloak admin parameters. Dev-only credentials — overridden per environment.
var identityApi = builder.AddProject<Projects.Roomy_Identity_Api>("identity-api")
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

// --- Organization API (002) --------------------------------------------------------
// The organization context service: it owns its database and exposes the office/room admin surface
// (ADR-0014). It validates the BFF-forwarded token against Keycloak to authorize (no admin
// provisioning — it only reads roles). It starts after the migrator has applied the schema
// (WaitForCompletion, ADR-0033); the single company is then seeded at startup so offices have a company
// to belong to (research.md D2). No RabbitMQ — this slice publishes and consumes nothing.
var organizationApi = builder.AddProject<Projects.Roomy_Organization_Api>("organization-api")
    .WithReference(organizationDatabase).WaitForCompletion(dbMigrator)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Company__Name", "Roomy");

// --- YARP gateway / BFF (ADR-0013, ADR-0018) ---------------------------------------
// The single public entry point. It is the confidential OIDC client (BFF security
// pattern): it holds the session server-side, hands the browser only a cookie, and
// composes the internal context APIs as they come online (#001+). It references Keycloak
// for OIDC and waits for it. The dev BFF client secret matches the realm import; in real
// environments it is supplied via configuration / user secrets, never hard-coded.
var gateway = builder.AddProject<Projects.Roomy_Gateway>("gateway")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WithEnvironment("Authentication__Keycloak__ClientSecret", "dev-only-bff-secret-change-me")
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithReference(organizationApi)
    .WaitFor(organizationApi)
    .WithExternalHttpEndpoints();

// The gateway is the app's external entry point and is not referenced by another resource; the
// discard keeps it from tripping the unused-variable analyzer.
_ = gateway;

builder.Build().Run();

// Dev-only generator for the local infrastructure passwords: long enough to be safe, with no
// special characters so the value drops cleanly into connection strings and URLs.
static GenerateParameterDefault GeneratedSecret() => new() { MinLength = 24, Special = false };
