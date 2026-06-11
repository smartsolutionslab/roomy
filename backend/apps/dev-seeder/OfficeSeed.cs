namespace SmartSolutionsLab.Roomy.DevSeeder;

internal sealed record OfficeSeed(string Name, string Location, IReadOnlyList<RoomSeed> Rooms);
