var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres(
    "postgres",
    userName: builder.AddParameter("postgres-username", "postgres"),
    password: builder.AddParameter("postgres-password", "saga-test-only-password"));

var identityDatabase = postgres.AddDatabase("identity");
var organizationDatabase = postgres.AddDatabase("organization");
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

_ = builder.AddProject<Projects.Roomy_Identity_Api>("identity-api")
    .WithHttpEndpoint()
    .WithReference(identityDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Keycloak__AdminUsername", keycloakUser)
    .WithEnvironment("Keycloak__AdminPassword", keycloakPassword)
    .WithCredentialEncryption();

_ = builder.AddProject<Projects.Roomy_Organization_Api>("organization-api")
    .WithHttpEndpoint()
    .WithReference(organizationDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Company__Name", "Roomy")
    .WithEnvironment("DefaultAdmin__Email", "admin@roomy.local")
    .WithEnvironment("DefaultAdmin__DisplayName", "Default Admin")
    .WithEnvironment("DefaultAdmin__InitialPassword", "DevAdmin.23456")
    .WithCredentialEncryption();

// attendance-api also consumes EmployeeHired (into its directory): including it here makes the saga
// stack a faithful copy of production, so the per-service-queue fan-out (#189) is exercised end to end.
_ = builder.AddProject<Projects.Roomy_Attendance_Api>("attendance-api")
    .WithHttpEndpoint()
    .WithReference(attendanceDatabase).WaitForCompletion(dbMigrator)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Keycloak__BaseAddress", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Realm", "roomy")
    .WithEnvironment("Attendance__CompanyId", "0199a0b0-0000-7000-8000-000000000001");

builder.Build().Run();

internal static class CredentialEncryptionExtensions
{
    // Organization encrypts the initial credential and identity decrypts it with the SAME key (ADR-0063),
    // so the whole saga stack shares one well-known test key id + key.
    public static IResourceBuilder<ProjectResource> WithCredentialEncryption(this IResourceBuilder<ProjectResource> builder) =>
        builder
            .WithEnvironment("CredentialEncryption__ActiveKeyId", "test")
            .WithEnvironment("CredentialEncryption__Keys__test", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
}
