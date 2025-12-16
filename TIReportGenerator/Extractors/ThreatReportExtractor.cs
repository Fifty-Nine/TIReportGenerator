
using System.Collections.Generic;
using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class ThreatReportExtractor
    {
        private static Protos.XenoformingSite ExtractXenoformingSite(TIRegionXenoformingState xf)
        {
            return new Protos.XenoformingSite
            {
                Nation = Util.ExtractName(xf.region.nation),
                Region = Util.ExtractName(xf.region),
                Severity = xf.severityDescription
            };
        }

        private static IEnumerable<Protos.XenoformingSite> ExtractXenoformingSites(TIFactionState player)
        {
            return player.KnownXenoforming.Select(ExtractXenoformingSite);
        }

        private static Protos.XenoCouncilor ExtractCouncilor(TICouncilorState xeno, TIFactionState player)
        {
            return new Protos.XenoCouncilor
            {
                Name = Util.ExtractName(xeno),
                Location = player.HasIntelOnCouncilorLocation(xeno) ? Util.ExtractName(xeno.location) : "Unknown",
                Mission = player.HasIntelOnCouncilorMission(xeno) ? xeno.activeMission?.missionTemplate?.displayName ?? "None" : "Unknown"
            };
        }

        private static IEnumerable<Protos.XenoCouncilor> ExtractXenoCouncilors(TIFactionState player)
        {
            return player.knownSpies
                .Where(spy => spy.faction == GameStateManager.AlienFaction())
                .Select(spy => ExtractCouncilor(spy, player));
        }

        private static string ExtractPresumedIntent(TISpaceFleetState fleet)
        {
            return fleet.AssignedGoal()?.GetGoalType() switch
            {
                GoalType.AttackWithFleet => "hostile",
                GoalType.InvadeEarth => "hostile",
                GoalType.CaptureHab => "hostile",
                GoalType.SecureEarthSpace => "hostile",
                GoalType.SurveilEarth => "surveillance",
                GoalType.FoundSurveillanceStation => "surveillance",
                GoalType.DefendWithFleet => "defense",
                GoalType.TransportCouncilorsViaFleet => "transport",
                GoalType.JoinFleet => "rendezvous",
                GoalType.AssembleFleet => "rendezvous",
                GoalType.ResupplyFleet => "logistics",
                GoalType.RepairFleet => "logistics",
                GoalType.RefitFleet => "logistics",
                _ => "unknown"
            };
        }

        private static Protos.FleetMovement ExtractFleetMovement(TISpaceFleetState fleet)
        {
            return new Protos.FleetMovement
            {
                FleetName = Util.ExtractName(fleet),
                CombatPower = fleet.SpaceCombatValue(),
                DepartingFrom = Util.ExtractName(fleet.trajectory.originOrbit),
                Destination = Util.ExtractName(fleet.trajectory.destination),
                Arrival = fleet.trajectory.arrivalTime.ToCustomTimeDateString(),
                PresumedIntent = ExtractPresumedIntent(fleet)
            };
        }

        private static IEnumerable<Protos.FleetMovement> ExtractFleetMovements(TIFactionState faction)
        {
            return faction.fleets
                .Where(f => f.inTransfer)
                .Select(ExtractFleetMovement);
        }

        public static Protos.ThreatReport Extract()
        {
            var player = GameControl.control.activePlayer;
            var data = new Protos.ThreatReport
            {
                Player = Util.ExtractName(player),
                EstimatedHate = player.GetEstimatedAlienHate(),
                MinimumHateFromMissionControl = GameStateManager.AlienFaction().MCBasedAlienHate(player),
                WarHateThreshold = TIGlobalConfig.globalConfig.factionHateWarThreshold,
                TotalWarHateThreshold = TIGlobalConfig.globalConfig.factionHateWarThreshold * 4
            };


            data.KnownXenos.Add(ExtractXenoCouncilors(player));
            data.KnownXenoforming.Add(ExtractXenoformingSites(player));
            data.FleetMovements.Add(ExtractFleetMovements(GameStateManager.AlienFaction()));
            return data;
        }
    };
}