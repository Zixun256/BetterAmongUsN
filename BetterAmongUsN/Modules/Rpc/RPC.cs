using AmongUs.GameOptions;
using BetterAmongUsN.Modules;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Linq;

namespace BetterAmongUsN;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.PlayerControlReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance.IsHost() ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                subReader.ReadUInt32();
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC Set Name For Player: " + __instance.GetClient().PlayerName + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetRoleRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                var canOverriddenRole = subReader.ReadBoolean();
                Logger.Info("RPC Set Role For Player: " + __instance.GetClient().PlayerName + " => " + role + " CanOverrideRole: " + canOverriddenRole, "SetRole");
                break;
            //case RpcCalls.SendChat: // Free chat
            //    var text = subReader.ReadString();
            //    Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
            //    ChatCommands.OnReceiveChat(__instance, text, out var canceled);
            //    if (canceled) return false;
            //    break;
            //others commands
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal class PlayerPhysicsRPCHandlerPatch
{
    public static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
    {
        //var rpcType = (RpcCalls)callId;
        //MessageReader subReader = MessageReader.Get(reader);

        if (EAC.PlayerPhysicsRpcCheck(__instance, callId, reader)) return false;

        var player = __instance.myPlayer;

        if (!player)
        {
            Logger.Warn("Received Physics RPC without a player", "PlayerPhysics_ReceiveRPC");
            return false;
        }

        Logger.Info($"{player.PlayerId}({(__instance.IsHost() ? "Host" : player.Data.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "PlayerPhysics_ReceiveRPC");
        return true;
    }
}

internal static class RPC
{
    public static void SendRpcLogger(uint targetNetId, byte callId, SendOption sendOption, int targetClientId = -1)
    {
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.GetClient().PlayerName;
        }
        catch { }

        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName}) SendOption:{sendOption}", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        //else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpcImmediately))]
public class StartRpcImmediatelyPatch
{
    public static void Prefix(InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(2)] SendOption option, [HarmonyArgument(3)] int targetClientId, ref MessageWriter __result)
    {
        RPC.SendRpcLogger(targetNetId, callId, option, targetClientId);
    }
}
