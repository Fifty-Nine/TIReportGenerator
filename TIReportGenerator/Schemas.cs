using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator {
public static class Schemas
{
    private static string FormatSmallNumber<T>(T v)
    {
        return TIUtilities.FormatSmallNumber(Convert.ToDouble(v));
    }

    private static string PlayerDisplayName<T>(T obj, string nullText = "<null>") where T : TIGameState
    {
        return obj?.GetDisplayName(GameControl.control.activePlayer) ?? nullText;
    }

    public static IEnumerable<CouncilorAttribute> AllCouncilorAttributes()
    {
        return Enum.GetValues(typeof(CouncilorAttribute))
                   .Cast<CouncilorAttribute>()
                   .Where(a => a != CouncilorAttribute.None);
    }

    private static Dictionary<FactionResource, float> GetCouncilorIncomes(TICouncilorState councilor)
    {
        return Util.GameEnums.AllFactionResources().Select(r => (r, councilor.GetMonthlyIncome(r)))
                                    .Where(rp => rp.Item2 > 0.05f || rp.Item2 < -0.05f)
                                    .ToDictionary(rp => rp.Item1, rp => rp.Item2);
    }

    private static string FormatIncomes(Dictionary<FactionResource, float> incomes)
    {
        return FormatList(incomes, kvp => $"{(kvp.Value > 0 ? "+" : "")}{FormatSmallNumber(kvp.Value)} {TIUtilities.GetResourceString(kvp.Key)}");
    }

    public static string FormatList<T>(IEnumerable<T> list, Func<T, string> formatter = null)
    {
        formatter ??= o => o.ToString();
        return string.Join(", ", list.Select(formatter));
    }

    public enum CouncilorStatElement
    {
        Total,
        Base,
        FromTraits,
        FromOrgs,
        Cap
    };

    public static Dictionary<CouncilorStatElement, float> ComputeCouncilorStatValues(TICouncilorState c, CouncilorAttribute attr)
    {
        var baseVal = c.attributes[attr];
        var fromTraits = c.GetAttribute(attr, includeOrgs: false, includeAllUnconditionalTraits: true) - baseVal;
        var total = c.GetAttribute(attr, includeOrgs: true, includeAllUnconditionalTraits: true);
        var fromOrgs = total - fromTraits - baseVal;
        List<(CouncilorStatElement, float)> result = [
            (CouncilorStatElement.Total, total),
            (CouncilorStatElement.Base, baseVal),
            (CouncilorStatElement.FromTraits, fromTraits),
            (CouncilorStatElement.FromOrgs, fromOrgs),
            /*
             * Traits like furtive impose a cap on apparent loyalty, but crucially, not *actual* loyalty.
             * Since we don't display the actual loyalty, we return the cap on actual loyalty to avoid
             * giving the impression that the actual loyalty cannot be raised.
             */
            (CouncilorStatElement.Cap, attr == CouncilorAttribute.ApparentLoyalty ? c.GetClampedMaxStatValue(CouncilorAttribute.Loyalty) : c.GetClampedMaxStatValue(attr))
        ];
        return result.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
    }

    public static ObjectSchema<(CouncilorAttribute, Dictionary<CouncilorStatElement, float>)> CouncilorStats = new ObjectSchema<(CouncilorAttribute, Dictionary<CouncilorStatElement, float>)>()
        .AddField("Attribute", tup => TIUtilities.GetAttributeString(tup.Item1))
        .AddField("Total", tup => tup.Item2[CouncilorStatElement.Total], "N0")
        .AddField("Base", tup => tup.Item2[CouncilorStatElement.Base], "N0")
        .AddField("From Traits", tup => tup.Item2[CouncilorStatElement.FromTraits], "N0")
        .AddField("From Orgs", tup => tup.Item2[CouncilorStatElement.FromOrgs], "N0")
        .AddField("Cap", tup => tup.Item2[CouncilorStatElement.Cap], "N0")
    ;

    public static ObjectSchema<TICouncilorState> Councilor = new ObjectSchema<TICouncilorState>()
         /* fixme: This should probably be more respectful of fog-of-war constraints, along with orgs. */
        .AddField("Name", c => c.displayName)
        .AddField("Faction", c => c.faction, f => PlayerDisplayName(f))
        .AddField("Background", c => c.jobDisplayName)
        .AddField("Age", c => c.age, "N0")
        .AddField("XP", c => c.XP, "N0")
        .AddField("Location", c => PlayerDisplayName(c.location))
        .AddField("Home Region", c => PlayerDisplayName(c.homeRegion))
        .AddField("Current Mission", c => c.activeMission?.missionTemplate.displayName ?? "None")
        .AddField("Income", GetCouncilorIncomes, FormatIncomes)
    ;

    public static ObjectSchema<TITraitTemplate> Trait = new ObjectSchema<TITraitTemplate>()
        .AddField("Name", tt => tt.displayName)
    ;
};
}