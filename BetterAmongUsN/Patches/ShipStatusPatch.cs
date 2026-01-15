using BetterAmongUsN.Modules;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterAmongUsN.Patches;
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
public static class MessageReaderUpdateSystemPatch
{
    public static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (systemType is
            SystemTypes.Ventilation
            or SystemTypes.Security
            or SystemTypes.Decontamination
            or SystemTypes.Decontamination2
            or SystemTypes.Decontamination3
            or SystemTypes.MedBay) return true;

        if (GameStates.IsHideNSeek) return true;

        var amount = MessageReader.Get(reader).ReadByte();
        if (EAC.RpcUpdateSystemCheck(player, systemType, amount))
        {
            Logger.Info("Eac patched Sabotage RPC", "MessageReaderUpdateSystemPatch");
            return false;
        }

        return UpdateSystemPatch.Prefix(__instance, systemType, player, amount);
    }
    public static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (systemType is
            SystemTypes.Ventilation
            or SystemTypes.Security
            or SystemTypes.Decontamination
            or SystemTypes.Decontamination2
            or SystemTypes.Decontamination3
            or SystemTypes.MedBay) return;

        if (GameStates.IsHideNSeek) return;

        //UpdateSystemPatch.Postfix(__instance, systemType, player, MessageReader.Get(reader).ReadByte());
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
class UpdateSystemPatch
{
    public static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        Logger.Msg($"SystemType: {systemType}, PlayerName: {player.GetClient().PlayerName}, amount: {amount}", "ShipStatus.UpdateSystem");

        if (!AmongUsClient.Instance.AmHost) return true;
        if (GameStates.IsHideNSeek) return false;
        return true;
    }
    public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
    {
        var Ids = new List<int>();
        for (var i = min; i <= max; i++)
        {
            Ids.Add(i);
        }
        CheckAndOpenDoors(__instance, amount, [.. Ids]);
    }
    private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
    {
        if (!DoorIds.Contains(amount)) return;
        foreach (var id in DoorIds)
        {
            __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
        }
    }
}
