using System;
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class CouncilorExtractor
    {
        private static Protos.CouncilorAttributeData ExtractAttribute(TICouncilorState c, CouncilorAttribute attr)
        {
            var baseVal = c.attributes[attr];
            var fromTraits = c.GetAttribute(attr, includeOrgs: false, includeAllUnconditionalTraits: true) - baseVal;
            var total = c.GetAttribute(attr, includeOrgs: true, includeAllUnconditionalTraits: true);
            var fromOrgs = total - fromTraits - baseVal;
            var cap = attr == CouncilorAttribute.ApparentLoyalty ? c.GetClampedMaxStatValue(CouncilorAttribute.Loyalty) : c.GetClampedMaxStatValue(attr);

            return new Protos.CouncilorAttributeData
            {
                Total = total,
                Base = baseVal,
                FromTraits = fromTraits,
                FromOrgs = fromOrgs,
                Cap = cap
            };
        }

        private static IDictionary<string, Protos.CouncilorAttributeData> ExtractAttributes(TICouncilorState c)
        {
            return GameEnums.AllCouncilorAttributes()
                .Where(a => a != CouncilorAttribute.Loyalty)
                .Select(a => (a, ExtractAttribute(c, a)))
                .ToDictionary(p => p.Item1.ToString(), p => p.Item2);
        }

        private static IDictionary<string, float> ExtractIncomes(TICouncilorState c)
        {
            return GameEnums.AllFactionResources()
                .Select(r => (r, c.GetMonthlyIncome(r)))
                .ExcludeZeroValues(p => p.Item2)
                .ToDictionary(p => p.r.ToString(), p => p.Item2);
        }

        public static Protos.CouncilorData Extract(TICouncilorState c)
        {
            var data = new Protos.CouncilorData
            {
                Name = c.displayName,
                Faction = Util.ExtractName(c.faction),
                Background = c.jobDisplayName,
                Age = c.age,
                Xp = c.XP,
                Location = Util.ExtractName(c.location),
                HomeRegion = Util.ExtractName(c.homeRegion),
                HomeNation = Util.ExtractName(c.homeNation),
                CurrentMission = c.activeMission?.missionTemplate?.displayName ?? "None"
            };

            data.Income.Add(ExtractIncomes(c));
            data.Attributes.Add(ExtractAttributes(c));
            data.Traits.AddRange(c.traits.Select(t => t.displayName));

            return data;
        }
    }
}
