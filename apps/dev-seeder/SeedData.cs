namespace SmartSolutionsLab.Roomy.DevSeeder;

// The fixed shape of the Obex Labs demo dataset: three offices with Star-Trek/Star-Wars-named rooms and 42
// colleagues spread across them (Hamburg deliberately tiny and rarely in). Reservation volume is generated
// from this in Seeder; the names/rooms live here so the dataset is easy to read and tweak.
internal static class SeedData
{
    public const string CompanyName = "Obex Labs";
    public const string EmailDomain = "obexlabs.com";

    public static readonly IReadOnlyList<OfficeSeed> Offices =
    [
        new("Nürnberg", "Nürnberg",
        [
            new("Enterprise", 12), new("Voyager", 10), new("Defiant", 8), new("Phoenix", 6), new("Galileo", 4),
        ]),
        new("München", "München",
        [
            new("Falcon", 10), new("Endor", 8), new("Hoth", 6), new("Naboo", 4),
        ]),
        new("Hamburg", "Hamburg",
        [
            new("Tatooine", 4), new("Dagobah", 3),
        ]),
    ];

    // 42 colleagues, a mix of Star Trek and Star Wars. Nürnberg (HQ) and München carry the company; Hamburg
    // has only four and they are rarely in (Seeder keeps Hamburg's occupancy near zero).
    public static readonly IReadOnlyList<EmployeeSeed> Employees =
    [
        // Nürnberg (20)
        new("Jean-Luc Picard", "Nürnberg"), new("William Riker", "Nürnberg"), new("Data", "Nürnberg"),
        new("Geordi LaForge", "Nürnberg"), new("Beverly Crusher", "Nürnberg"), new("Deanna Troi", "Nürnberg"),
        new("Worf", "Nürnberg"), new("Tasha Yar", "Nürnberg"), new("Miles O'Brien", "Nürnberg"),
        new("Guinan", "Nürnberg"), new("Luke Skywalker", "Nürnberg"), new("Leia Organa", "Nürnberg"),
        new("Han Solo", "Nürnberg"), new("Chewbacca", "Nürnberg"), new("Obi-Wan Kenobi", "Nürnberg"),
        new("Padmé Amidala", "Nürnberg"), new("Mace Windu", "Nürnberg"), new("Lando Calrissian", "Nürnberg"),
        new("Wedge Antilles", "Nürnberg"), new("Mon Mothma", "Nürnberg"),
        // München (18)
        new("Kathryn Janeway", "München"), new("Chakotay", "München"), new("Tuvok", "München"),
        new("Seven of Nine", "München"), new("B'Elanna Torres", "München"), new("Tom Paris", "München"),
        new("Harry Kim", "München"), new("Neelix", "München"), new("Benjamin Sisko", "München"),
        new("Kira Nerys", "München"), new("Jadzia Dax", "München"), new("Julian Bashir", "München"),
        new("Odo", "München"), new("Quark", "München"), new("Rey Skywalker", "München"),
        new("Finn Trooper", "München"), new("Poe Dameron", "München"), new("Ahsoka Tano", "München"),
        // Hamburg (4)
        new("James Kirk", "Hamburg"), new("Spock", "Hamburg"), new("Leonard McCoy", "Hamburg"),
        new("Cassian Andor", "Hamburg"),
    ];
}
