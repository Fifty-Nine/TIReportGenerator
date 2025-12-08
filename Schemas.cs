using System;
using System.Collections.Generic;
using PavonisInteractive.TerraInvicta;

public static class Schemas
{
    private static string FormatResourceIncome((float stockpile, float income) values)
    {
        return $"{values.stockpile:F1}{values.income:+0.0;-0.0;''}";
    }

    private static Func<TIFactionState, (float, float)> GetResourceValues(FactionResource r)
    {
        return f => (f.GetCurrentResourceAmount(r), f.GetMonthlyIncome(r));
    }

    private static string FormatCapacity((float usage, float capacity) values)
    {
        return $"{values.usage:F0}/{values.capacity:F0}";
    }

    private static (float, float) GetMCValues(TIFactionState faction)
    {
        return (faction.GetMissionControlUsage(), faction.GetMonthlyIncome(FactionResource.MissionControl));
    }

    private static (float, float) GetCPValues(TIFactionState faction)
    {
        return (faction.GetBaselineControlPointMaintenanceCost(),
                faction.GetControlPointMaintenanceFreebieCap());
    }

    private static string FormatBigNumber<T>(T v)
    {
        return TIUtilities.FormatBigNumber(Convert.ToDouble(v));
    }

    private static string FormatBigOrSmallNumber<T>(T v)
    {
        return TIUtilities.FormatBigOrSmallNumber(Convert.ToDouble(v));
    }

    private static string FormatBool(bool v)
    {
        return v ? "yes" : "no";
    }

    private static string FormatIdeology(FactionIdeology fi)
    {
        if (fi == FactionIdeology.Undecided) return "Undecided";
        return TIFactionIdeologyTemplate.GetFactionByIdeology(fi).displayName;
    }

    public static ObjectSchema<TIFactionState> FactionResources = new ObjectSchema<TIFactionState>()
        .AddField("Faction", f => $@"{f.displayName}{(f == GameControl.control.activePlayer ? " (player)" : "")}")
        .AddField("Money", GetResourceValues(FactionResource.Money), FormatResourceIncome)
        .AddField("Influence", GetResourceValues(FactionResource.Influence), FormatResourceIncome)
        .AddField("Ops", GetResourceValues(FactionResource.Operations), FormatResourceIncome)
        .AddField("Boost", GetResourceValues(FactionResource.Boost), FormatResourceIncome)
        .AddField("MC", GetMCValues, FormatCapacity)
        .AddField("Research", f => f.GetMonthlyIncome(FactionResource.Research), "F0")
        .AddField("Projects", f => f.GetMonthlyIncome(FactionResource.Projects), "F0")
        .AddField("CP", GetCPValues, FormatCapacity)
        .AddField("Water", GetResourceValues(FactionResource.Water), FormatResourceIncome)
        .AddField("Volatiles", GetResourceValues(FactionResource.Volatiles), FormatResourceIncome)
        .AddField("Metals", GetResourceValues(FactionResource.Metals), FormatResourceIncome)
        .AddField("Nobles", GetResourceValues(FactionResource.NobleMetals), FormatResourceIncome)
        .AddField("Fissiles", GetResourceValues(FactionResource.Fissiles), FormatResourceIncome)
        .AddField("Antimatter", GetResourceValues(FactionResource.Antimatter), FormatResourceIncome)
        .AddField("Exotics", GetResourceValues(FactionResource.Exotics), FormatResourceIncome)
    ;

    public static ObjectSchema<TIControlPoint> ControlPoint = new ObjectSchema<TIControlPoint>()
        .AddField("Owner", cp => cp.faction, f => f?.displayName ?? "<unclaimed>")
        .AddField("Disabled", cp => cp.benefitsDisabled, FormatBool)
        .AddField("Executive", cp => cp.executive, FormatBool)
        .AddField("Defended", cp => cp.defended, FormatBool)
        .AddField("CP Cost", cp => cp.CurrentMaintenanceCost, "N1")
    ;

    public static ObjectSchema<KeyValuePair<FactionIdeology, float>> PublicOpinion =
        new ObjectSchema<KeyValuePair<FactionIdeology, float>>()
            .AddField("Faction", kvp => kvp.Key, FormatIdeology)
            .AddField("Support", kvp => kvp.Value, f => $"{f*100:F1}%")
    ;

    public static ObjectSchema<(TINationState, string)> NationalRelations =
        new ObjectSchema<(TINationState, string)>()
            .AddField("Faction", kvp => kvp.Item1.displayName)
            .AddField("Relation", kvp => kvp.Item2)
    ;


    public static ObjectSchema<TINationState> Nations = new ObjectSchema<TINationState>()
        .AddField("Name", n => n.displayName)
        .AddField("IP", n => n.BaseInvestmentPoints_month())
        .AddField("GDP/Cap", n => n.perCapitaGDP, "N0")
        .AddField("Funding", n => n.spaceFundingIncome_month, FormatBigOrSmallNumber)
        .AddField("Boost", n => n.boostIncome_month_dekatons, FormatBigOrSmallNumber)
        .AddField("MC", n => n.currentMissionControl, "N0")
        .AddField("Research", n => n.research_month, FormatBigOrSmallNumber)
        .AddField("Population", n => n.population, FormatBigNumber)
        .AddField("Government", n => n.democracy, "F1")
        .AddField("Education", n => n.education, "F1")
        .AddField("Inequality", n => n.inequality, "F1")
        .AddField("Cohesion", n => n.cohesion, "F1")
        .AddField("Unrest", n => n.unrest, "F1")
        .AddField("Sustainability", n => n.sustainability, TINationState.SustainabilityValueForDisplay)
        .AddField("Armies", n => n.armies.Count)
        .AddField("Navies", n => n.numNavies)
        .AddField("Miltech", n => n.militaryTechLevel, "F1")
        .AddField("Exofighters", n => n.numSTOFighters)
        .AddField("Nukes", n => n.numNuclearWeapons)
        .AddField("At War", n => n.atWar, FormatBool)
    ;


};
