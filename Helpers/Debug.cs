using System;
using CitizenFX.Core;
using dynamicpd.models;

namespace dynamicpd.Helpers
{
    public static class DebugHelper
    {
        private static bool _debugEnabled = false;
        private static string _calloutName = "Unknown";
        private static bool _printToConsole = false;

        public static void EnableDebug(bool enabled, string calloutName = "Unknown", bool print = false)
        {
            _debugEnabled = enabled;
            _calloutName = calloutName;
            _printToConsole = print;
        }

        public static void Log(string message, string level = "")
        {
            if (!_debugEnabled) return;
            {
                string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
                string color;

                switch (level.ToUpper())
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

                var cleanLevel = string.IsNullOrWhiteSpace(level) || level == "_" ? "" : $" [{level}]";
                Debug.WriteLine($"{color}[{timestamp}]{cleanLevel} {message}"); // Client console only

                if (_printToConsole)
                {
                    BaseScript.TriggerServerEvent("dynamicpd:consolePrint", $"{color}[{timestamp}]{cleanLevel} {message}"); // The updater is required by the server to be able to trigger this event
                }
            }
        }
    }
}
