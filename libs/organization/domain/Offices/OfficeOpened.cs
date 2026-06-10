using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public sealed record OfficeOpened(
    OfficeIdentifier Office,
    CompanyIdentifier Company,
    OfficeName Name,
    Location Location) : IDomainEvent;
