using System.Linq;
using PavonisInteractive.TerraInvicta;
using YamlDotNet.Core.Tokens;

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

        private static Protos.PublicOpinion ExtractPublicOpinion(TINationState nation)
        {
            var result = new Protos.PublicOpinion {};
            result.Support.Add(
                nation.publicOpinion.ToDictionary(
                    kvp => ExtractIdeology(kvp.Key),
                    kvp => new Protos.Percentage { Value = kvp.Value }
                )
            );
            return result;
        }

        private static Protos.NationalRelations ExtractRelations(TINationState nation)
        {
             var result = new Protos.NationalRelations {};
             result.Relations.Add(
                nation.allies.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.Ally))
                      .Concat(nation.rivals.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.Rival)))
                      .Concat(nation.wars.Select(v => (Util.ExtractName(v), Protos.NationalRelationType.War)))
                      .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2)
             );
             return result;
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
                AtWar = nation.atWar,
                Relations = ExtractRelations(nation),
                PublicOpinion = ExtractPublicOpinion(nation)
            };

            data.ControlPoints.AddRange(nation.controlPoints.Select(ExtractControlPoint));

            return data;
        }
    }
}
