using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.UseCases;

public sealed record ChangeOfficeLocation(OfficeIdentifier OfficeIdentifier, Location Location) : ICommand;
