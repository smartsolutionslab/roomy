using JasperFx;
using JasperFx.CommandLine;
using SmartSolutionsLab.Roomy.Attendance.Api;
using SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Authentication;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var attendanceConnectionString = builder.Configuration.GetAttendanceConnectionString();

builder.Services.AddAttendancePersistence(attendanceConnectionString)
    .AddAttendanceUseCases()
    .AddOpenApi(options => options.CreateSchemaReferenceId = EndpointSchemaIds.ForEndpointDto);
var emittingOpenApiDocument = builder.Configuration.GetValue<bool>("OpenApi:EmitDocument");

if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

var messagingEnabled = builder.Configuration.GetValue("Messaging:Enabled", true);

if (!emittingOpenApiDocument && messagingEnabled)
{
    builder.AddRoomyMessaging(
        new MessagingOptions
        {
            Transport = MessagingTransport.RabbitMq,
            PostgresConnectionString = attendanceConnectionString,
            ConnectionString = builder.Configuration.GetRabbitMqConnectionString(),
        },
        applicationAssembly: typeof(AttendanceApiHost).Assembly,
        typeof(RoomAddedConsumer).Assembly);
}

var attendance = builder.Configuration.GetSection(AttendanceApiOptions.SectionName);
builder.Services.AddSingleton(new AttendanceApiOptions
{
    CompanyId = Guid.Parse(attendance["CompanyId"] ?? throw new InvalidOperationException("Missing configuration 'Attendance:CompanyId'."))
});

var businessZone = BusinessTimeZone.Resolve(attendance["TimeZone"]);
builder.Services.AddSingleton<IBusinessClock>(serviceProvider => new BusinessClock(serviceProvider.GetRequiredService<TimeProvider>(), businessZone));

var keycloak = builder.Configuration.GetSection("Keycloak");
var keycloakBaseAddress = new Uri(keycloak["BaseAddress"] ?? throw new InvalidOperationException("Missing configuration 'Keycloak:BaseAddress'."));
var keycloakRealm = keycloak["Realm"] ?? "roomy";

builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication()
    .UseAuthorization();

app.MapReservationEndpoints()
    .MapOccupancyEndpoints()
    .MapRoomCatalogueEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

return await app.RunJasperFxCommands(args);
