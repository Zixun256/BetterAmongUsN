using BetterAmongUsN.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BetterAmongUsN;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    public static List<string> ChatHistory = [];
    public static bool Prefix(ChatController __instance)
    {
        var text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Trim().Split(' ');
        var canceled = false;
        var cancelVal = "";
        Logger.Info(text, "SendChat");
        if (AmongUsClient.Instance.AmHost)
        {
            switch (args[0].ToLower())
            {
                case "/skipmeeting":
                    canceled = true;
                    MeetingHud.Instance.ForceSkipAll();
                    break;
            }
        }
        if (canceled)
        {
            Logger.Info("Command Canceled", "ChatCommand");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(cancelVal);

            __instance.quickChatMenu.Clear();
            __instance.quickChatField.Clear();
        }
        return !canceled;
    }
}
