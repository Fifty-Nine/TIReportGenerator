using System.Linq;
using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    public static class ArmyExtractor
    {
        private static string GetArmyStatus(TIArmyState army)
        {
            if (army.currentOperations == null || army.currentOperations.Count == 0)
            {
                return "Idle";
            }

            var op = army.currentOperations.First();
            var desc = op.operation.GetDisplayName();
            if (op.target != null) desc += $" -> {Util.ExtractName(op.target)}";
            if (op.completionDate != null) desc += $" (ETA: {op.completionDate.ToCustomDateString()})";
            return desc;
        }

        public static Protos.ArmyData Extract(TIArmyState army)
        {
            return new Protos.ArmyData
            {
                Name = Util.ExtractName(army),
                Faction = Util.ExtractName(army.faction),
                Nation = Util.ExtractName(army.homeNation),
                Location = Util.ExtractName(army.currentRegion),
                Miltech = army.techLevel,
                Strength = new Protos.Percentage { Value = army.strength },
                HasNavy = army.deploymentType == DeploymentType.Naval,
                Status = GetArmyStatus(army)
            };
        }
    }
}
