namespace AtcDemo.Server;

using System.Globalization;
using AtcDemo.Shared;

public static class AtcExtensions
{
    public static Atc.Chemical ConvertFromProtobuf(this AtcChemical chemical)
    {
        var doses = chemical.Doses.Select(c => new Atc.Dose(
            c.DefinedDailyDose,
            c.AdministrationRoute,
            c.Unit));
        var level = new Atc.Levels(
            chemical.Level1AnatomicalMainGroup,
            chemical.Level2TherapeuticSubgroup,
            chemical.Level3PharmacologicalSubgroup,
            chemical.Level4ChemicalSubgroup,
            chemical.Level5ChemicalSubstance);
        var result = new Atc.Chemical(
            chemical.Code,
            chemical.Name,
            doses,
            level);
        return result;
    }

    public static AtcChemical ConvertFromRecord(this Atc.Chemical chemical)
    {
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        var doses = chemical.Doses.Select(c => new AtcDose()
        {
            DefinedDailyDose = Math.Round(c.DefinedDailyDose, 5, MidpointRounding.AwayFromZero),
            AdministrationRoute = c.AdministrationRoute,
            Unit = c.Unit
        });
        var result = new AtcChemical()
        {
            Code = chemical.Code,
            Name = textInfo.ToTitleCase(chemical.Name).Replace(" And ", " and "),
            Level1AnatomicalMainGroup = chemical.Levels.Level1AnatomicalMainGroup,
            Level2TherapeuticSubgroup = chemical.Levels.Level2TherapeuticSubgroup,
            Level3PharmacologicalSubgroup = chemical.Levels.Level3PharmacologicalSubgroup,
            Level4ChemicalSubgroup = chemical.Levels.Level4ChemicalSubgroup,
            Level5ChemicalSubstance = chemical.Levels.Level5ChemicalSubstance
        };
        result.Doses.AddRange(doses);
        return result;
    }
}
