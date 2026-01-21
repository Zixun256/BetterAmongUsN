using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

class ReleaseContent
{
#nullable enable
    public string? name { get; set; } = null!;
#nullable disable
}

namespace BetterAmongUsNLoader
{
    [BepInPlugin("BetterAmongUsNLoader", "BetterAmongUsNLoader", "1.0.0")]
    [BepInProcess("Among Us.exe")]
    public class BetterAmongUsNLoader : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static BetterAmongUsNLoader Instance { get; private set; }
        public static ConfigEntry<bool> AutoUpdate { get; private set; }
        public static ConfigEntry<bool> UsePreview { get; private set; }
        public static ConfigEntry<string> UrlConverter { get; private set; }
        public override void Load()
        {
            Instance = this;
            AutoUpdate = Config.Bind("BAU-NaHCO3 Loader", "AutoUpdate", false, "Automatically update BAU-NaHCO3 when a new version is available.");
            UsePreview = Config.Bind("BAU-NaHCO3 Loader", "UsePreview", false, "Use preview versions of BAU-NaHCO3 when updating.");
            UrlConverter = Config.Bind("BAU-NaHCO3 Loader", "UrlConverter", "<url>", "A URL converter to use for downloading updates.");

            Log = base.Log;
            bool autoUpdate = AutoUpdate.Value;
            string dllDirectoryPath = BepInEx.Paths.BepInExRootPath + Path.DirectorySeparatorChar + "nahco3";
            string dllFilePath = dllDirectoryPath + Path.DirectorySeparatorChar + "BetterAmongUsN.dll";
            if (!File.Exists(dllFilePath))  autoUpdate = true; // force update if the file doesn't exist
            if (autoUpdate)
            {
                long size = GetVanillaSize();
                if (size == -1)
                {
                    Log.LogWarning("Assembly Is Not Found.\nAttempts to load an existing BAU-NaHCO3.");
                    TryLoad(dllFilePath);
                    return;
                }
                Log.LogInfo("Assembly Size: " + size.ToString());
                HttpClient http = new();
                http.DefaultRequestHeaders.Add("User-Agent", "BAU-NaHCO3 Updater");

                var assemblyInfoTask = GetAssemblyInfoAsync(http);
                Log.LogInfo("Start getting information about the Among us assembly...");
                assemblyInfoTask.Wait();
                var assemblyCandidates = assemblyInfoTask.Result.Where(tuple => tuple.Size == size).Select(tuple => tuple.Epoch).Distinct().ToArray();

                if (assemblyCandidates.Length == 0)
                {
                    Log.LogWarning("Unknown assembly detected.\nAttempts to load an existing BAU-NaHCO3.");
                    TryLoad(dllFilePath);
                    return;
                }

                if (assemblyCandidates.Length == 1) Log.LogInfo("Detected Epoch: " + assemblyCandidates[0]);

                var allVersions = FetchAsync(http);
                allVersions.Wait();
                Log.LogInfo("Releases Count: " + allVersions.Result.Count);
                Log.LogInfo("Version Matched Releases Count: " + allVersions.Result.Count(v => assemblyCandidates.Contains(v.Epoch)));
                var candidates = allVersions.Result.Where(v => assemblyCandidates.Contains(v.Epoch) && (v.Category == "v" || (UsePreview.Value && v.Category == "s"))).ToArray();
                if (candidates.Length == 0)
                {
                    Log.LogWarning("There is no BAU-NaHCO3 that can be implemented in the current environment.\nAttempts to load an existing BAU-NaHCO3.");
                    TryLoad(dllFilePath);
                    return;
                }
                Directory.CreateDirectory(dllDirectoryPath);
                bool shouldDownload = true;

                if (System.IO.File.Exists(dllFilePath))
                {
                    FileVersionInfo file = FileVersionInfo.GetVersionInfo(dllFilePath);
                    int currentEpoch = file.FileMajorPart;
                    int currentBuild = file.FileMinorPart;
                    Log.LogInfo($"latest version: {file.FileVersion}");

                    if (candidates[0].Epoch == currentEpoch && candidates[0].Build == currentBuild)
                    {
                        Log.LogInfo("The latest BAU-NaHCO3 is already in place.");
                        shouldDownload = false;
                    }

                    //バージョン不一致時のみ更新する場合、バージョン候補内のエポックと現在のエポックが一致していれば何もしない。
                    if (!autoUpdate && candidates.Any(c => c.Epoch == currentEpoch))
                    {
                        shouldDownload = false;
                    }
                }
                if (shouldDownload)
                {
                    Log.LogInfo("Installing " + candidates[0].VisualName.Replace('_', ' ') + "...");
                    UpdateAsync(http, candidates[0].Tag, dllFilePath).Wait();
                }
            }
            TryLoad(dllFilePath);
        }
        public static void TryLoad(string dllFilePath)
        {
            string dllFullFilePath = System.IO.Path.GetFullPath(dllFilePath);
            if (System.IO.File.Exists(dllFullFilePath))
            {
                Assembly NaHCO3Assembly = Assembly.LoadFile(dllFullFilePath);

                var nahco3PluginType = NaHCO3Assembly.GetType("BetterAmongUsN.Main");
                nahco3PluginType?.GetField("LoaderPlugin", BindingFlags.Static | BindingFlags.Public)?.SetValue(null, Instance);
                nahco3PluginType?.GetMethod("Load", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
            }
        }
        private long GetVanillaSize()
        {
            var path = BepInEx.Paths.GameRootPath + Path.DirectorySeparatorChar + "GameAssembly.dll";
            Log.LogInfo("GameAssembly Path: " + path);

            if (!System.IO.File.Exists(path)) return -1;

            FileInfo file = new FileInfo(path);
            return file.Length;
        }
        private async Task<List<(int Epoch, long Size)>> GetAssemblyInfoAsync(HttpClient http)
        {
            string url = ConvertUrl("https://raw.githubusercontent.com/Zixun256/BetterAmongUsN/master/epoch.dat");
            var response = await http.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) return [];
            string result = await response.Content.ReadAsStringAsync();
            var strings = result.Replace("\r\n", "\n").Split('\n');

            List<(int Epoch, long Size)> list = new();
            foreach (var s in strings)
            {
                var splited = s.Split(',');
                if (splited.Length != 2) continue;
                if (int.TryParse(splited[0], out var epoch) && long.TryParse(splited[1], out var size)) list.Add((epoch, size));
            }
            return list;
        }
        static public string ConvertUrl(string url)
        {
            var converter = UrlConverter.Value;
            if (converter.Length <= 1) return url;
            else return UrlConverter.Value.Replace("<url>", url);
        }
        static string GetTagsUrl(int page) => ConvertUrl("https://api.github.com/repos/Zixun256/BetterAmongUsN/tags?per_page=100&page=" + (page));
        private async Task<List<(string Tag, string Category, int Epoch, int Build, string VisualName)>> FetchAsync(HttpClient http)
        {
            List<(string Tag, string Category, int Epoch, int Build, string VisualName)> releases = new();

            int page = 1;
            while (true)
            {
                var response = await http.GetAsync(GetTagsUrl(page));

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.LogError("Bad Response: " + response.StatusCode.ToString());
                    break;
                }

                string json = await response.Content.ReadAsStringAsync();

                var tags = JsonSerializer.Deserialize<ReleaseContent[]>(json);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (tag.name != null)
                        {

                            string[] strings = tag.name.Split(",");
                            if (strings.Length != 4) continue;

                            if (!int.TryParse(strings[2], out var epoch)) continue;
                            if (!int.TryParse(strings[3], out var build)) continue;
                            releases.Add(new(tag.name, strings[0], epoch, build, strings[1]));
                        }
                    }
                }
                if (tags == null || tags.Length == 0) break;
                page++;
            }

            releases.Sort((v1, v2) => v1.Epoch != v2.Epoch ? v2.Epoch - v1.Epoch : v2.Build - v1.Build);
            return releases;
        }
        private async Task UpdateAsync(HttpClient http, string tag, string dllFilePath)
        {
            string url = ConvertUrl($"https://github.com/Zixun256/BetterAmongUsN/releases/download/{tag}/BetterAmongUsN.dll");
            var response = await http.GetAsync(url);
            if (response.StatusCode != HttpStatusCode.OK) return;
            var dllStream = await response.Content.ReadAsStreamAsync();

            try
            {
                if (System.IO.File.Exists(dllFilePath)) System.IO.File.Move(dllFilePath, dllFilePath + ".old", true);
                using var fileStream = System.IO.File.Create(dllFilePath);
                dllStream.CopyTo(fileStream);
                fileStream.Flush();
            }
            catch (Exception ex)
            {
                Log.LogError(ex);
            }
        }
    }
}
