using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record RenameOffice(OfficeIdentifier OfficeIdentifier, OfficeName Name) : ICommand;
