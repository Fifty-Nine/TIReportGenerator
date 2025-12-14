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

        public static Protos.FactionData Extract(TIFactionState faction)
        {
            return new Protos.FactionData
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
        }
    };
}