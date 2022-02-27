namespace AtcDemo.Shared;

/// <summary>
/// Respresentation of an ATC classification.
/// </summary>
/// <remarks>
/// Used for mapping of ATC.json schema into SQLite database, WebAPI DTO and display in Razor.
/// </remarks>
public static class Atc
{
    /// <summary>
    /// The drug being classified.
    /// </summary>
    /// <param name="Code"></param>
    /// <param name="Name"></param>
    /// <param name="Doses"></param>
    /// <param name="Levels"></param>
    public record Chemical(string Code, string Name, IEnumerable<Dose> Doses, Levels Levels);

    /// <summary>
    /// Each drug can have zero or more doses defined.
    /// </summary>
    /// <param name="DefinedDailyDose"></param>
    /// <param name="AdministrationRoute"></param>
    public record Dose(double DefinedDailyDose, string AdministrationRoute);

    /// <summary>
    /// The different groups associated with the drug.
    /// </summary>
    /// <param name="Level1AnatomicalMainGroup"></param>
    /// <param name="Level2TherapeuticSubgroup"></param>
    /// <param name="Level3PharmacologicalSubgroup"></param>
    /// <param name="Level4ChemicalSubgroup"></param>
    /// <param name="Level5ChemicalSubstance"></param>
    public record Levels(
        string Level1AnatomicalMainGroup,
        string Level2TherapeuticSubgroup,
        string Level3PharmacologicalSubgroup,
        string Level4ChemicalSubgroup,
        string Level5ChemicalSubstance);
}
