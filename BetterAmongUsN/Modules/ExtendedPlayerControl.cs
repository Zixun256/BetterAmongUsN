using AmongUs.GameOptions;
using BetterAmongUsN.Modules;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterAmongUsN;
static class ExtendedPlayerControl
{
    public static void SetRole(this PlayerControl player, RoleTypes role, bool canOverride)
    {
        player.StartCoroutine(player.CoSetRole(role, canOverride));
    }
    public static void RpcEnterVentDesync(this PlayerPhysics physics, int ventId, PlayerControl seer)
    {
        if (physics == null) return;

        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            physics.StopAllCoroutines();
            physics.StartCoroutine(physics.CoEnterVent(ventId));
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, seer.GetClientId());
        writer.WritePacked(ventId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcExitVentDesync(this PlayerPhysics physics, int ventId, PlayerControl seer)
    {
        if (physics == null) return;

        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            physics.StopAllCoroutines();
            physics.StartCoroutine(physics.CoExitVent(ventId));
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(physics.NetId, (byte)RpcCalls.ExitVent, SendOption.Reliable, seer.GetClientId());
        writer.WritePacked(ventId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    /// <summary>
    /// ONLY to be used when killer surely may kill the target, please check with killer.RpcCheckAndMurder(target, check: true) for indirect kill.
    /// </summary>
    public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target, true);
    }
    public static void RpcSpecificShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.IsHost())
        {
            player.Shapeshift(target, shouldAnimate);
            return;
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Shapeshift, SendOption.Reliable, player.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static Vent GetClosestVent(this PlayerControl player)
    {
        var pos = player.GetCustomPosition();
        return ShipStatus.Instance.AllVents.Where(x => x != null).MinBy(x => Vector2.Distance(pos, x.transform.position));
    }

    public static List<Vent> GetVentsFromClosest(this PlayerControl player)
    {
        Vector2 playerpos = player.transform.position;
        List<Vent> vents = new(ShipStatus.Instance.AllVents);
        vents.Sort((v1, v2) => Vector2.Distance(playerpos, v1.transform.position).CompareTo(Vector2.Distance(playerpos, v2.transform.position)));

        // If player is inside a vent, we get the nearby vents that the player can snapto and insert them to the top of the list
        // Idk how to directly get the vent a player is in, so just assume the closet vent from the player is the vent that player is in
        // Not sure about whether inVent flags works 100% correct here. Maybe player is being kicked from a vent and inVent flags can return true there
        if ((player.MyPhysics.Animations.IsPlayingEnterVentAnimation() || player.walkingToVent || player.inVent) && vents[0] != null)
        {
            var nextvents = vents[0].NearbyVents.ToList();
            nextvents.RemoveAll(v => v == null);

            foreach (var vent in nextvents)
            {
                vents.Remove(vent);
            }

            vents.InsertRange(0, nextvents.FindAll(v => v != null));
        }

        return vents;
    }
    public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        var messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(PlayerControl.LocalPlayer);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcDesyncTeleport(this PlayerControl player, Vector2 position, PlayerControl seer)
    {
        if (player == null) return;
        var netTransform = player.NetTransform;
        var clientId = seer.GetClientId();
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            netTransform.SnapTo(position, (ushort)(6 + netTransform.lastSequenceId));
            return;
        }
        netTransform.lastSequenceId += 326;
        netTransform.SetDirtyBit(uint.MaxValue);

        ushort newSid = (ushort)(8 + netTransform.lastSequenceId);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable, clientId);
        NetHelpers.WriteVector2(position, writer);
        writer.Write(newSid);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var data = player.Data;
        return data == null ? -1 : data.ClientId;
    }
    public static int GetClientId(this NetworkedPlayerInfo playerData) => playerData == null ? -1 : playerData.ClientId;

    public static DeadBody GetDeadBody(this NetworkedPlayerInfo playerData)
    {
        return UnityEngine.Object.FindObjectsOfType<DeadBody>().FirstOrDefault(bead => bead.ParentId == playerData.PlayerId);
    }
    public static bool IsHost(this InnerNetObject innerObject) => innerObject.OwnerId == AmongUsClient.Instance.HostId;
    private readonly static LogHandler logger = Logger.Handler("KnowRoleTarget");

    public static bool CanBeTeleported(this PlayerControl player)
    {
        if (player.Data == null // Check if PlayerData is not null
            || MeetingHud.Instance
            // Check target status
            || !player.Data.IsDead
            || player.inVent
            || player.walkingToVent
            || player.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || player.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            return false;
        }
        return true;
    }

    public static Vector2 GetCustomPosition(this PlayerControl player) => new(player.transform.position.x, player.transform.position.y);

    public static Vector2 GetBlackRoomPosition()
    {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId switch
        {
            0 => new Vector2(-27f, 3.3f), // The Skeld
            1 => new Vector2(-11.4f, 8.2f), // MIRA HQ
            2 => new Vector2(42.6f, -19.9f), // Polus
            3 => new Vector2(27f, 3.3f), // dlekS ehT
            4 => new Vector2(-16.8f, -6.2f), // Airship
            5 => new Vector2(10.2f, 18.1f), // The Fungle
            _ => throw new NotImplementedException(),
        };
    }
    ///<summary>Is the player currently protected</summary>
    public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;
    public const MurderResultFlags ResultFlags = MurderResultFlags.Succeeded; //No need for DecisonByHost
}
