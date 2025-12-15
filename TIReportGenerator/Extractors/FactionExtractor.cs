using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class FactionExtractor
    {
        private static Protos.ResourceStockpile ExtractIncome(TIFactionState f, FactionResource r)
        {
            return new Protos.ResourceStockpile
            {
                Stockpile = f.GetCurrentResourceAmount(r),
                Income = f.GetMonthlyIncome(r)
            };
        }

        private static Protos.CapacityUse ExtractMC(TIFactionState f)
        {
            return new Protos.CapacityUse
            {
                Usage = (uint)f.GetMissionControlUsage(),
                Capacity = (uint)f.GetMonthlyIncome(FactionResource.MissionControl)
            };
        }

        private static Protos.CapacityUse ExtractCP(TIFactionState f)
        {
            return new Protos.CapacityUse
            {
                Usage = (uint)f.GetBaselineControlPointMaintenanceCost(),
                Capacity = (uint)f.GetControlPointMaintenanceFreebieCap()
            };
        }

        private static IEnumerable<Protos.Treaty> ExtractTreaties(TIFactionState from, TIFactionState to)
        {
            List<Protos.Treaty> result = [];
            if (from.HasTruce(to)) result.Add(Protos.Treaty.Truce);
            if (from.HasNAP(to)) result.Add(Protos.Treaty.NonAggressionPact);
            if (from.intelSharingFactions.Contains(to)) result.Add(Protos.Treaty.IntelSharing);
            return result;
        }

        internal enum InformalRelationType
        {
            Pleased = -2,
            Tolerant = -1,
            Wary = 0,
            Annoyed = 10,
            Displeased = 20,
            Aggrieved = 30,
            Angry = 40,
            Furious = 50,
            Outraged = 60,
            Hate = 70
        };

        internal static InformalRelationType CategorizeHate(float hate)
        {
            return Enum.GetValues(typeof(InformalRelationType))
                       .Cast<InformalRelationType>()
                       .OrderByDescending(v => (float)v)
                       .First(v => hate > (float)v);
        }

        internal static InformalRelationType CategorizeHate(TIFactionState from, TIFactionState to)
        {
            if (from.permanentAlly(to))
            {
                return InformalRelationType.Pleased;
            }

            return CategorizeHate(from.GetFactionHate(to));
        }

        private static Protos.FactionRelation ExtractRelation(TIFactionState from, TIFactionState to)
        {
            var data = new Protos.FactionRelation
            {
                Hatred = from.GetFactionHate(to),
                Opinion = CategorizeHate(from, to).ToString(),
                AtWar = from.AI_AtWarWithFaction(to)
            };
            data.Treaties.Add(ExtractTreaties(from, to));
            return data;
        }

        public static Protos.FactionData Extract(TIFactionState faction)
        {
            var data = new Protos.FactionData
            {
                Name = Util.ExtractName(faction),
                Money = ExtractIncome(faction, FactionResource.Money),
                Influence = ExtractIncome(faction, FactionResource.Influence),
                Operations = ExtractIncome(faction, FactionResource.Operations),
                Boost = ExtractIncome(faction, FactionResource.Boost),
                MissionControl = ExtractMC(faction),
                Research = faction.GetMonthlyIncome(FactionResource.Research),
                Projects = (int)faction.GetMonthlyIncome(FactionResource.Projects),
                ControlPoint = ExtractCP(faction),
                Water = ExtractIncome(faction, FactionResource.Water),
                Volatiles = ExtractIncome(faction, FactionResource.Volatiles),
                Metals = ExtractIncome(faction, FactionResource.Metals),
                Nobles = ExtractIncome(faction, FactionResource.NobleMetals),
                Fissiles = ExtractIncome(faction, FactionResource.Fissiles),
                Antimatter = ExtractIncome(faction, FactionResource.Antimatter),
                Exotics = ExtractIncome(faction, FactionResource.Exotics)
            };

            data.Relations.Add(
                GameStateManager.AllFactions()
                    .Where(o => o != faction)
                    .Select(o => (Util.ExtractName(o), ExtractRelation(faction, o)))
                    .ToDictionary(p => p.Item1, p => p.Item2)
            );
            return data;
        }
    };
}