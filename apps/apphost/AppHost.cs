var builder = DistributedApplication.CreateBuilder(args);

// Local development infrastructure (issue #17). The app host provisions the backing
// services every context needs, so the full stack comes up with one command
// (`dotnet run --project apps/apphost`). No application/context services exist yet —
// they are wired in later issues (#18-#23) and will reference these resources by name.
//
// Credentials are Aspire parameters, never hard-coded secrets: they default to a
// generated value and can be overridden per environment via configuration / user
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
var postgresUser = builder.AddParameter("postgres-username", secret: false);
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", userName: postgresUser, password: postgresPassword)
    .WithDataVolume("roomy-postgres-data")
    .WithContainerName("roomy-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

// --- RabbitMQ (ADR-0015) -----------------------------------------------------------
// The default message broker for cross-service integration events. Aspire runs it
// locally regardless of the deployed transport. The management UI eases local
// debugging; a data volume preserves broker state across restarts.
var rabbitUser = builder.AddParameter("rabbitmq-username", secret: false);
var rabbitPassword = builder.AddParameter("rabbitmq-password", secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword)
    .WithDataVolume("roomy-rabbitmq-data")
    .WithContainerName("roomy-rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// --- Keycloak (ADR-0013) -----------------------------------------------------------
// The OIDC provider for the BFF security pattern. The dev realm (`roomy`, with roles
// `employee`/`administrator`) and the confidential `roomy-bff` client are imported from
// the gateway's realm file (#21). The gateway is the only OIDC client.
var keycloakUser = builder.AddParameter("keycloak-username", secret: false);
var keycloakPassword = builder.AddParameter("keycloak-password", secret: true);

var keycloak = builder.AddKeycloak("keycloak", adminUsername: keycloakUser, adminPassword: keycloakPassword)
    .WithDataVolume("roomy-keycloak-data")
    .WithContainerName("roomy-keycloak")
    .WithRealmImport("../gateway/keycloak")
    .WithLifetime(ContainerLifetime.Persistent);

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
    .WithExternalHttpEndpoints();

// Reference the resources so the app model and analyzers treat them as used until the
// context services that consume them are introduced (#18-#23).
_ = postgres;
_ = rabbitmq;
_ = gateway;

builder.Build().Run();
