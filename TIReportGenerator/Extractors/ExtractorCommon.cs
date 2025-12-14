using PavonisInteractive.TerraInvicta;

namespace TIReportGenerator.Extractors
{
    internal static class Util
    {
        public static string ExtractName(TIGameState obj)
        {
            return obj?.GetDisplayName(GameControl.control.activePlayer) ?? "<null>";
        }

        public static string ExtractName(TIDataTemplate t)
        {
            return t.displayName;
        }
    };
}