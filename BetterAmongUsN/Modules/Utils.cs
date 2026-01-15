using BetterAmongUsN.Patches;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BetterAmongUsN;

[Obfuscation(Exclude = true, Feature = "renaming", ApplyToMembers = true)]
public static class Utils
{
    public static string ColorString(Color32 color, string str, bool withoutEnding = false)
    {
        var sb = new StringBuilder();
        sb.Append("<color=#").Append($"{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>").Append(str);
        if (!withoutEnding) sb.Append("</color>");
        return sb.ToString();
    }

    public static string GetRegionName(IRegionInfo region = null)
    {
        region ??= ServerManager.Instance.CurrentRegion;

        string name = region.Name;

        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            name = "Local Games";
            return name;
        }

        if (AmongUsClient.Instance.GameId == EnterCodeManagerPatch.tempGameId)
        {
            if (EnterCodeManagerPatch.tempRegion != null)
            {
                region = EnterCodeManagerPatch.tempRegion;
                name = EnterCodeManagerPatch.tempRegion.Name;
            }
        }

        if (region.PingServer.EndsWith("among.us", StringComparison.Ordinal))
        {
            // Official server
            if (name == "North America") name = "NA";
            else if (name == "Europe") name = "EU";
            else if (name == "Asia") name = "AS";

            return name;
        }

        var Ip = region.Servers.FirstOrDefault()?.Ip ?? string.Empty;

        if (Ip.Contains("aumods.us", StringComparison.Ordinal)
            || Ip.Contains("duikbo.at", StringComparison.Ordinal))
        {
            // Official Modded Server
            if (Ip.Contains("au-eu")) name = "MEU";
            else if (Ip.Contains("au-as")) name = "MAS";
            else if (Ip.Contains("www.")) name = "MNA";

            return name;
        }

        if (name.Contains("nikocat233", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Replace("nikocat233", "Niko233", StringComparison.OrdinalIgnoreCase);
        }

        return name;
    }
}
