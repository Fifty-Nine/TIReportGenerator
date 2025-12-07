using System;
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

    public static ReportSchema<TIFactionState> FactionResources = new ReportSchema<TIFactionState>()
        .AddColumn("Name", f => f.displayName)
        .AddColumn("Money", GetResourceValues(FactionResource.Money), FormatResourceIncome)
        .AddColumn("Influence", GetResourceValues(FactionResource.Influence), FormatResourceIncome)
        .AddColumn("Ops", GetResourceValues(FactionResource.Operations), FormatResourceIncome)
        .AddColumn("Boost", GetResourceValues(FactionResource.Boost), FormatResourceIncome)
        .AddColumn("MC", GetMCValues, FormatCapacity)
        .AddColumn("Research", f => f.GetMonthlyIncome(FactionResource.Research), "F0")
        .AddColumn("Projects", f => f.GetMonthlyIncome(FactionResource.Projects), "F0")
        .AddColumn("CP", GetCPValues, FormatCapacity)
        .AddColumn("Water", GetResourceValues(FactionResource.Water), FormatResourceIncome)
        .AddColumn("Volatiles", GetResourceValues(FactionResource.Volatiles), FormatResourceIncome)
        .AddColumn("Metals", GetResourceValues(FactionResource.Metals), FormatResourceIncome)
        .AddColumn("Nobles", GetResourceValues(FactionResource.NobleMetals), FormatResourceIncome)
        .AddColumn("Fissiles", GetResourceValues(FactionResource.Fissiles), FormatResourceIncome)
        .AddColumn("Antimatter", GetResourceValues(FactionResource.Antimatter), FormatResourceIncome)
        .AddColumn("Exotics", GetResourceValues(FactionResource.Exotics), FormatResourceIncome)
    ;


};
