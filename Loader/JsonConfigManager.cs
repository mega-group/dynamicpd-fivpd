using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using dynamicpd.models;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace dynamicpd.Loader
{
    public static class JsonConfigManager
    {
        public static List<CalloutConfig> Configs { get; private set; } = new List<CalloutConfig>();
        private static HashSet<string> checkedCallouts = new HashSet<string>();
        public static bool IsLoaded { get; private set; } = false;
        private static bool receivedServerReply = false;

        public static async Task Initialize()
        {
            if (IsLoaded) return;

            try
            {
                Debug.WriteLine("[JsonConfigManager] Triggering cross-resource directory scan request to dynamicpd_updater...");
                receivedServerReply = false;

                BaseScript.TriggerServerEvent("dynamicpd:requestCalloutFiles");

                int timeoutTicks = 0;
                while (!receivedServerReply && timeoutTicks < 500)
                {
                    await BaseScript.Delay(10);
                    timeoutTicks++;
                }

                if (!IsLoaded)
                {
                    Debug.WriteLine("^3[JsonConfigManager] Updater response window timed out or failed. Reverting to local manifest.json backup loop...^7");
                    LoadFromManifestFallback();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"^1[JsonConfigManager] Critical defect flagged during startup initialization: {ex}^7");
                if (!IsLoaded) LoadFromManifestFallback();
            }
        }

        public static void OnCalloutFilesReceived(string jsonSerializedList)
        {
            if (IsLoaded) return;

            receivedServerReply = true;

            try
            {
                var discoveredFiles = JsonConvert.DeserializeObject<List<string>>(jsonSerializedList);
                if (discoveredFiles == null || discoveredFiles.Count == 0)
                {
                    Debug.WriteLine("^3[JsonConfigManager] Server returned 0 files in dynamicpd_callouts folder.^7");
                    LoadFromManifestFallback();
                    return;
                }

                Debug.WriteLine($"^2[JsonConfigManager] Cross-Resource Discovery Success! Processing {discoveredFiles.Count} entries from updater.^7");
                CompileTargetFiles(discoveredFiles);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"^1[JsonConfigManager] Error processing server asset mapping: {ex.Message}^7");
                LoadFromManifestFallback();
            }
        }

        private static void LoadFromManifestFallback()
        {
            if (IsLoaded) return;

            try
            {
                var manifestJson = LoadResourceFile("fivepd", "callouts/dynamicpd_callouts/manifest.json");
                if (string.IsNullOrEmpty(manifestJson))
                {
                    Debug.WriteLine("^1[JsonConfigManager] Critical Failure: No callout maps found via updater, and manifest.json is missing or blank.^7");
                    IsLoaded = true;
                    return;
                }

                var filesToLoad = JsonConvert.DeserializeObject<List<string>>(manifestJson) ?? new List<string>();
                Debug.WriteLine($"[JsonConfigManager] Fallback verified {filesToLoad.Count} target configs out of local manifest.json.");
                CompileTargetFiles(filesToLoad);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"^1[JsonConfigManager] Fallback manifest processing dropped unexpected exception: {ex.Message}^7");
                IsLoaded = true;
            }
        }

        private static void CompileTargetFiles(List<string> files)
        {
            string targetFolder = "callouts/dynamicpd_callouts";

            foreach (var rawName in files)
            {
                if (string.IsNullOrEmpty(rawName)) continue;

                string fileName = rawName.Replace("./", "").Replace("\\", "/").Trim();
                string fullPath = $"{targetFolder}/{fileName}";

                if (fileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"^5[JsonConfigManager] Script Target Registered (LUA Engine Context Ready): fivepd/{fullPath}^7");
                    continue;
                }

                var json = LoadResourceFile("fivepd", fullPath);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine($"^3[JsonConfigManager] Skipping file (File unreadable or unmapped by fivepd stream runtime): {fullPath}^7");
                    continue;
                }

                try
                {
                    var cfg = JsonConvert.DeserializeObject<CalloutConfig>(json);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.shortName))
                    {
                        Configs.Add(cfg);
                        Debug.WriteLine($"^2[JsonConfigManager] Successfully Loaded: {cfg.shortName} ({fileName})^7");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"^1[JsonConfigManager] Schema validation error while parsing {fileName}: {ex.Message}^7");
                }
            }

            IsLoaded = true;
            TriggerUpdateChecks();
        }

        private static void TriggerUpdateChecks()
        {
            try
            {
                foreach (var cfg in Configs)
                {
                    if (checkedCallouts.Contains(cfg.shortName)) continue;
                    if (string.IsNullOrEmpty(cfg.updateURL) || string.IsNullOrEmpty(cfg.version)) continue;

                    checkedCallouts.Add(cfg.shortName);
                    BaseScript.TriggerServerEvent("dynamicpd:checkUpdate", cfg.shortName, cfg.version, cfg.updateURL);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonConfigManager] Update checker postponed: Server event framework not ready ({ex.Message})");
            }
        }

        public static CalloutConfig GetRandomConfig()
        {
            if (Configs == null || Configs.Count == 0) return null;
            var rnd = new Random();
            return Configs[rnd.Next(Configs.Count)];
        }

        public static CalloutConfig GetConfigByShortName(string shortName)
        {
            if (Configs == null || string.IsNullOrEmpty(shortName)) return null;
            return Configs.Find(cfg => cfg.shortName.Equals(shortName, StringComparison.OrdinalIgnoreCase));
        }
    }
}