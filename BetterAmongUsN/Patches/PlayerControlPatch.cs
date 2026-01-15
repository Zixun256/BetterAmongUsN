using BetterAmongUsN.Modules;
using HarmonyLib;

namespace BetterAmongUsN.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        if (MeetingHud.Instance || GameStates.IsHideNSeek) return false;
        if (EAC.RpcReportDeadBodyCheck(__instance, target))
        {
            Logger.Fatal("EAC patched the report body rpc", "ReportDeadBodyPatch");
            return false;
        }
        return true;
    }
}
