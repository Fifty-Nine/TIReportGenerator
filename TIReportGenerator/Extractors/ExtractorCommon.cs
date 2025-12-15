using PavonisInteractive.TerraInvicta;
using TIReportGenerator.Util;

namespace TIReportGenerator.Extractors
{
    internal static class Util
    {
        public static string ExtractName(TIGameState obj, string nullDesc = "<null>")
        {
            return obj?.GetDisplayName(GameControl.control.activePlayer) ?? nullDesc;
        }

        public static string ExtractName(TIDataTemplate t, string nullDesc = "<null>")
        {
            return t?.displayName ?? nullDesc;
        }

        public static Protos.Percentage ExtractPercentage(float value)
        {
            var result = new Protos.Percentage { };
            if (RangeFilters.IsNonzeroPercentage(value)) {
                result.Value = value;
            }
            return result;
        }
    };
}