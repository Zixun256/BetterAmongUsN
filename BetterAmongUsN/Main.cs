using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BetterAmongUsN;
using Cpp2IL.Core.Attributes;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterAmongUsN;
public class Main
{
    public static BepInEx.Logging.ManualLogSource Logger;
    private static Harmony Harmony = new(Id);
    internal static string credentialsText = $"{Name} {VisualVersion}";
    internal static string LANGUAGE_FOLDER_NAME = "./BAU-NaHCO3/Lang";
    public const string Id = "BetterAmongUsN";
    public const string PluginVersion = "1.0.0";
    //public const string VisualVersion = "v1.0.0";
    public const string VisualVersion = "Preview 26.02.01a";
    public const string Name = "BetterAmongUsN";
    public static ConfigEntry<bool> DebuggerMode;
    public static BasePlugin LoaderPlugin = null!;
    public static PlayerControl[] AllPlayerControls
    {
        get
        {
            int count = PlayerControl.AllPlayerControls.Count;
            var result = new PlayerControl[count];
            int i = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.PlayerId == 255 || pc.notRealPlayer) continue;
                result[i++] = pc;
            }

            if (i == 0) return [];

            Array.Resize(ref result, i);
            return result;
        }
    }
    public static void Load()
    {
        DebuggerMode = LoaderPlugin.Config.Bind("BetterAmongUsN Settings", "DebuggerMode", false, "Show Debugger Message");
        Logger = BepInEx.Logging.Logger.CreateLogSource("BetterAmongUsN");
        BetterAmongUsN.Logger.Enable();
        if (!DebuggerMode.Value)
        {
            BetterAmongUsN.Logger.Disable("SendRPC");
            BetterAmongUsN.Logger.Disable("ReceiveRPC");
            BetterAmongUsN.Logger.Disable("SetName");
            BetterAmongUsN.Logger.Disable("PlayerControl.RpcSetRole");
            BetterAmongUsN.Logger.Disable("SetRole");
        }
        BetterAmongUsN.Logger.Info($"BetterAmongUsN v{PluginVersion} is loading...", "BetterAmongUsN");
        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();
        Hotkey.Add([KeyCode.LeftControl, KeyCode.F5], () =>
        {
            if (AmongUsClient.Instance.AmHost)
                GameManager.Instance.RpcEndGame(GameOverReason.HideAndSeek_CrewmatesByTimer, false);
        });
        Harmony.PatchAll();
        BetterAmongUsN.Logger.Msg("========= BetterAmongUsN loaded! =========", "Plugin Load");
    }
}
