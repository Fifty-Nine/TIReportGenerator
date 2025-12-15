using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class ShipTemplateExtractor
    {
        public static Protos.ShipTemplateData Extract(TISpaceShipTemplate t)
        {
            var data = new Protos.ShipTemplateData
            {
                Name = Util.ExtractName(t),
                FactionName = Util.ExtractName(t.designingFaction),
                ShipClass = t.fullClassName,
                Role = t.roleStr,
                HullName = t.hullTemplate.displayName,
                Crew = t.crewBillets,
                CruiseDeltaV = Quantity.Get(t.baseCruiseDeltaV_kps(forceUpdate: false) * 1000.0f, Protos.Unit.MetersPerSecond),
                TurnRate = Quantity.Get(t.baseAngularAcceleration_degs2, Protos.Unit.DegPerSecondSquared),
                DriveName = t.driveTemplate?.displayName ?? "none",
                PowerPlantName = t.powerPlantTemplate?.displayName ?? "none",
                PowerUsage = Quantity.Get(t.shipPowerProductionRequirement_GW * 1.0e9f, Protos.Unit.Watt),
                RadiatorName = t.radiatorTemplate?.displayName ?? "none",
                BatteryCapacity = Quantity.Get(t.BatteryCapacity_GJ() * 1.0e9f, Protos.Unit.Joule),
                McUsage = t.hullTemplate.missionControl,
                MoneyUpkeep = t.GetMonthlyExpenses(FactionResource.Money),
                BuildTimeDays = Quantity.Get(t.hullTemplate.noShipyardConstructionTime_Days(t.designingFaction), Protos.Unit.Day),

                Mass = new Protos.MassStats
                {
                    Wet = Quantity.Get(t.wetMass_tons, Protos.Unit.Ton),
                    Dry = Quantity.Get(t.dryMass_tons(), Protos.Unit.Ton)
                },

                Acceleration = new Protos.AccelerationStats
                {
                    Combat = Quantity.Get(t.baseCombatAcceleration_gs, Protos.Unit.Gee),
                    Cruise = Quantity.Get(t.baseCruiseAcceleration_gs(forceUpdate: false), Protos.Unit.Gee)
                },

                NoseArmor = new Protos.ArmorStats
                {
                    Type = t.noseArmorTemplate.displayName,
                    Value = t.noseArmorValue,
                    Thickness = Quantity.Get(t.noseArmorThickness, Protos.Unit.Meter)
                },

                LateralArmor = new Protos.ArmorStats
                {
                    Type = t.lateralArmorTemplate.displayName,
                    Value = t.lateralArmorValue,
                    Thickness = Quantity.Get(t.lateralArmorThickness_m, Protos.Unit.Meter)
                },

                TailArmor = new Protos.ArmorStats
                {
                    Type = t.tailArmorTemplate.displayName,
                    Value = t.tailArmorValue,
                    Thickness = Quantity.Get(t.tailArmorThickness, Protos.Unit.Meter)
                },

                NoseWeapons = ModuleListExtractor.Extract(t.noseWeapons, t => t.moduleTemplate.displayName),
                HullWeapons = ModuleListExtractor.Extract(t.hullWeapons, t => t.moduleTemplate.displayName),
                UtilityModules = ModuleListExtractor.Extract(t.utilityModules, t => t.moduleTemplate.displayName)
            };
            
            data.RefuelCost.Add(ResourceCostExtractor.Extract(t.propellantTanksBuildCost(faction: null)));
            data.BuildCost.Add(ResourceCostExtractor.Extract(t.spaceResourceConstructionCost(forceUpdateToCache: false, shipyard: null)));

            return data;
        }
    }
}
