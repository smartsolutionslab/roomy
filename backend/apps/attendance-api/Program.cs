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

builder.Services.AddRoomyExceptionHandling();

var emittingOpenApiDocument = builder.Configuration.IsEmittingOpenApiDocument();

if (emittingOpenApiDocument)
{
    JasperFxEnvironment.AutoStartHost = true;
}

var messagingEnabled = builder.Configuration.GetValue("Messaging:Enabled", true);

if (!emittingOpenApiDocument && messagingEnabled)
{
    builder.AddRoomyMessaging(attendanceConnectionString, typeof(AttendanceApiHost).Assembly, typeof(RoomAddedConsumer).Assembly);
}

builder.Services.AddValidatedOptions<AttendanceApiOptions>(
    builder.Configuration,
    AttendanceApiOptions.SectionName,
    options => options.CompanyId != Guid.Empty,
    "Missing configuration 'Attendance:CompanyId'.");

var businessZone = BusinessTimeZone.Resolve(builder.Configuration.GetSection(AttendanceApiOptions.SectionName)["TimeZone"]);
builder.Services.AddSingleton<IBusinessClock>(serviceProvider => new BusinessClock(serviceProvider.GetRequiredService<TimeProvider>(), businessZone));

var (keycloakBaseAddress, keycloakRealm) = builder.Configuration.ReadKeycloak();

builder.Services.AddKeycloakJwtBearer(keycloakBaseAddress, keycloakRealm);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.UseAuthentication()
    .UseAuthorization();

app.MapReservationEndpoints()
    .MapOccupancyEndpoints()
    .MapRoomCatalogueEndpoints();

// Serves the document at /openapi/v1.json. The service is internal — the gateway has no /openapi
// route (ADR-0030) — so it is mapped in every environment for local tooling and the codegen emit.
app.MapOpenApi();

return await app.RunJasperFxCommands(args);
