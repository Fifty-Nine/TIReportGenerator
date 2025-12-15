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