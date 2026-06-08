var builder = DistributedApplication.CreateBuilder(args);

// A minimal app host for the Keycloak admin-adapter integration tests: only Keycloak, with the
// production realm import (roles employee/administrator, unique email, length(8) password policy) so
// the adapter runs against the same provider configuration the app uses. Fixed dev/test admin
// credentials keep the admin REST calls deterministic; a throwaway container isolates each run.
var adminUsername = builder.AddParameter("keycloak-username", "admin");
var adminPassword = builder.AddParameter("keycloak-password", "admin-test-only-password");

builder.AddKeycloak("keycloak", adminUsername: adminUsername, adminPassword: adminPassword)
    .WithRealmImport("../../apps/gateway/keycloak");

builder.Build().Run();
