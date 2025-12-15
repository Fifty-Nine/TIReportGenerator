using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Mdb;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class HabExtractor
    {
        private static IDictionary<string, float> ExtractResources(TIHabState hab)
        {
            return GameEnums.AllFactionResources()
                .Select(
                    fr => (TIUtilities.GetResourceString(fr), hab.GetNetCurrentMonthlyIncome(hab.coreFaction, fr, includeInactivesIncomeAndSupport: false))
                )
                .Where(p => p.Item2 >= 0.05f || p.Item2 <= -0.05f)
                .ToDictionary(p => p.Item1, p => p.Item2);
        }

        private static IDictionary<string, Protos.Percentage> ExtractLabBonuses(TIHabState hab)
        {
            return GameEnums.AllTechCategories()
                .Select(c => (c.ToString(), hab.GetNetTechBonusByFaction(c, hab.coreFaction, includeInactives: false)))
                .Where(p => p.Item2 > 0.0001f || p.Item2 < -0.0001f)
                .ToDictionary(p => p.Item1, p => new Protos.Percentage { Value = p.Item2});
        }
        
        private static Protos.ModuleList ExtractHabModules(IEnumerable<TIHabModuleState> modules)
        {
            return ModuleListExtractor.Extract(modules, hm => hm.moduleTemplate.displayName);
        }

        public static Protos.HabData Extract(TIHabState hab)
        {
            var result = new Protos.HabData
            {
                Name = Util.ExtractName(hab),
                Type = hab.habType.ToString(),
                Location = hab.LocationName,
                Tier = hab.tier,
                DockedFleets = hab.dockedFleets.Count,
                FreeSlots = hab.AvailableSlots().Count,
                Population = hab.crew,
                CanResupply = hab.AllowsResupply(hab.coreFaction, allowHumanTheft: false),
                CanConstruct = hab.AllowsShipConstruction(hab.coreFaction),
                ConstructionTimeModifier = hab.GetModuleConstructionTimeModifier(),
                ActiveModules = ExtractHabModules(hab.AllModules().Where(m => m.active && !m.destroyed)),
                UnpoweredModules = ExtractHabModules(hab.AllModules().Where(m => !m.powered && !m.underConstruction && !m.destroyed)),
                UnderConstructionModules = ExtractHabModules(hab.AllModules().Where(m => m.underConstruction && !m.destroyed)),
                DestroyedModules = ExtractHabModules(hab.AllModules().Where(m => m.destroyed))
            };
            result.MonthlyIncome.Add(ExtractResources(hab));
            result.LabBonuses.Add(ExtractLabBonuses(hab));
            return result;
        }
    }
}
