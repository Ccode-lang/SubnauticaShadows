using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.IO;


namespace SubnauticaShadows
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static ConfigEntry<string> ServerAddress;

        public static AssetBundle Assets = null;

        public static GameObject PlayerPrefab = null;

        private void Awake()
        {
            // set project-scoped logger instance
            Logger = base.Logger;

            string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assets = AssetBundle.LoadFromFile(Path.Combine(currentPath, "shadowplayer"));

            PlayerPrefab = Assets.LoadAsset<GameObject>("Player.prefab");

            Assets.Unload(false);

            ServerAddress = Config.Bind("General", "ServerAddress", "127.0.0.1", "The address of the server you want to connect to.");

            // register harmony patches, if there are any
            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}