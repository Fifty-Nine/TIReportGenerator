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

    private static string FormatSmallNumberWithPrefix<T>(T v)
    {
        return TIUtilities.FormatSmallNumber(Convert.ToDouble(v), avoidPrefix: false);
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
        return PlayerDisplayName(
            TIFactionIdeologyTemplate.GetFactionByIdeology(fi),
            "Undecided"
        );
    }


    private static string GetFleetStatus(TISpaceFleetState fleet)
    {
        if (fleet.unavailableForOperations)
        {
            return $"Unavailable for operations until {fleet.returnToOperationsTime.ToCustomTimeDateString()}";
        }

        if (fleet.inTransfer)
        {
            return $"Transferring to {PlayerDisplayName(fleet.trajectory.destination)}, arrival at {fleet.trajectory.arrivalTime.ToCustomTimeDateString()}";
        }

        if (fleet.currentOperations.Count == 0) {
            return fleet.dockedOrLanded ? "Docked" : "Idle";
        }

        var operation = fleet.currentOperations.First();
        return operation.operation.GetDescription();
    }

    private static (float, float) GetFleetDeltaV(TISpaceFleetState fleet)
    {
        return (fleet.currentDeltaV_kps, fleet.maxDeltaV_kps);
    }

    private static (float, float) GetShipDeltaV(TISpaceShipState ship)
    {
        return (ship.currentDeltaV_kps, ship.currentMaxDeltaV_kps);
    }

    private static string FormatDeltaV((float current, float max) deltaV)
    {
        return $"{deltaV.current:F1} / {deltaV.max:F1} kps";
    }

    private static string FormatAccelerationInGs(float gs)
    {
        if (gs >= 1.0) return $"{gs:N1} g";
        return $"{gs*1000:N1} mg";
    }

    private static (float, float) ShipAccel(TISpaceShipState ship)
    {
        return (ship.combatAcceleration_gs, ship.cruiseAcceleration_gs);
    }

    private static string FormatShipAccel((float combat, float cruise) accel)
    {
        return $"{FormatAccelerationInGs(accel.combat)} / {FormatAccelerationInGs(accel.cruise)}";
    }

    private static string GetShipStatus(TISpaceShipState ship)
    {
        if (!ship.damaged) return "OK";

        var ops = ship.fleet.currentOperations;
        if (ops.Count == 0 || ops.First().operation is not RepairFleetOperation) return "Damaged";

        return $"Repairing ({ops.First().completionDate.ToCustomTimeDateString()})";
    }

    private static (float, float) GetShipTemplateMasses(TISpaceShipTemplate t)
    {
        return (t.wetMass_tons, t.dryMass_tons());
    }

    private static (float, float) GetShipTemplateAccel(TISpaceShipTemplate t)
    {
        return (t.baseCombatAcceleration_gs, t.baseCruiseAcceleration_gs(forceUpdate: false));
    }

    private static string FormatShipMasses((float wet, float dry) masses)
    {
        return $"{masses.wet:N0} tons (Wet) / {masses.dry:N0} tons (Dry)";
    }

    private static string PlayerDisplayName<T>(T obj, string nullText = "<null>") where T : TIGameState
    {
        return obj?.GetDisplayName(GameControl.control.activePlayer) ?? nullText;
    }

    private static string FormatBatteryList(IEnumerable<TIBatteryTemplate> batteries)
    {
        if (batteries.Count() == 0) return "none";
        return "[" + string.Join(", ", batteries.Select(b => b.displayName)) + "]";
    }

    private static (string, int, float) GetShipTemplateNoseArmor(TISpaceShipTemplate t)
    {
        return (t.noseArmorTemplate.displayName, t.noseArmorValue, t.noseArmorThickness);
    }

    private static (string, int, float) GetShipTemplateLateralArmor(TISpaceShipTemplate t)
    {
        return (t.lateralArmorTemplate.displayName, t.lateralArmorValue, t.lateralArmorThickness_m);
    }
    private static (string, int, float) GetShipTemplateTailArmor(TISpaceShipTemplate t)
    {
        return (t.tailArmorTemplate.displayName, t.tailArmorValue, t.tailArmorThickness);
    }

    private static string FormatArmorValues((string name, int value, float thickness) armor)
    {
        return $"{armor.name} ({armor.value} pts / {FormatSmallNumberWithPrefix(armor.thickness)}m)";
    }

    private static Dictionary<FactionResource, float> ResourceCostToDictionary(TIResourcesCost c)
    {
        var values = Enum.GetValues(typeof(FactionResource))
                         .Cast<FactionResource>()
                         .Where(v => v != FactionResource.MissionControl && v != FactionResource.None)
        ;
        return values.ToDictionary(
            fr => fr,
            c.GetSingleCostValue
        );
    }

    public static Dictionary<FactionResource, float> CollectBuildCosts(TISpaceShipTemplate t)
    {
        return ResourceCostToDictionary(
            t.spaceResourceConstructionCost(forceUpdateToCache: false, shipyard: null)
        );
    }

    public static Dictionary<FactionResource, float> CollectRefuelCosts(TISpaceShipTemplate t)
    {
        return ResourceCostToDictionary(
            t.propellantTanksBuildCost(faction: null)
        );
    }

    public static string FormatResourceCost(TIResourcesCost c)
    {
        return c.GetString("Relevant", includeCostStr: true, includeCompletionTime: false, completionTimeOnly: false);
    }

    public static ObjectSchema<TIFactionState> FactionResources = new ObjectSchema<TIFactionState>()
        .AddField("Faction", f => $@"{PlayerDisplayName(f)}{(f == GameControl.control.activePlayer ? " (player)" : "")}")
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
        .AddField("Owner", cp => PlayerDisplayName(cp.faction, "<unclaimed>"))
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
            .AddField("Faction", kvp => PlayerDisplayName(kvp.Item1))
            .AddField("Relation", kvp => kvp.Item2)
    ;

    public static ObjectSchema<TINationState> Nations = new ObjectSchema<TINationState>()
        .AddField("Name", n => PlayerDisplayName(n))
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
        .AddField("Name", hab => PlayerDisplayName(hab))
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

    public static ObjectSchema<TISpaceShipState> Ships = new ObjectSchema<TISpaceShipState>()
        .AddField("Name", s => PlayerDisplayName(s))
        .AddField("Class", s => s.template.className)
        .AddField("Role", s => s.template.roleStr)
        .AddField("Template", s => s.template.displayName)
        .AddField("Space Combat Power", s => s.SpaceCombatValue(), "N0")
        .AddField("Assault Combat Power", s => s.AssaultCombatValue(defense: false))
        .AddField("Delta-V", GetShipDeltaV, FormatDeltaV)
        .AddField("Acceleration (Combat/Cruise)", ShipAccel, FormatShipAccel)
        .AddField("Status", GetShipStatus)
    ;

    public static ObjectSchema<TISpaceFleetState> Fleets = new ObjectSchema<TISpaceFleetState>()
        .AddField("Name", f => PlayerDisplayName(f))
        .AddField("Location", f => f.GetLocationDescription(GameControl.control.activePlayer, capitalize: true, expand: false))
        .AddField("Status", GetFleetStatus)
        .AddField("Combat Power", f => f.SpaceCombatValue(), "N0")
        .AddField("Ships", f => f.ships.Count)
        .AddField("Delta-V", GetFleetDeltaV, FormatDeltaV)
        .AddField("Cruise Acc.", f => f.cruiseAcceleration_gs, FormatAccelerationInGs)
        .AddField("Homeport", f => f.homeport?.displayName ?? "None")
    ;

    public static ObjectSchema<ModuleDataEntry> ShipWeapon = new ObjectSchema<ModuleDataEntry>()
        .AddField("Name", t => t.weaponTemplate.displayName)
    ;

    public static ObjectSchema<ModuleDataEntry> ShipUtilityModule = new ObjectSchema<ModuleDataEntry>()
        .AddField("Name", t => t.moduleTemplate.displayName)
    ;

    public static ObjectSchema<TISpaceShipTemplate> ShipTemplate = new ObjectSchema<TISpaceShipTemplate>()
        .AddField("Name", t => t.displayName)
        .AddField("Faction", t => t.designingFaction, f => PlayerDisplayName(f))
        .AddField("Class", t => t.fullClassName)
        .AddField("Role", t => t.roleStr)
        .AddField("Hull", t => t.hullTemplate.displayName)
        .AddField("Mass", GetShipTemplateMasses, FormatShipMasses)
        .AddField("Crew", t => t.crewBillets)
        .AddField("Acceleration (Combat/Cruise)", GetShipTemplateAccel, FormatShipAccel)
        .AddField("Delta-V", t => t.baseCruiseDeltaV_kps(forceUpdate: false), v => $"{v:F1} kps")
        .AddField("Turn Rate", t => t.baseAngularAcceleration_degs2, v => $"{v:F1} deg/s²")
        .AddField("Drive", t => t.driveTemplate?.displayName ?? "none")
        .AddField("Power Plant", t => t.powerPlantTemplate?.displayName ?? "none")
        .AddField("Power Usage", t => t.shipPowerProductionRequirement_GW, v => $"{FormatSmallNumber(v)} GW")
        .AddField("Radiator", t => t.radiatorTemplate?.displayName ?? "none")
        .AddField("Batteries", t => t.batteryTemplates, FormatBatteryList)
        .AddField("Battery Capacity", t => t.BatteryCapacity_GJ(), v => $"{FormatSmallNumber(v)} GJ")
        .AddField("Nose Armor", GetShipTemplateNoseArmor, FormatArmorValues)
        .AddField("Lateral Armor", GetShipTemplateLateralArmor, FormatArmorValues)
        .AddField("Tail Armor", GetShipTemplateTailArmor, FormatArmorValues)
        .AddField("MC Usage", t => t.hullTemplate.missionControl)
        .AddField("Money Upkeep", t => t.GetMonthlyExpenses(FactionResource.Money), FormatSmallNumber)
        .AddField("Build Time", t => t.hullTemplate.noShipyardConstructionTime_Days(t.designingFaction), v => $"{v:N0} days")
    ;
};
