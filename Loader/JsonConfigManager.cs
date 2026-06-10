using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;
using dynamicpd.models;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace dynamicpd.Loader
{
    public static class JsonConfigManager
    {
        public static List<CalloutConfig> Configs { get; private set; }
        private static HashSet<string> checkedCallouts = new HashSet<string>();

        static JsonConfigManager()
        {
            Configs = new List<CalloutConfig>();
            LoadConfigs();
        }

        public static void LoadConfigs()
        {
            if (Configs.Count > 0) return;

            List<string> filesToLoad = new List<string>();

            // Attempt to load from fxmanifest.lua
            var fxmanifest = LoadResourceFile(GetCurrentResourceName(), "fxmanifest.lua");
            if (!string.IsNullOrEmpty(fxmanifest))
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    fxmanifest,
                    @"callouts\/dynamicpd_callouts\/([A-Za-z0-9_\-\.]+\.json)"
                );

                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    filesToLoad.Add(m.Groups[1].Value);
                }

                if (filesToLoad.Count > 0)
                {
                    Debug.WriteLine("[JsonConfigManager] Loaded callout list from fxmanifest.lua.");
                }
            }

            // Fallback to manifest.json if no files were found in fxmanifest
            if (filesToLoad.Count == 0)
            {
                var manifestJson = LoadResourceFile(GetCurrentResourceName(), "callouts/dynamicpd_callouts/manifest.json");
                if (!string.IsNullOrEmpty(manifestJson))
                {
                    try
                    {
                        filesToLoad = JsonConvert.DeserializeObject<List<string>>(manifestJson) ?? new List<string>();
                        if (filesToLoad.Count > 0)
                        {
                            Debug.WriteLine("[JsonConfigManager] Loaded callout list from manifest.json fallback.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[JsonConfigManager] Failed to parse manifest.json fallback: {ex.Message}");
                    }
                }
            }

            // Abort if both methods failed to find any files
            if (filesToLoad.Count == 0)
            {
                Debug.WriteLine("[JsonConfigManager] No callout configuration files found in fxmanifest.lua or manifest.json.");
                BaseScript.TriggerServerEvent("dynamicpd:consolePrint",
                    "^1[dynamicpd]^7 No callouts were loaded.\n^3[Hint]^7 Check your folder structure."
                );
                return;
            }

            // Parse the configs using our populated list
            foreach (var fileName in filesToLoad)
            {
                var json = LoadResourceFile(GetCurrentResourceName(), $"callouts/dynamicpd_callouts/{fileName}");
                if (string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine($"[JsonConfigManager] Could not load {fileName}");
                    continue;
                }

                try
                {
                    var cfg = JsonConvert.DeserializeObject<CalloutConfig>(json);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.shortName))
                    {
                        Configs.Add(cfg);
                        Debug.WriteLine($"[JsonConfigManager] Loaded config: {cfg.shortName}");
                    }
                    else
                    {
                        Debug.WriteLine($"[JsonConfigManager] Invalid config in {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JsonConfigManager] Failed to parse {fileName}: {ex.Message}");
                }
            }

            // Run the update checker
            foreach (var cfg in Configs)
            {
                if (checkedCallouts.Contains(cfg.shortName))
                    continue;

                if (string.IsNullOrEmpty(cfg.updateURL) || string.IsNullOrEmpty(cfg.version)) continue;

                checkedCallouts.Add(cfg.shortName);

                var argsArray = new object[] { cfg.shortName, cfg.version, cfg.updateURL };
                string payload = JsonConvert.SerializeObject(argsArray);
                int byteLen = Encoding.UTF8.GetBytes(payload).Length;

                BaseScript.TriggerServerEvent("dynamicpd:checkUpdate", cfg.shortName, cfg.version, cfg.updateURL);
            }
        }

        public static CalloutConfig GetRandomConfig()
        {
            if (Configs.Count == 0) return null;
            var rnd = new Random();
            return Configs[rnd.Next(Configs.Count)];
        }
        public static CalloutConfig GetConfigByShortName(string shortName)
        {
            return Configs.Find(cfg => cfg.shortName.Equals(shortName, StringComparison.OrdinalIgnoreCase));
        }
}