using System;
using CitizenFX.Core;
using dynamicpd.models;

namespace dynamicpd.Helpers
{
    public static class DebugHelper
    {
        public static void Log(CalloutConfig config, string message, string level = "")
        {
            if (config == null || !config.debug) return;

            string formattedMessage = BuildLogString(config.shortName, message, level);

            Debug.WriteLine(formattedMessage);
            if (config.debugToConsole || config.printToConsole)
            {
                BaseScript.TriggerServerEvent("dynamicpd:consolePrint", formattedMessage);
            }
        }
        public static void Log(string prefix, string message, string level = "")
        {
            string formattedMessage = BuildLogString(prefix, message, level);

            Debug.WriteLine(formattedMessage);
            BaseScript.TriggerServerEvent("dynamicpd:consolePrint", formattedMessage);
        }
        private static string BuildLogString(string prefix, string message, string level)
        {
            string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
            string color;

            switch ((level ?? "").ToUpper())
            {
                case "INFO":
                    color = "^4"; // Blue
                    break;

                case "SUCCESS":
                    color = "^2"; // Green
                    break;

                case "WARN":
                    color = "^3"; // Yellow
                    break;

                case "ERROR":
                    color = "^1"; // Red
                    break;

                case "DEBUG":
                    color = "^6"; // Purple
                    break;

                default:
                    color = "^7"; // Default (white)
                    break;
            }

            var cleanLevel = string.IsNullOrWhiteSpace(level) || level == "_" ? "" : $" [{level.ToUpper()}]";

            return $"{color}[{timestamp}] [{prefix}]{cleanLevel} {message}^7";
        }
    }
}