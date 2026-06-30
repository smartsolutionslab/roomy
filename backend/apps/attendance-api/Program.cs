using SmartSolutionsLab.Roomy.Attendance.Api;
using SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddRoomyApiDefaults();

var attendanceConnectionString = builder.Configuration.GetAttendanceConnectionString();
builder.Services.AddAttendancePersistence(attendanceConnectionString)
    .AddAttendanceUseCases();

var messagingEnabled = builder.Configuration.GetValue("Messaging:Enabled", true);

if (!builder.Configuration.IsEmittingOpenApiDocument() && messagingEnabled)
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

var app = builder.Build();

app.MapReservationEndpoints()
    .MapOccupancyEndpoints()
    .MapRoomCatalogueEndpoints();

return await app.UseRoomyApiPipeline(args);
