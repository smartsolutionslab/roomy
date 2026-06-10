var builder = DistributedApplication.CreateBuilder(args);

var adminUsername = builder.AddParameter("keycloak-username", "admin");
var adminPassword = builder.AddParameter("keycloak-password", "admin-test-only-password");

builder.AddKeycloak("keycloak", adminUsername: adminUsername, adminPassword: adminPassword)
    .WithRealmImport("../../apps/gateway/keycloak");

builder.Build().Run();
