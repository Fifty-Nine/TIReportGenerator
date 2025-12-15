using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class OrgExtractor
    {
        private static Protos.OrgStatus ExtractOrgStatus(TIOrgState org)
        {
            if (org.assignedCouncilor != null) {
                if (org.applyingBonuses) {
                    return Protos.OrgStatus.Equipped;
                } else {
                    return Protos.OrgStatus.Inactive;
                }
            }

            if (org.factionOrbit == null) {
                return Protos.OrgStatus.LimboNoFactionOrbit;
            }

            var faction = org.factionOrbit;
            if (faction.unassignedOrgs.Contains(org)) {
                return Protos.OrgStatus.InFactionPool;
            }

            if (faction.availableOrgs.Contains(org)) {
                return Protos.OrgStatus.InFactionMarket;
            }

            return Protos.OrgStatus.LimboInOrbit;
        }

        private static string ExtractOtherOrgBonuses(TIOrgState org)
        {
            List<string> features = new List<string>();
            if (org.takeoverDefense != 0.0f)
            {
                features.Add($"{org.takeoverDefense:N0} additional takeover defense");
            }

            return string.Join(", ", features);
        }

        private static IEnumerable<string> ExtractTraitRequirements(TIOrgState org)
        {
            return org.requiredOwnerTraits.Select(v => Util.ExtractName(v))
                      .Concat(org.prohibitedOwnerTraits.Select(v => $"not {Util.ExtractName(v)}"));
        }

        private static IEnumerable<string> ExtractMissionsGranted(TIOrgState org)
        {
            return org.missionsGranted.Select(m => m.displayName);
        }

        private static IDictionary<string, Protos.Percentage> ExtractTechBonuses(TIOrgState org)
        {
            return GameEnums.AllTechCategories()
                .Select(c => (c, org.techBonuses.Where(b => b.category == c).Sum(b => b.bonus)))
                .ExcludeZeroPercent(p => p.Item2)
                .ToDictionary(p => p.Item1.ToString(), p => new Protos.Percentage { Value = p.Item2 });
        }

        private static IDictionary<string, float> ExtractEquipCost(TIOrgState org)
        {
            return ResourceCostExtractor.Extract(org.GetPurchaseOrTransferCost(GameControl.control.activePlayer));
        }

        private static IDictionary<string, int> ExtractStatBonuses(TIOrgState org)
        {
            return GameEnums.AllCouncilorAttributes()
                            .Select(attr => (attr, org.GetStatBonus(attr)))
                            .ExcludeZeroValues(p => p.Item2)
                            .ToDictionary(p => p.Item1.ToString(), p => p.Item2);
        }

        private static IDictionary<string, float> ExtractResourceIncomes(TIOrgState org)
        {
            return new List<(FactionResource, float)> {
                (FactionResource.Money, org.incomeMoney_month),
                (FactionResource.Influence, org.incomeInfluence_month),
                (FactionResource.Operations, org.incomeOps_month),
                (FactionResource.Boost, org.incomeBoost_month),
                (FactionResource.MissionControl, org.incomeMissionControl),
                (FactionResource.Research, org.incomeResearch_month),
                (FactionResource.Projects, org.projectCapacityGranted)
            }.ExcludeZeroValues(p => p.Item2)
             .ToDictionary(p => TIUtilities.GetResourceString(p.Item1), p => p.Item2);
        }

        private static IDictionary<string, Protos.Percentage> ExtractPriorityBonuses(TIOrgState org)
        {
            return new List<(PriorityType, float)> {
                (PriorityType.Economy, org.economyBonus),
                (PriorityType.Welfare, org.welfareBonus),
                (PriorityType.Environment, org.environmentBonus),
                (PriorityType.Knowledge, org.knowledgeBonus),
                (PriorityType.Government, org.governmentBonus),
                (PriorityType.Unity, org.unityBonus),
                (PriorityType.Military, org.militaryBonus),
                (PriorityType.Oppression, org.oppressionBonus),
                (PriorityType.Spoils, org.spoilsBonus),
                (PriorityType.Funding, org.spaceDevBonus),
                (PriorityType.LaunchFacilities, org.spaceflightBonus),
                (PriorityType.MissionControl, org.MCBonus)
            }.ExcludeZeroPercent(p => p.Item2)
             .ToDictionary(p => p.Item1.ToString(), p => new Protos.Percentage { Value = p.Item2 });
        }

        public static Protos.OrgData Extract(TIOrgState org)
        {
            var data = new Protos.OrgData
            {
                Name = Util.ExtractName(org),
                Tier = org.tier,
                OrbitFaction = Util.ExtractName(org.factionOrbit),
                Councilor = Util.ExtractName(org.assignedCouncilor, "none"),
                Status = ExtractOrgStatus(org),
                EligibleForPlayer = org.IsEligibleForFaction(GameControl.control.activePlayer),
                HomeNation = Util.ExtractName(org.homeNation),
                HomeNationConnectionRequired = org.requiresNationInterest,
                GrantsMarked = org.grantsMarked,
                XpModifier = Util.ExtractPercentage(org.XPModifier),
                MiningBonus = Util.ExtractPercentage(org.miningBonus)
            };

            data.TraitRequirements.Add(ExtractTraitRequirements(org));
            data.MissionsGranted.Add(ExtractMissionsGranted(org));
            data.TechBonuses.Add(ExtractTechBonuses(org));
            data.EquipCost.Add(ExtractEquipCost(org));
            data.SkillBonuses.Add(ExtractStatBonuses(org));
            data.Income.Add(ExtractResourceIncomes(org));
            data.PriorityBonuses.Add(ExtractPriorityBonuses(org));

            return data;
        }
    }
}
