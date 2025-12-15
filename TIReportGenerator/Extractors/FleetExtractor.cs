using System.Linq;
using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    public static class FleetExtractor
    {
        private static string ExtractFleetStatus(TISpaceFleetState fleet)
        {
            if (fleet.unavailableForOperations)
            {
                return $"Unavailable for operations until {fleet.returnToOperationsTime.ToCustomTimeDateString()}";
            }

            if (fleet.inTransfer)
            {
                return $"Transferring to {Util.ExtractName(fleet.trajectory.destination)}, arrival at {fleet.trajectory.arrivalTime.ToCustomTimeDateString()}";
            }

            if (fleet.currentOperations.Count == 0) {
                return fleet.dockedOrLanded ? "Docked" : "Idle";
            }

            var operation = fleet.currentOperations.First();
            return operation.operation.GetDescription();
        }

        private static string ExtractShipStatus(TISpaceShipState ship)
        {
            if (!ship.damaged) return "OK";

            var ops = ship.fleet.currentOperations;
            if (ops.Count == 0 || ops.First().operation is not RepairFleetOperation) return "Damaged";

            return $"Repairing ({ops.First().completionDate.ToCustomTimeDateString()})";
        }

        private static Protos.DeltaVData ExtractDeltaV(TISpaceShipState ship)
        {
            return new Protos.DeltaVData
            {
                Current = Quantity.Get(ship.currentDeltaV_kps * 1000.0f, Protos.Unit.MetersPerSecond),
                Max = Quantity.Get(ship.currentMaxDeltaV_kps * 1000.0f, Protos.Unit.MetersPerSecond)
            };
        }

        private static Protos.AccelerationData ExtractShipAcceleration(TISpaceShipState ship)
        {
            return new Protos.AccelerationData
            {
                Combat = Quantity.Get(ship.combatAcceleration_gs, Protos.Unit.Gee),
                Cruise = Quantity.Get(ship.cruiseAcceleration_gs, Protos.Unit.Gee)
            };
        }

        private static Protos.DeltaVData ExtractDeltaV(TISpaceFleetState fleet)
        {
            return new Protos.DeltaVData
            {
                Current = Quantity.Get(fleet.currentDeltaV_mps, Protos.Unit.MetersPerSecond),
                Max = Quantity.Get(fleet.maxDeltaV_kps * 1000.0f, Protos.Unit.MetersPerSecond)
            };
        }

        public static Protos.ShipData Extract(TISpaceShipState ship)
        {
            return new Protos.ShipData
            {
                Name = Util.ExtractName(ship),
                ClassName = $"{ship.template.className} {ship.template.hullTemplate.displayName}",
                Role = ship.template.roleStr,
                CombatPower = ship.SpaceCombatValue(),
                AssaultPower = ship.AssaultCombatValue(defense: false),
                DeltaV = ExtractDeltaV(ship),
                Acceleration = ExtractShipAcceleration(ship),
                Status = ExtractShipStatus(ship)
            };
        }

        public static Protos.FleetData Extract(TISpaceFleetState fleet)
        {
            var data = new Protos.FleetData
            {
                Name = Util.ExtractName(fleet),
                Faction = Util.ExtractName(fleet.faction),
                Location = fleet.GetLocationDescription(GameControl.control.activePlayer, capitalize: true, expand: false),
                Status = ExtractFleetStatus(fleet),
                CombatPower = fleet.SpaceCombatValue(),
                ShipCount = fleet.ships.Count,
                DeltaV = ExtractDeltaV(fleet),
                CruiseAcceleration = Quantity.Get(fleet.cruiseAcceleration_gs, Protos.Unit.Gee),
                Homeport = fleet.homeport?.displayName ?? "None"
            };

            data.Ships.AddRange(fleet.ships.Select(Extract));
            return data;
        }
    }
}
