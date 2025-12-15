using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class NationExtractor
    {
        private static Protos.ControlPointData ExtractControlPoint(TIControlPoint cp)
        {
            return new Protos.ControlPointData
            {
                Owner = cp.faction != null ? Util.ExtractName(cp.faction) : "<unclaimed>",
                Crackdown = cp.benefitsDisabled,
                Executive = cp.executive,
                Defended = cp.defended,
                CpCost = cp.CurrentMaintenanceCost
            };
        }

        private static string ExtractIdeology(FactionIdeology fi)
        {
            var faction = TIFactionIdeologyTemplate.GetFactionByIdeology(fi);
            return faction != null ? Util.ExtractName(faction) : "Undecided";
        }

        private static IDictionary<string, Protos.Percentage> ExtractPublicOpinion(TINationState nation)
        {
            return nation.publicOpinion.ToDictionary(
                kvp => ExtractIdeology(kvp.Key),
                kvp => new Protos.Percentage { Value = kvp.Value }
            );
        }

        private static IDictionary<string, Protos.NationalRelationType> ExtractRelations(TINationState nation)
        {
            return nation.allies.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.Ally))
                      .Concat(nation.rivals.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.Rival)))
                      .Concat(nation.wars.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.War)))
                      .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
        }

        public static Protos.NationData Extract(TINationState nation)
        {
            var data = new Protos.NationData
            {
                Name = Util.ExtractName(nation),
                InvestmentPoints = nation.BaseInvestmentPoints_month(),
                GdpPerCapita = nation.perCapitaGDP,
                Funding = nation.spaceFundingIncome_month,
                Boost = nation.boostIncome_month_dekatons,
                MissionControl = nation.currentMissionControl,
                Research = nation.research_month,
                Population = nation.population,
                Government = nation.democracy,
                Education = nation.education,
                Inequality = nation.inequality,
                Cohesion = nation.cohesion,
                Unrest = nation.unrest,
                Sustainability = TINationState.SustainabilityValueForDisplay(nation.sustainability),
                Armies = nation.armies.Count,
                Navies = nation.numNavies,
                Miltech = nation.militaryTechLevel,
                Exofighters = nation.numSTOFighters,
                Nukes = nation.numNuclearWeapons,
                AtWar = nation.atWar
            };

            data.ControlPoints.AddRange(nation.controlPoints.Select(ExtractControlPoint));
            data.PublicOpinion.Add(ExtractPublicOpinion(nation));
            data.Relations.Add(ExtractRelations(nation));

            return data;
        }
    }
}
