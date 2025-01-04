using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SubnauticaShadows
{
    [HarmonyPatch(typeof(DevConsole))]
    internal class DevConsolePatch
    {
        [HarmonyPatch(nameof(DevConsole.OnSubmit))]
        [HarmonyPostfix]
        public static void Patch(DevConsole __instance, ref bool __result, object[] __args)
        {
            if (!__result)
            {
                NetworkStream stream = ServerComVars.client_tcp.GetStream();
                Byte[] message = Encoding.ASCII.GetBytes($"CHAT:{(string)__args[0]}");
                stream.Write(message, 0, message.Length);
                Plugin.Logger.LogInfo($"Chat sent:{(string)__args[0]}");
            }
        }
    }
}
