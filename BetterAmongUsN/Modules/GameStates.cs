using AmongUs.GameOptions;
using System;
using System.Linq;

namespace BetterAmongUsN.Modules;
public class PlayerVersion(Version ver, string tag_str, string forkId)
{
    public readonly Version version = ver;
    public readonly string tag = tag_str;
    public readonly string forkId = forkId;
#pragma warning disable CA1041 // Provide ObsoleteAttribute message
    [Obsolete] public PlayerVersion(string ver, string tag_str) : this(Version.Parse(ver), tag_str, string.Empty) { }
    [Obsolete] public PlayerVersion(Version ver, string tag_str) : this(ver, tag_str, string.Empty) { }
#pragma warning restore CA1041
    public PlayerVersion(string ver, string tag_str, string forkId) : this(Version.Parse(ver), tag_str, forkId) { }

    public bool IsEqual(PlayerVersion pv)
    {
        return pv.version == version && pv.tag == tag;
    }
}
public static class GameStates
{
    public static bool InGame = false;
    public static bool AlreadyDied = false;
    /**********Check Game Status***********/
    public static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.Normal or GameModes.NormalFools;
    public static bool IsHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.HideNSeek or GameModes.SeekFools;
    public static bool SkeldIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Skeld;
    public static bool MiraHQIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.MiraHQ;
    public static bool PolusIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Polus;
    public static bool DleksIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Dleks;
    public static bool AirshipIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Airship;
    public static bool FungleIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Fungle;
    public static bool IsLobby => AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined;
    public static bool IsCoStartGame => !InGame && !DestroyableSingleton<GameStartManager>.InstanceExists;
    public static bool IsInGame => InGame;
    public static bool IsNotJoined => AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.NotJoined;
    public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool IsExilling => ExileController.Instance != null && !(AirshipIsActive && Minigame.Instance != null && Minigame.Instance.isActiveAndEnabled);
    public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    /**********TOP ZOOM.cs***********/
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
