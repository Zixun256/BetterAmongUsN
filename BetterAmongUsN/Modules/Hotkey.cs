using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace BetterAmongUsN
{
    [HarmonyPatch]
    internal class Hotkey
    {
        private class HotkeyEntry
        {
            public KeyCode[] Keys { get; set; }
            public Action Action { get; set; }
            public bool WasPressedLastFrame { get; set; }
        }
        private static List<HotkeyEntry> keyValues = new();
        public static void Add(KeyCode[] keys, Action action)
        {
            keyValues.Add(new HotkeyEntry
            { 
                Keys = keys,
                Action = action,
                WasPressedLastFrame = false
            });
        }
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        public static void Update()
        {
            foreach (var entry in keyValues)
            {
                bool allPressedCurrentFrame = true;
                foreach (var key in entry.Keys)
                {
                    if (!Input.GetKey(key))
                    {
                        allPressedCurrentFrame = false;
                        break;
                    }
                }
                if (allPressedCurrentFrame && !entry.WasPressedLastFrame)
                {
                    try
                    {
                        entry.Action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"快捷键动作执行失败：{ex.Message}", "Hotkey");
                    }
                }
                entry.WasPressedLastFrame = allPressedCurrentFrame;
            }
        }
    }
}
