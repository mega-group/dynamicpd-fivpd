using System;
using CitizenFX.Core;
using dynamicpd.Loader;

namespace dynamicpd
{
    public class MainInit : BaseScript
    {
        public MainInit()
        {
            try
            {
                EventHandlers["dynamicpd:receiveCalloutFiles"] += new Action<string>(JsonConfigManager.OnCalloutFilesReceived);

                TriggerServerEvent("dynamicpd:requestCalloutFiles");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"^1[DynamicPD-Init] Fatal exception during client startup bridge: {ex.Message}^7");
            }
        }
    }
}