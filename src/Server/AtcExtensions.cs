namespace AtcDemo.Server;

using System.Globalization;
using AtcDemo.Shared;

public static class AtcExtensions
{
    public static Atc.Classification ConvertFromProtobuf(this AtcClassification classification)
    {
        var doses = classification.Doses.Select(c => new Atc.Dose(
            c.DefinedDailyDose,
            c.AdministrationRoute,
            c.Unit));
        var level = new Atc.Levels(
            classification.Level1AnatomicalMainGroup,
            classification.Level2TherapeuticSubgroup,
            classification.Level3PharmacologicalSubgroup,
            classification.Level4ChemicalSubgroup,
            classification.Level5ChemicalSubstance);
        var result = new Atc.Classification(
            classification.Code,
            classification.Name,
            doses,
            level);
        return result;
    }

    public static AtcClassification ConvertFromRecord(this Atc.Classification classification)
    {
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        var doses = classification.Doses.Select(c => new AtcDose()
        {
            DefinedDailyDose = Math.Round(c.DefinedDailyDose, 5, MidpointRounding.AwayFromZero),
            AdministrationRoute = c.AdministrationRoute,
            Unit = c.Unit
        });
        var result = new AtcClassification()
        {
            Code = classification.Code,
            Name = textInfo.ToTitleCase(classification.Name).Replace(" And ", " and "),
            ModifiedTicks = DateTime.UtcNow.Ticks,
            Level1AnatomicalMainGroup = classification.Levels.Level1AnatomicalMainGroup,
            Level2TherapeuticSubgroup = classification.Levels.Level2TherapeuticSubgroup,
            Level3PharmacologicalSubgroup = classification.Levels.Level3PharmacologicalSubgroup,
            Level4ChemicalSubgroup = classification.Levels.Level4ChemicalSubgroup,
            Level5ChemicalSubstance = classification.Levels.Level5ChemicalSubstance
        };
        result.Doses.AddRange(doses);
        return result;
    }
}
