using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

// Create an office under the single seeded company. Returns the new office's identifier so the
// endpoint can answer 201 with its location.
public sealed record CreateOffice(OfficeName Name, Location Location) : ICommand<OfficeIdentifier>;
