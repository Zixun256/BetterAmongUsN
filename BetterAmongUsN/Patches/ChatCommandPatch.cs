using BetterAmongUsN.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        return true;
    }
}
