using System;
using System.Collections.Generic;
using System.Linq;
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
    private static Func<TIHabState, float> GetNetHabIncome(FactionResource r)
    {
        return hab => hab.GetNetCurrentMonthlyIncome(hab.coreFaction, r, includeInactivesIncomeAndSupport: false);
    }

    private static Func<TIHabState, float> GetHabTechBonus(TechCategory c)
    {
        return hab => hab.GetNetTechBonusByFaction(c, hab.coreFaction, includeInactives: false);
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

    private static string FormatSmallNumber<T>(T v)
    {
        return TIUtilities.FormatSmallNumber(Convert.ToDouble(v));
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
        .AddField("IP", n => n.BaseInvestmentPoints_month(), "F2")
        .AddField("GDP/Cap", n => n.perCapitaGDP, v => $"${v:F0}")
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
    public static ObjectSchema<TIHabState> HabsAndStations = new ObjectSchema<TIHabState>()
        .AddField("Name", hab => hab.displayName)
        .AddField("Type", hab => hab.habType, v => v.ToString())
        .AddField("Location", hab => hab.LocationName)
        .AddField("Tier", hab => hab.tier)
        .AddField("Defended", hab => hab.coreDefended, FormatBool)
        .AddField("Docked Fleets", hab => hab.dockedFleets.Count)
        .AddField("Module Count", hab => hab.AllModules().Count)
        .AddField("Modules Under Construction", hab => hab.AllModules().Count(state => state.underConstruction))
        .AddField("Unpowered Modules", hab => hab.AllModules().Count(state => !state.powered))
        .AddField("Free Slots", hab => hab.AvailableSlots().Count)
        .AddField("Population", hab => hab.crew)
        .AddField("Can Resupply", hab => hab.AllowsResupply(hab.coreFaction, allowHumanTheft: false), FormatBool)
        .AddField("Can Construct", hab => hab.AllowsShipConstruction(hab.coreFaction), FormatBool)
        .AddField("Construction Time", hab => hab.GetModuleConstructionTimeModifier(), "P0")
        .AddField("Money", GetNetHabIncome(FactionResource.Money), FormatSmallNumber)
        .AddField("Influence", GetNetHabIncome(FactionResource.Influence), FormatSmallNumber)
        .AddField("Ops", GetNetHabIncome(FactionResource.Operations), FormatSmallNumber)
        .AddField("Boost", GetNetHabIncome(FactionResource.Boost), FormatSmallNumber)
        .AddField("MissionControl", GetNetHabIncome(FactionResource.MissionControl), "F0")
        .AddField("Research", GetNetHabIncome(FactionResource.Research), FormatSmallNumber)
        .AddField("Projects", GetNetHabIncome(FactionResource.Projects), FormatSmallNumber)
        .AddField("Water", GetNetHabIncome(FactionResource.Water), FormatSmallNumber)
        .AddField("Volatiles", GetNetHabIncome(FactionResource.Volatiles), FormatSmallNumber)
        .AddField("Metals", GetNetHabIncome(FactionResource.Metals), FormatSmallNumber)
        .AddField("Nobles", GetNetHabIncome(FactionResource.NobleMetals), FormatSmallNumber)
        .AddField("Fissiles", GetNetHabIncome(FactionResource.Fissiles), FormatSmallNumber)
        .AddField("Antimatter", GetNetHabIncome(FactionResource.Antimatter), FormatSmallNumber)
        .AddField("Exotics", GetNetHabIncome(FactionResource.Exotics), FormatSmallNumber)
        .AddField("Materials Lab Bonus", GetHabTechBonus(TechCategory.Materials), "P1")
        .AddField("Space Science Lab Bonus", GetHabTechBonus(TechCategory.SpaceScience), "P1")
        .AddField("Energy Lab Bonus", GetHabTechBonus(TechCategory.Energy), "P1")
        .AddField("Life Science Lab Bonus", GetHabTechBonus(TechCategory.LifeScience), "P1")
        .AddField("Military Science Lab Bonus", GetHabTechBonus(TechCategory.MilitaryScience), "P1")
        .AddField("Information Science Lab Bonus", GetHabTechBonus(TechCategory.InformationScience), "P1")
        .AddField("Social Science Lab Bonus", GetHabTechBonus(TechCategory.SocialScience), "P1")
        .AddField("Xenology Lab Bonus", GetHabTechBonus(TechCategory.Xenology), "P1")
    ;
};
