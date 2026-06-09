using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Attendance.Api;
using SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The attendance context owns its database (ADR-0014); Aspire injects the connection string by name.
var attendanceConnectionString = builder.Configuration.GetConnectionString("attendance")
    ?? throw new InvalidOperationException("Missing connection string 'attendance'.");

builder.Services.AddAttendancePersistence(attendanceConnectionString);
builder.Services.AddAttendanceUseCases();

// Until organization's capacity feed lands (US2), no room is known to attendance, so a reservation is
// rejected as unknown_room. US2 replaces this with the Rooms read-model adapter.
builder.Services.AddScoped<IRoomDirectory, UnprovisionedRoomDirectory>();

// Single-tenant v1 (ADR-0011): the company that owns every AttendanceDay (ADR-0026) is configured.
var attendance = builder.Configuration.GetSection(AttendanceApiOptions.SectionName);
builder.Services.AddSingleton(new AttendanceApiOptions
{
    CompanyId = Guid.Parse(attendance["CompanyId"]
        ?? throw new InvalidOperationException("Missing configuration 'Attendance:CompanyId'.")),
});

// The attendance API is internal — reached only through the BFF, which forwards the Keycloak access
// token (ADR-0013). Validate it as a JWT bearer against the realm; the audience is not validated (the
// gateway gates access, and a Keycloak token's audience varies by client), but the issuer/realm must
// match. The BFF owns login/session.
var keycloak = builder.Configuration.GetSection("Keycloak");
var keycloakBaseAddress = new Uri(keycloak["BaseAddress"]
    ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
var keycloakRealm = keycloak["Realm"] ?? "roomy";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakBaseAddress.ToString().TrimEnd('/')}/realms/{keycloakRealm}";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapReservationEndpoints();

app.Run();
