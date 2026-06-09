using System.Runtime.CompilerServices;
using JasperFx.CommandLine;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The attendance host dispatches startup through JasperFx (RunJasperFxCommands, ADR-0034) so the
// Wolverine codegen commands exist. Under WebApplicationFactory the entry point is invoked with no
// arguments; without this flag JasperFx would not actually start the web host, and the factory's
// TestServer would never be configured ("The server has not been started"). Setting it once for the
// whole test assembly makes the dispatcher start the host the factory then intercepts.
internal static class TestEnvironment
{
    [ModuleInitializer]
    public static void EnableHostStartUnderWebApplicationFactory() => JasperFxEnvironment.AutoStartHost = true;
}
