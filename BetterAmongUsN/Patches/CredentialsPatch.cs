using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

namespace BetterAmongUsN;
[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
class PingTrackerUpdatePatch
{
    public static PingTracker Instance;
    private static int DelayUpdate = 0;
    private static readonly StringBuilder sb = new();

    private static bool Prefix(PingTracker __instance)
    {
        try
        {
            Instance ??= __instance;

            DelayUpdate--;

            if (DelayUpdate > 0 && sb.Length > 0)
            {
                ChangeText(__instance);
                __instance.aspectPosition.DistanceFromEdge = GetPingPosition();
                __instance.text.text = sb.ToString();
                return false;
            }

            DelayUpdate = 500;

            ChangeText(__instance);
            sb.Clear();

            sb.Append(Main.credentialsText);

            var ping = AmongUsClient.Instance.Ping;
            string pingcolor = "#ff4500";
            if (ping < 30) pingcolor = "#44dfcc";
            else if (ping < 100) pingcolor = "#7bc690";
            else if (ping < 200) pingcolor = "#f3920e";
            else if (ping < 400) pingcolor = "#ff146e";
            sb.Append($"\r\n<color={pingcolor}>Ping: {ping} ms</color>\r\n<color=#a54aff>Server: <color=#f34c50>{Utils.GetRegionName()}</color>");
            var FPSGame = 1.0f / Time.deltaTime;
            Color fpscolor = Color.green;

            if (FPSGame < 20f) fpscolor = Color.red;
            else if (FPSGame < 40f) fpscolor = Color.yellow;

            sb.Append($"\r\n{Utils.ColorString(fpscolor, Utils.ColorString(Color.cyan, "FPS:") + ((int)FPSGame).ToString())}");

            __instance.aspectPosition.DistanceFromEdge = GetPingPosition();
            __instance.text.text = sb.ToString();
            return false;
        }
        catch
        {
            DelayUpdate = 0;
            sb.Clear();

            return false;
        }
    }
    private static Vector3 GetPingPosition()
    {
        var settingButtonTransformPosition = DestroyableSingleton<HudManager>.Instance.SettingsButton.transform.localPosition;
        var offset_x = settingButtonTransformPosition.x - 3.2f;
        var offset_y = settingButtonTransformPosition.y + 3.2f;
        Vector3 position;
        if (AmongUsClient.Instance.IsGameStarted)
        {
            if (DestroyableSingleton<HudManager>.Instance && !HudManager.Instance.Chat.isActiveAndEnabled)
            {
                offset_x += 0.7f; // Additional offsets for chat button if present
            }
            else
            {
                offset_x += 0.1f;
            }

            position = new Vector3(offset_x, offset_y, 0f);
        }
        else
        {
            position = new Vector3(offset_x, offset_y, 0f);
        }

        return position;
    }
    private static void ChangeText(PingTracker __instance)
    {
        __instance.text.alignment = TextAlignmentOptions.Right;
        __instance.text.outlineColor = Color.black;
        var language = DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID;
        __instance.text.outlineWidth = language switch
        {
            SupportedLangs.Russian or SupportedLangs.Japanese or SupportedLangs.SChinese or SupportedLangs.TChinese => 0.25f,
            _ => 0.40f,
        };
    }
}
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

        LateTask.Update(Time.deltaTime);
    }
    public static void Postfix(ModManager __instance)
    {
        var offset_y = HudManager.InstanceExists ? 1.8f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
    }
}
