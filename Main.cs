using Kitchen;
using KitchenMods;
using PreferenceSystem;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenMoveLayoutFeatures
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = $"IcedMilo.PlateUp.{MOD_NAME}";
        public const string MOD_NAME = "Move Layout Features";
        public const string MOD_VERSION = "0.1.0";

        internal static PreferenceSystemManager PrefManager;

        public void PostActivate(KitchenMods.Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");

            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);
            PrefManager
                .AddLabel("Move Front Door")
                .AddConditionalBlocker(() => Session.CurrentGameNetworkMode == GameNetworkMode.Client)
                    .AddButton("Left", delegate (int _)
                    {
                        MoveFrontDoor.MoveLeft();
                    })
                    .AddButton("Right", delegate (int _)
                    {
                        MoveFrontDoor.MoveRight();
                    })
                .ConditionalBlockerDone()
                .AddConditionalBlocker(() => Session.CurrentGameNetworkMode == GameNetworkMode.Host)
                    .AddInfo("Only accessible for host")
                .ConditionalBlockerDone()
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        public void PreInject()
        {
        }

        public void PostInject()
        {
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
