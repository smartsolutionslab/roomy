using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-username", "postgres", publishValueAsDefault: true);
var postgresPassword = builder.AddParameter(
    "postgres-password", GeneratedSecret(), secret: true, persist: true);

var postgres = builder.AddPostgres("postgres", userName: postgresUser, password: postgresPassword)
    .WithDataVolume("roomy-postgres-data")
    .WithContainerName("roomy-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var identityDatabase = postgres.AddDatabase("identity");

var organizationDatabase = postgres.AddDatabase("organization");

var attendanceDatabase = postgres.AddDatabase("attendance");

var rabbitUser = builder.AddParameter("rabbitmq-username", "guest", publishValueAsDefault: true);
var rabbitPassword = builder.AddParameter(
    "rabbitmq-password", GeneratedSecret(), secret: true, persist: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword)
    .WithDataVolume("roomy-rabbitmq-data")
    .WithContainerName("roomy-rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var keycloakUser = builder.AddParameter("keycloak-username", "admin", publishValueAsDefault: true);
var keycloakPassword = builder.AddParameter(
    "keycloak-password", GeneratedSecret(), secret: true, persist: true);

// Shared password for the seeded demo accounts (DefaultAdmin + seeded employees). A fixed, well-known
// default so local sign-in is predictable; override per environment via the `demo-password` parameter.
var demoPassword = builder.AddParameter("demo-password", "Test1234!", secret: true);

// The well-known seeded company id, shared by the attendance context and the dev seeder.
const string SeedCompanyId = "0199a0b0-0000-7000-8000-000000000001";

var keycloak = builder.AddKeycloak("keycloak", adminUsername: keycloakUser, adminPassword: keycloakPassword)
    .WithDataVolume("roomy-keycloak-data")
    .WithContainerName("roomy-keycloak")
    .WithRealmImport("../gateway/keycloak")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddExecutable(
        "web",
        OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm",
        "../../..",
        "nx", "serve", "web", "--port", "4200", "--host", "127.0.0.1")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithExplicitStart();

var dbMigrator = builder.AddProject<Projects.Roomy_DbMigrator>("db-migrator")
    .WithReference(identityDatabase).WaitFor(identityDatabase)
    .WithReference(organizationDatabase).WaitFor(organizationDatabase)
    .WithReference(attendanceDatabase).WaitFor(attendanceDatabase);

var identityApi = builder.AddProject<Projects.Roomy_Identity_Api>("identity-api")
    .WithHttpEndpoint()
    .WithReference(identityDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithKeycloakConnection(keycloak)
    .WithEnvironment("Keycloak__AdminUsername", keycloakUser)
    .WithEnvironment("Keycloak__AdminPassword", keycloakPassword);

// Wait for Keycloak so organization-api starts alongside identity-api (which also waits for it),
// not 30-60s earlier: the startup DefaultAdminSeeder must not publish EmployeeHired before identity
// has declared its queue binding, or the fanout exchange drops it and the admin never provisions (#189).
var organizationApi = builder.AddProject<Projects.Roomy_Organization_Api>("organization-api")
    .WithHttpEndpoint()
    .WithReference(organizationDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithKeycloakConnection(keycloak)
    .WithEnvironment("Company__Name", "Roomy")
    .WithEnvironment("DefaultAdmin__Email", "admin@roomy.local")
    .WithEnvironment("DefaultAdmin__DisplayName", "Default Admin")
    .WithEnvironment("DefaultAdmin__InitialPassword", demoPassword);

var attendanceApi = builder.AddProject<Projects.Roomy_Attendance_Api>("attendance-api")
    .WithHttpEndpoint()
    .WithReference(attendanceDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak)
    .WithKeycloakConnection(keycloak)
    .WithEnvironment("Attendance__CompanyId", SeedCompanyId);

_ = builder.AddProject<Projects.Roomy_DevSeeder>("dev-seeder")
    .WithExplicitStart()
    .WithReference(identityDatabase)
    .WithReference(organizationDatabase)
    .WithReference(attendanceDatabase)
    .WithReference(keycloak).WaitFor(keycloak)
    .WaitForCompletion(dbMigrator)
    .WithKeycloakConnection(keycloak)
    .WithEnvironment("Keycloak__AdminUsername", keycloakUser)
    .WithEnvironment("Keycloak__AdminPassword", keycloakPassword)
    .WithEnvironment("Seed__CompanyId", SeedCompanyId)
    .WithEnvironment("Seed__EmployeePassword", demoPassword);

_ = builder.AddScalarApiReference()
    .WithApiReference(identityApi)
    .WithApiReference(organizationApi)
    .WithApiReference(attendanceApi);

foreach (var api in new[] { identityApi, organizationApi, attendanceApi })
{
    api.WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation
    {
        Url = "/openapi/v1.json",
        DisplayText = "OpenAPI",
    });
}

var gateway = builder.AddProject<Projects.Roomy_Gateway>("gateway")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WithEnvironment("Authentication__Keycloak__ClientSecret", "dev-only-bff-secret-change-me")
    .WithReference(identityApi)
    .WaitFor(identityApi)
    .WithReference(organizationApi)
    .WaitFor(organizationApi)
    .WithReference(attendanceApi)
    .WaitFor(attendanceApi)
    .WithExternalHttpEndpoints();

// The BFF login only completes over HTTPS (__Host- session cookie + SameSite=None form_post correlation
// cookie both require Secure), so the gateway redirects plain-http callers to its external HTTPS port.
// Behind the Aspire/DCP proxy Kestrel can't infer that port, so feed it in from the endpoint itself.
gateway.WithEnvironment("HttpsRedirection__HttpsPort", gateway.GetEndpoint("https").Property(EndpointProperty.Port));

_ = gateway;

builder.Build().Run();

static GenerateParameterDefault GeneratedSecret() => new() { MinLength = 24, Special = false };

internal static class AppHostKeycloakExtensions
{
    public const string Realm = "roomy";

    public static IResourceBuilder<ProjectResource> WithKeycloakConnection(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<KeycloakResource> keycloak) =>
        builder
            .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
            .WithEnvironment("Keycloak__Realm", Realm);
}
