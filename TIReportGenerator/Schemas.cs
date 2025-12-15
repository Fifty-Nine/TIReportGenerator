using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator {
public static class Schemas
{
    private static string FormatBigNumber<T>(T v)
    {
        return TIUtilities.FormatBigNumber(Convert.ToDouble(v));
    }

    private static string FormatSmallNumber<T>(T v)
    {
        return TIUtilities.FormatSmallNumber(Convert.ToDouble(v));
    }

    private static string FormatBool(bool v)
    {
        return v ? "yes" : "no";
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

    public static string FormatResourceCost(TIResourcesCost c)
    {
        return FormatList(Extractors.ResourceCostExtractor.ResourceCostToDictionary(c), kvp => $"{TIUtilities.FormatSmallNumber(kvp.Value)} {TIUtilities.GetResourceString(kvp.Key)}");
    }
    private static bool InProgress(TIGenericTechTemplate t)
    {
        var project = t as TIProjectTemplate;
        if (project != null) return GameControl.control.activePlayer.CurrentlyActiveProjects().Contains(project);
        return TIGlobalResearchState.CurrentResearchingTechs.Contains(t as TITechTemplate);
    }

    public enum TechStatus
    {
        /* Research finished. */
        Completed,
        /* Tech actively being researched. */
        Active,
        /* Unlocked and ready to research, but not yet in progress. */
        Available,
        /* Prerequisites complete, unlock not yet rolled or prohibited for faction. */
        Locked,
        /* Prerequisites not yet complete. */
        Blocked,
    };

    public static TechStatus GetTechStatus(TITechTemplate t)
    {
        if (TIGlobalResearchState.TechFinished(t)) return TechStatus.Completed;
        if (InProgress(t)) return TechStatus.Active;
        if (TIGlobalResearchState.AvailableTechs().Contains(t)) return TechStatus.Available;
        return TechStatus.Blocked;
    }

    public static TechStatus GetProjectStatus(TIProjectTemplate p)
    {
        var player = GameControl.control.activePlayer;
        if (player.completedProjects.Contains(p)) return TechStatus.Completed;
        if (InProgress(p)) return TechStatus.Active;
        if (player.availableProjects.Contains(p)) return TechStatus.Available;
        if (p.PrereqsSatisfied(
            TIGlobalResearchState.FinishedTechs(),
            player.completedProjects,
            player))
        {
            return TechStatus.Locked;
        }
        return TechStatus.Blocked;
    }

    private static float GetInitialCost(TIGenericTechTemplate t)
    {
        return t.GetResearchCost(GameControl.control.activePlayer);
    }

    private static float GetRemainingCost(TITechTemplate t)
    {
        if (TIGlobalResearchState.TechFinished(t)) return 0.0f;
        var cost = t.GetResearchCost(GameControl.control.activePlayer);

        var idx = TIGlobalResearchState.CurrentResearchingTechs.IndexOf(t);
        if (idx == -1) return cost;

        var state = GameStateManager.GlobalResearch();

        return cost - state.GetTechProgress(idx).accumulatedResearch;
    }

    private static float GetRemainingCost(TIProjectTemplate t)
    {
        var player = GameControl.control.activePlayer;
        if (player.completedProjects.Contains(t)) return 0.0f;
        var cost = t.GetResearchCost(player);
        return cost - player.GetProjectProgressValueByTemplate(t);
    }

    private static float GetRemainingCost(TIGenericTechTemplate t)
    {
        if (t is TITechTemplate) return GetRemainingCost((TITechTemplate)t);
        return GetRemainingCost((TIProjectTemplate)t);
    }

    private static float GetRemainingTreeCost(TIGenericTechTemplate t)
    {
        HashSet<TIGenericTechTemplate> incompleteAncestors = new();
        Stack<TIGenericTechTemplate> stack = new Stack<TIGenericTechTemplate>();
        stack.Push(t);

        while (stack.Count() > 0)
        {
            var current = stack.Pop();

            if (incompleteAncestors.Contains(current)) continue;

            var selfCost = GetRemainingCost(current);
            if (selfCost == 0.0f) continue;

            incompleteAncestors.Add(current);
            foreach (var prereq in current.TechPrereqs)
            {
                stack.Push(prereq);
            }
        }

        float totalCost = 0.0f;
        foreach (var tech in incompleteAncestors)
        {
            totalCost += GetRemainingCost(tech);
        }
        return totalCost;
    }

    private static (float?, float) GetResearchCosts(TIGenericTechTemplate t)
    {
        return (InProgress(t) ? GetRemainingCost(t) : null, GetInitialCost(t));
    }

    private static string FormatResearchCosts((float? progress, float cost) tech)
    {
        if (tech.progress != null)
        {
            return $"{tech.progress:F0} / {tech.cost:F0}";
        }
        return $"{tech.cost:F0}";
    }

    private static string GetLargestContributionFromList(IEnumerable<(float, string)> contributions)
    {
        var best = contributions.Max();
        return best.Item1 > 0.0f ? best.Item2 : "No one";
    }

    private static string GetLargestContributionForActiveTech(TITechTemplate t)
    {
        var globalResearch = GameStateManager.GlobalResearch();
        for (int i = 0; i < 3; ++i)
        {
            var progress = globalResearch.GetTechProgress(i);
            if (progress.techTemplate != t) continue;

            return GetLargestContributionFromList(progress.factionContributions.Select(kvp => (kvp.Value, PlayerDisplayName(kvp.Key))));
        }
        return "No one";
    }

    private static string GetLargestContribution(TITechTemplate t)
    {
        if (InProgress(t)) return GetLargestContributionForActiveTech(t);
        var contributions = GameStateManager.AllFactions().Select(f => (f.techContributionHistory.TryGetValue(t, out var c) ? c : 0.0f, PlayerDisplayName(f)));
        return GetLargestContributionFromList(contributions);
    }

    private static IEnumerable<TIFactionState> FactionsWithProjectCompleted(TIProjectTemplate p)
    {
        return GameStateManager.AllFactions().Where(f => f.completedProjects.Contains(p));
    }

    private static string FormatFactionsList(IEnumerable<TIFactionState> fs)
    {
        return GameStateManager.AllHumanFactions().All(f => fs.Contains(f))
            ? "Everyone"
            : FormatList(fs, f => PlayerDisplayName(f));
    }

    private static string GetProjectDisplayName(TIProjectTemplate p)
    {
        if (p.factionPrereq.Count() == 0) return p.displayName;

        return $"{p.displayName} (" + FormatList(p.factionPrereq) + ")";
    }

    private enum HabSiteStatus
    {
        Occupied,
        Unprospected,
        Available
    };

    private static bool IsProspectedByPlayer(TIHabSiteState site)
    {
        return GameControl.control.activePlayer.Prospected(site);
    }

    private static (HabSiteStatus, string) GetHabSiteStatus(TIHabSiteState site)
    {
        return site.hasPlannedOrOperatingBase ? (HabSiteStatus.Occupied, PlayerDisplayName(site.hab.faction)) :
                   IsProspectedByPlayer(site) ? (HabSiteStatus.Available, null) :
                                                (HabSiteStatus.Unprospected, null);
    }

    private static string FormatHabSiteStatus((HabSiteStatus status, string owner) site)
    {
        return site.status == HabSiteStatus.Occupied  ? $"Occupied ({site.owner})" :
               site.status == HabSiteStatus.Available ? "Available" :
                                                        "Not Prospected";
    }

    private static Func<TIHabSiteState, string> GetResourceGrade(FactionResource resource)
    {
        return site => IsProspectedByPlayer(site) ? $"{site.GetMonthlyProduction(resource):F1} ({site.GetActualResourceGrade(resource).ToString()})"
                                                  : site.GetExpectedResourceGrade(resource).ToString();
    }

    private static string FormatOrgOrbit(TIFactionState f)
    {
        return f != null ? PlayerDisplayName(f) : "none";
    }

    private static string FormatTraitRequirements(IEnumerable<TITraitTemplate> traits)
    {
        return FormatList(traits, trait => trait.displayName);
    }

    private static Dictionary<CouncilorAttribute, int> GetOrgStatBonuses(TIOrgState org)
    {
        var result = new Dictionary<CouncilorAttribute, int>();
        var stats = Enum.GetValues(typeof(CouncilorAttribute)).Cast<CouncilorAttribute>();
        foreach (var v in stats)
        {
            var bonus = org.GetStatBonus(v);
            if (bonus == 0) continue;
            result[v] = bonus;
        }
        return result;
    }

    private static string FormatStatBonuses(Dictionary<CouncilorAttribute, int> bonuses)
    {
        return FormatList(bonuses, kvp => $"{kvp.Value:+0;-0;} {kvp.Key.ToString()}");
    }

    private static Dictionary<FactionResource, float> GetOrgIncomes(TIOrgState org)
    {
        var result = new List<(FactionResource, float)>
        {
            (FactionResource.Money, org.incomeMoney_month),
            (FactionResource.Influence, org.incomeInfluence_month),
            (FactionResource.Operations, org.incomeOps_month),
            (FactionResource.Boost, org.incomeBoost_month),
            (FactionResource.MissionControl, org.incomeMissionControl),
            (FactionResource.Research, org.incomeResearch_month),
            (FactionResource.Projects, org.projectCapacityGranted)
        };
        return result.Where(kvp => kvp.Item2 > 0.05f || kvp.Item2 < -0.05f)
                     .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
    }
    private static Dictionary<FactionResource, float> GetCouncilorIncomes(TICouncilorState councilor)
    {
        return Util.GameEnums.AllFactionResources().Select(r => (r, councilor.GetMonthlyIncome(r)))
                                    .Where(rp => rp.Item2 > 0.05f || rp.Item2 < -0.05f)
                                    .ToDictionary(rp => rp.Item1, rp => rp.Item2);
    }

    private static string FormatMissions(IEnumerable<TIMissionTemplate> missions)
    {
        return FormatList(missions, mission => mission.displayName);
    }

    private static string FormatIncomes(Dictionary<FactionResource, float> incomes)
    {
        return FormatList(incomes, kvp => $"{(kvp.Value > 0 ? "+" : "")}{FormatSmallNumber(kvp.Value)} {TIUtilities.GetResourceString(kvp.Key)}");
    }

    private static string FormatTechBonuses(IEnumerable<TechBonus> bonuses)
    {
        return FormatList(bonuses, b => $"{b.bonus:+0.0%;-0.0%; 0.0%} {TIGenericTechTemplate.GetTechCategoryString(b.category)}");
    }

    private static string GetOtherOrgBonuses(TIOrgState org)
    {
        List<string> features = [];
        if (org.grantsMarked)
        {
            features.Add("adds \"Marked\" trait");
        }

        if (org.projectGranted != null)
        {
            features.Add($"unlocks \"{org.projectGranted.displayName}\" project");
        }

        if (org.miningBonus != 0.0f)
        {
            features.Add($"{org.miningBonus:+0.0%;-0.0%;0.0%} bonus space mining output");
        }

        if (org.XPModifier != 0.0f)
        {
            features.Add($"{org.XPModifier:+0.0%;-0.0%;0.0%} XP modifier");
        }

        if (org.takeoverDefense != 0.0f)
        {
            features.Add($"{org.takeoverDefense:N0} additional takeover defense");
        }

        Action<float, string> addPriorityBonus = (val, desc) =>
        {
            if (val < 0.001f && val > -0.001f) return;

            features.Add($"{val:+0.0%;-0.0%;0.0%} {desc} priority");
        };

        addPriorityBonus(org.economyBonus, "economy");
        addPriorityBonus(org.welfareBonus, "welfare");
        addPriorityBonus(org.environmentBonus, "environment");
        addPriorityBonus(org.knowledgeBonus, "knowledge");
        addPriorityBonus(org.governmentBonus, "government");
        addPriorityBonus(org.unityBonus, "unity");
        addPriorityBonus(org.militaryBonus, "military");
        addPriorityBonus(org.oppressionBonus, "oppression");
        addPriorityBonus(org.spoilsBonus, "spoils");
        addPriorityBonus(org.spaceDevBonus, "funding");
        addPriorityBonus(org.spaceflightBonus, "exofighters");
        addPriorityBonus(org.MCBonus, "mission control");

        return string.Join(", ", features);
    }

    private static string GetOrgStatus(TIOrgState org)
    {
        if (org.assignedCouncilor != null) {
            if (org.applyingBonuses) {
                return $"Equipped ({org.assignedCouncilor.displayName})";
            } else {
                return $"Inactive ({org.assignedCouncilor.displayName})";
            }
        }

        if (org.factionOrbit == null) {
            return "Limbo (no faction orbit)";
        }

        var faction = org.factionOrbit;
        if (faction.unassignedOrgs.Contains(org)) {
            return "Unassigned, In Pool";
        }

        if (faction.availableOrgs.Contains(org)) {
            return "Available On Market";
        }

        return $"Limbo (orbiting {PlayerDisplayName(faction)})";
    }

    public static string FormatList<T>(IEnumerable<T> list, Func<T, string> formatter = null)
    {
        formatter ??= o => o.ToString();
        return string.Join(", ", list.Select(formatter));
    }

    public static string GetCouncilorLocation(TICouncilorState councilor)
    {
        return GameControl.control.activePlayer.HasIntelOnCouncilorLocation(councilor) ?
            PlayerDisplayName(councilor.location) :
            "Unknown";
    }

    public static string GetCouncilorMission(TICouncilorState councilor)
    {
        return GameControl.control.activePlayer.HasIntelOnCouncilorMission(councilor) ?
            councilor.activeMission?.missionTemplate?.displayName ?? "None" :
            "Unknown";
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
        .AddField("Faction", f => PlayerDisplayName(f.faction))
        .AddField("Location", f => f.GetLocationDescription(GameControl.control.activePlayer, capitalize: true, expand: false))
        .AddField("Status", GetFleetStatus)
        .AddField("Combat Power", f => f.SpaceCombatValue(), "N0")
        .AddField("Ships", f => f.ships.Count)
        .AddField("Delta-V", GetFleetDeltaV, FormatDeltaV)
        .AddField("Cruise Acc.", f => f.cruiseAcceleration_gs, FormatAccelerationInGs)
        .AddField("Homeport", f => f.homeport?.displayName ?? "None")
    ;

    public static ObjectSchema<TITechTemplate> GlobalTechs = new ObjectSchema<TITechTemplate>()
        .AddField("Name", t => t.displayName)
        .AddField("Status", GetTechStatus)
        .AddField("Cost", GetResearchCosts, FormatResearchCosts)
        .AddField("Remaining Tree Cost", GetRemainingTreeCost, FormatBigNumber)
        .AddField("Largest Contribution", GetLargestContribution)
    ;
    public static ObjectSchema<TIProjectTemplate> FactionProjects = new ObjectSchema<TIProjectTemplate>()
        .AddField("Name", GetProjectDisplayName)
        .AddField("Status", GetProjectStatus)
        .AddField("Cost", GetResearchCosts, FormatResearchCosts)
        .AddField("Remaining Tree Cost", GetRemainingTreeCost, FormatBigNumber)
        .AddField("Completed By", FactionsWithProjectCompleted, FormatFactionsList)
    ;

    public static ObjectSchema<TIHabSiteState> HabSites = new ObjectSchema<TIHabSiteState>()
        .AddField("Name", site => PlayerDisplayName(site))
        .AddField("Status", GetHabSiteStatus, FormatHabSiteStatus)
        .AddField("Water", GetResourceGrade(FactionResource.Water))
        .AddField("Volatiles", GetResourceGrade(FactionResource.Volatiles))
        .AddField("Metals", GetResourceGrade(FactionResource.Metals))
        .AddField("Noble Metals", GetResourceGrade(FactionResource.NobleMetals))
        .AddField("Fissiles", GetResourceGrade(FactionResource.Fissiles))
    ;

    public static ObjectSchema<TISpaceBodyState> Bodies = new ObjectSchema<TISpaceBodyState>()
        .AddField("Name", body => $"**{PlayerDisplayName(body)}**")
    ;

    public static ObjectSchema<TISpaceGameState> HabSitesAndBodies =
        ObjectSchema<TISpaceGameState>.mergeDerivedObjects(HabSites, Bodies);


    public static ObjectSchema<TIOrgState> Orgs = new ObjectSchema<TIOrgState>()
        .AddField("Name", org => PlayerDisplayName(org))
        .AddField("Tier", org => org.tierStars)
        .AddField("Orbit", org => org.factionOrbit, FormatOrgOrbit)
        .AddField("Status", GetOrgStatus)
        .AddField("Eligible for Player Faction", org => org.IsEligibleForFaction(GameControl.control.activePlayer), FormatBool)
        .AddField("Required National Ties", org => org.requiresNationInterest ? org.requiredNationInterest : null, n => PlayerDisplayName(n, "none"))
        .AddField("Trait Requirements", org => org.requiredOwnerTraits, FormatTraitRequirements)
        .AddField("Equip/Transfer Cost", org => org.GetPurchaseOrTransferCost(GameControl.control.activePlayer), FormatResourceCost)
        .AddField("Skills", GetOrgStatBonuses, FormatStatBonuses)
        .AddField("Income", GetOrgIncomes, FormatIncomes)
        .AddField("Missions Granted", org => org.missionsGranted, FormatMissions)
        .AddField("Tech Bonus", org => org.techBonuses, FormatTechBonuses)
        .AddField("Bonuses", GetOtherOrgBonuses)
    ;

    private static string GetArmyStatus(TIArmyState army)
    {
        if (army.currentOperations == null || army.currentOperations.Count == 0)
        {
            return "Idle";
        }

        var op = army.currentOperations.First();
        var desc = op.operation.GetDisplayName();
        if (op.target != null) desc += $" -> {PlayerDisplayName(op.target)}";
        if (op.completionDate != null) desc += $" (ETA: {op.completionDate.ToCustomDateString()})";
        return desc;
    }

    public static ObjectSchema<TIArmyState> Armies = new ObjectSchema<TIArmyState>()
        .AddField("Name", a => PlayerDisplayName(a))
        .AddField("Faction", a => PlayerDisplayName(a.faction))
        .AddField("Nation", a => PlayerDisplayName(a.homeNation))
        .AddField("Location", a => PlayerDisplayName(a.currentRegion))
        .AddField("Tech", a => a.techLevel, "F2")
        .AddField("Strength", a => a.strength, "P0")
        .AddField("Navy", a => a.deploymentType == DeploymentType.Naval, FormatBool)
        .AddField("Status", GetArmyStatus)
    ;

    public static ObjectSchema<FactionRelation> FactionRelations = new ObjectSchema<FactionRelation>()
        .AddField("Faction", r => PlayerDisplayName(r.From))
        .AddField("Other Faction", r => PlayerDisplayName(r.To))
        .AddField("War", r => r.AtWar, FormatBool)
        .AddField("Opinion", r => r.Relationship)
        .AddField("Treaties", r => r.Treaties, treaties => FormatList(treaties, FactionRelation.TreatyName))
    ;

    public static ObjectSchema<TIRegionXenoformingState> XenoformingSite = new ObjectSchema<TIRegionXenoformingState>()
        .AddField("Region", x => PlayerDisplayName(x.region))
        .AddField("Nation", x => PlayerDisplayName(x.region.nation))
        .AddField("Severity", x => x.severityDescription)
    ;

    public static ObjectSchema<TICouncilorState> XenoCouncilor = new ObjectSchema<TICouncilorState>()
        .AddField("Name", x => PlayerDisplayName(x))
        .AddField("Location", GetCouncilorLocation)
        .AddField("Mission", GetCouncilorMission)
    ;

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