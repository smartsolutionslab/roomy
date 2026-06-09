using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// Raised when an office is created (ADR-0032). Intra-context and framework-free — it carries the
// office's value objects. The infrastructure unit of work drains it at commit and maps it to the
// OfficeOpened integration contract for the attendance capacity feed (ADR-0037, 003 US2).
public sealed record OfficeOpened(
    OfficeIdentifier Office,
    CompanyIdentifier Company,
    OfficeName Name,
    Location Location) : IDomainEvent;
