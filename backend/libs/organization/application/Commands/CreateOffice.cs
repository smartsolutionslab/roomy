using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record CreateOffice(OfficeName Name, Location Location) : ICommand<OfficeIdentifier>;
