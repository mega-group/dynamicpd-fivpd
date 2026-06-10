using System.Threading.Tasks;
using CitizenFX.Core;

namespace dynamicpd.Helpers
{
    public static class SuspectMonitor
    {
        public static async Task MonitorAsync(Ped suspect, System.Func<bool> isFinished, System.Action markFinished, System.Action endCallout)
        {
            DebugHelper.Log("[dynamicpd_callout]", "Starting suspect monitor...");

            while (!isFinished())
            {
                if (suspect == null || !suspect.Exists())
                {
                    DebugHelper.Log("[dynamicpd_callout]", "Suspect no longer exists.");
                    break;
                }

                if (suspect.IsDead || suspect.IsCuffed)
                {
                    DebugHelper.Log("[dynamicpd_callout]", "Suspect is dead or cuffed. Ending callout.");
                    markFinished?.Invoke();
                    endCallout?.Invoke();
                    break;
                }

                await BaseScript.Delay(1000); // check every second
            }

            DebugHelper.Log("[dynamicpd_callout]", "Suspect monitor ended.");
        }

    }
}