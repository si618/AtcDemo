namespace AtcDemo.Shared;

/// <summary>
/// Respresentation of an ATC classification.
/// </summary>
/// <remarks>
/// Used for mapping of json file into SQLite database, WebAPI DTO and UI in Razor.
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
    public record Classification(string Code, string Name, IEnumerable<Dose> Doses, Levels Levels);

    /// <summary>
    /// The defined daily dose and administration route for a drug.
    /// </summary>
    /// <param name="DefinedDailyDose"></param>
    /// <param name="AdministrationRoute"></param>
    public record Dose(double DefinedDailyDose, string AdministrationRoute, string Unit);

    /// <summary>
    /// The five levels associated with the drug.
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
