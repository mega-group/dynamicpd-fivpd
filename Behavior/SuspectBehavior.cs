using System;
using System.Dynamic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using dynamicpd.Helpers;

namespace dynamicpd.Behavior
{
    public class SuspectBehavior : BaseScript
    {
        public SuspectBehavior()
        {
            Exports.Add("HandleSuspectBehavior", new Action<int, string, int>(HandleBehaviorFromExport));
        }

        private void HandleBehaviorFromExport(int pedHandle, string behavior, int targetHandle)
        {
            try
            {
                Ped ped = Array.Find(World.GetAllPeds(), p => p.Handle == pedHandle);
                Ped target = Array.Find(World.GetAllPeds(), p => p.Handle == targetHandle);

                if (ped != null)
                {
                    HandleBehavior(ped, behavior, target);
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Log("[SuspectBehavior-Export]", $"Bridge execution failed parsing incoming Lua network pointers: {ex.Message}");
            }
        }

        public static void HandleBehavior(Ped ped, string behavior, Ped target = null)
        {
            if (ped == null || !ped.Exists()) return;
            Ped playerPed = target ?? Game.PlayerPed;
            if (playerPed == null || !playerPed.Exists()) return;

            ped.Task.ClearAll();

            string cleanBehavior = (behavior ?? "").ToLower().Trim();

            if (cleanBehavior.Contains("&") || cleanBehavior.Contains("_"))
            {
                ExecuteComplexSequence(ped, playerPed, cleanBehavior);
                return;
            }

            switch (cleanBehavior)
            {
                case "fight":
                    ped.Task.FightAgainst(playerPed);
                    break;

                case "flee":
                    if (ped.IsInVehicle())
                    {
                        API.TaskVehicleMissionPedTarget(ped.Handle, ped.CurrentVehicle.Handle, playerPed.Handle, 8, 35.0f, 6, -1f, -1f, true);
                    }
                    else
                    {
                        ped.Task.FleeFrom(playerPed);
                    }
                    break;

                case "wander":
                    ped.Task.WanderAround();
                    break;

                default:
                    ExecuteNativeDirectly(ped, playerPed, behavior);
                    break;
            }
        }

        private static void ExecuteComplexSequence(Ped ped, Ped target, string compoundBehavior)
        {
            string[] subTasks = compoundBehavior.Split(new char[] { '&', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);

            bool wantsToFlee = Array.Exists(subTasks, t => t.Equals("flee", StringComparison.OrdinalIgnoreCase));
            bool wantsToShoot = Array.Exists(subTasks, t => t.Equals("shoot", StringComparison.OrdinalIgnoreCase) || t.Equals("fight", StringComparison.OrdinalIgnoreCase));

            if (ped.IsInVehicle())
            {
                Vehicle veh = ped.CurrentVehicle;
                if (wantsToFlee)
                {
                    API.TaskVehicleMissionPedTarget(ped.Handle, veh.Handle, target.Handle, 8, 38.0f, 6, -1f, -1f, true);
                }
                if (wantsToShoot)
                {
                    API.TaskVehicleShootAtPed(ped.Handle, target.Handle, 9000f);
                }
            }
            else
            {
                if (wantsToShoot && wantsToFlee)
                {
                    API.SetPedCombatAttributes(ped.Handle, 46, true);
                    API.SetPedCombatAttributes(ped.Handle, 5, true);
                    API.TaskSmartFleePed(ped.Handle, target.Handle, 600f, -1, true, true);
                }
                else if (wantsToFlee)
                {
                    ped.Task.FleeFrom(target);
                }
            }
        }

        private static void ExecuteNativeDirectly(Ped ped, Ped target, string nativeFunctionName)
        {
            try
            {
                // Convert common spelling variations automatically to match the native lookups
                string formattingLookup = nativeFunctionName.Trim();

                // Example validation: If they wrote "TaskVehicleChase" in their JSON, run it dynamically!
                if (formattingLookup.Equals("TaskVehicleChase", StringComparison.OrdinalIgnoreCase) && ped.IsInVehicle())
                {
                    API.TaskVehicleChase(ped.Handle, target.Handle);
                    return;
                }
                if (formattingLookup.Equals("TaskCombatPed", StringComparison.OrdinalIgnoreCase))
                {
                    API.TaskCombatPed(ped.Handle, target.Handle, 0, 16);
                    return;
                }

                DebugHelper.Log("[SuspectBehavior-Dynamic]", $"Could not find a runtime native mapping configuration for token: {nativeFunctionName}");
            }
            catch (Exception ex)
            {
                DebugHelper.Log("[SuspectBehavior-Reflection]", $"Error trying to dynamically assign native parameters: {ex.Message}");
            }
        }
    }
}