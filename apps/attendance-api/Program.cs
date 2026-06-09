using JasperFx;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Attendance.Api;
using SmartSolutionsLab.Roomy.Attendance.Api.Authentication;
using SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The attendance context owns its database (ADR-0014); Aspire injects the connection string by name.
var attendanceConnectionString = builder.Configuration.GetConnectionString("attendance")
    ?? throw new InvalidOperationException("Missing connection string 'attendance'.");

builder.Services.AddAttendancePersistence(attendanceConnectionString);
builder.Services.AddAttendanceUseCases();

// Capacity comes from the local Rooms read model, fed by organization's RoomAdded (ADR-0014/0037).
builder.Services.AddScoped<IRoomDirectory, RoomDirectory>();

// The acting user resolves to their EmployeeId via the local Employees read model, fed by EmployeeHired
// (003 US4). Used by the endpoints to authorize reserve/cancel.
builder.Services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();

// Occupancy reads the local Reservations/Rooms/Offices/Employees read models for the view (004 US6,
// ADR-0038) — no cross-service join.
builder.Services.AddScoped<IOccupancyReadModel, OccupancyReadModel>();

// "My reservations" reads the caller's rows from the local Reservations read model (004 US9).
builder.Services.AddScoped<IMyReservationsReadModel, MyReservationsReadModel>();

// Wolverine's durable transactional inbox over the attendance database, RabbitMQ transport
// (ADR-0005/0015). It consumes organization's RoomAdded (the RoomAddedConsumer in the infrastructure
// assembly) into the Rooms read model; the inbox shares the attendance database so the projection and
// the dedup commit together. No outbound integration events this slice (occupancy folds the stream
// locally, 004).
builder.AddRoomyMessaging(
    new MessagingOptions
    {
        Transport = MessagingTransport.RabbitMq,
        PostgresConnectionString = attendanceConnectionString,
        ConnectionString = builder.Configuration.GetConnectionString("rabbitmq")
            ?? throw new InvalidOperationException("Missing connection string 'rabbitmq'."),
    },
    applicationAssembly: typeof(AttendanceApiHost).Assembly,
    typeof(RoomAddedConsumer).Assembly);

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

        // Keycloak nests realm roles under realm_access.roles; flatten them to role claims so the
        // reserve/cancel endpoints can authorize the administrator on-behalf path (FR-011, ADR-0013).
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                KeycloakRealmRoles.AddRoleClaims(context.Principal);
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapReservationEndpoints();
app.MapOccupancyEndpoints();

// RunJasperFxCommands so the Wolverine code-generation commands are available (ADR-0034): the host runs
// from committed, pre-generated code (TypeLoadMode.Static). With no arguments it just runs the host.
return await app.RunJasperFxCommands(args);
