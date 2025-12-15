using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class TechExtractor
    {
        private static bool InProgress(TIGenericTechTemplate gt) => gt switch
        {
            TIProjectTemplate p => GameControl.control.activePlayer.CurrentlyActiveProjects().Contains(p),
            TITechTemplate t => TIGlobalResearchState.CurrentResearchingTechs.Contains(t),
            _ => throw new ArgumentException("Expected argument with project or tech type.")
        };

        public static Protos.TechStatus ExtractTechStatus(TITechTemplate t)
        {
            if (TIGlobalResearchState.TechFinished(t)) return Protos.TechStatus.Completed;
            if (InProgress(t)) return Protos.TechStatus.Active;
            if (TIGlobalResearchState.AvailableTechs().Contains(t)) return Protos.TechStatus.Available;
            return Protos.TechStatus.Blocked;
        }

        public static Protos.TechStatus ExtractProjectStatus(TIProjectTemplate p)
        {
            var player = GameControl.control.activePlayer;
            if (player.completedProjects.Contains(p)) return Protos.TechStatus.Completed;
            if (InProgress(p)) return Protos.TechStatus.Active;
            if (player.availableProjects.Contains(p)) return Protos.TechStatus.Available;
            if (p.PrereqsSatisfied(
                TIGlobalResearchState.FinishedTechs(),
                player.completedProjects,
                player))
            {
                return Protos.TechStatus.Locked;
            }
            return Protos.TechStatus.Blocked;
        }

        public static Protos.TechStatus ExtractStatus(TIGenericTechTemplate gt) => gt switch
        {
            TITechTemplate t => ExtractTechStatus(t),
            TIProjectTemplate p => ExtractProjectStatus(p),
            _ => throw new ArgumentException($"Unexpected type for {nameof(ExtractStatus)}")
        };

        private static float ExtractInitialCost(TIGenericTechTemplate t)
        {
            return t.GetResearchCost(GameControl.control.activePlayer);
        }

        private static float ExtractRemainingCost(TITechTemplate t)
        {
            if (TIGlobalResearchState.TechFinished(t)) return 0.0f;
            var cost = t.GetResearchCost(GameControl.control.activePlayer);

            var idx = TIGlobalResearchState.CurrentResearchingTechs.IndexOf(t);
            if (idx == -1) return cost;

            var state = GameStateManager.GlobalResearch();

            return cost - state.GetTechProgress(idx).accumulatedResearch;
        }

        private static float ExtractRemainingCost(TIProjectTemplate t)
        {
            var player = GameControl.control.activePlayer;
            if (player.completedProjects.Contains(t)) return 0.0f;
            var cost = t.GetResearchCost(player);
            return cost - player.GetProjectProgressValueByTemplate(t);
        }

        private static float ExtractRemainingCost(TIGenericTechTemplate gt) => gt switch
        {
            TITechTemplate t => ExtractRemainingCost(t),
            TIProjectTemplate p => ExtractRemainingCost(p),
            _ => throw new ArgumentException("Unexpected type for ExtractRemainingCost")
        };

        private static float ExtractRemainingTreeCost(TIGenericTechTemplate t)
        {
            HashSet<TIGenericTechTemplate> incompleteAncestors = [];
            Stack<TIGenericTechTemplate> stack = new();
            stack.Push(t);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (incompleteAncestors.Contains(current)) continue;

                var selfCost = ExtractRemainingCost(current);
                if (selfCost == 0.0f) continue;

                incompleteAncestors.Add(current);
                foreach (var prereq in current.TechPrereqs)
                {
                    stack.Push(prereq);
                }
            }

            return incompleteAncestors.Sum(ExtractRemainingCost);
        }

        private static Protos.ResearchProgress ExtractResearchProgress(TIGenericTechTemplate t)
        {
            var initial = ExtractInitialCost(t);
            var status = ExtractStatus(t);
            var progress = status == Protos.TechStatus.Completed ? initial :
                           status <= Protos.TechStatus.Available ? initial - ExtractRemainingCost(t) :
                                                                   0.0f;
            return new Protos.ResearchProgress
            {
                Progress = progress,
                Cost = initial
            };
        }

        private static string GetLargestContributionFromList(IEnumerable<(float, string)> contributions)
        {
            if (!contributions.Any()) return "none";
            var best = contributions.Max();
            return best.Item1 > 0.0f ? best.Item2 : "none";
        }

        private static string GetLargestContributionForActiveTech(TITechTemplate t)
        {
            var globalResearch = GameStateManager.GlobalResearch();
            for (int i = 0; i < 3; ++i)
            {
                var progress = globalResearch.GetTechProgress(i);
                if (progress.techTemplate != t) continue;

                return GetLargestContributionFromList(progress.factionContributions.Select(kvp => (kvp.Value, Util.ExtractName(kvp.Key))));
            }
            return "none";
        }

        private static string ExtractLargestContribution(TITechTemplate t)
        {
            if (InProgress(t)) return GetLargestContributionForActiveTech(t);
            var contributions = GameStateManager.AllFactions().Select(f => (f.techContributionHistory.TryGetValue(t, out var c) ? c : 0.0f, Util.ExtractName(f)));
            return GetLargestContributionFromList(contributions);
        }

        private static IEnumerable<string> ExtractAllowedFactions(TIProjectTemplate p)
        {
            var result = p.factionPrereq
                    .Select(fn => GameStateManager.FindByTemplate<TIFactionState>(fn));

            return (result.Any() ? result : GameStateManager.AllHumanFactions())
                .Select(f => Util.ExtractName(f));
        }

        private static IEnumerable<string> ExtractCompletedFactions(TIProjectTemplate p)
        {
            return GameStateManager.AllFactions().Where(f => f.completedProjects.Contains(p))
                                                 .Select(f => Util.ExtractName(f));
        }

        public static Protos.TechData ExtractTech(TITechTemplate t)
        {
            return new Protos.TechData
            {
                Name = t.displayName,
                Status = ExtractTechStatus(t),
                Progress = ExtractResearchProgress(t),
                RemainingTreeCost = ExtractRemainingTreeCost(t),
                LargestContribution = ExtractLargestContribution(t)
            };
        }

        public static Protos.ProjectData ExtractProject(TIProjectTemplate p)
        {
            var data = new Protos.ProjectData
            {
                Name = Util.ExtractName(p),
                Status = ExtractProjectStatus(p),
                Progress = ExtractResearchProgress(p),
                RemainingTreeCost = ExtractRemainingTreeCost(p)
            };

            data.CompletedByFactions.Add(ExtractCompletedFactions(p));
            data.AllowedForFactions.Add(ExtractAllowedFactions(p));

            return data;
        }

        public static Protos.AllTechsData ExtractAllTechs()
        {
            var data = new Protos.AllTechsData {};

            data.GlobalTechs.Add(
                TIGlobalResearchState.GetAllTechs()
                                     .Select(ExtractTech)
                                     .OrderBy(t => t.Status)
            );
            data.FactionProjects.Add(
                TIGlobalResearchState.GetAllProjects()
                                     .Select(ExtractProject)
                                     .Where(p => p.AllowedForFactions.Count != 1 || p.AllowedForFactions[0] != "the Aliens")
                                     .OrderBy(t => (t.Status, t.RemainingTreeCost, t.Name))
            );

            return data;
        }
    }
}
