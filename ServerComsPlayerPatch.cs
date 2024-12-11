using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace SubnauticaShadows
{
    [HarmonyPatch(typeof(Player))]
    internal class ServerComsPlayerPatch
    {
        [HarmonyPatch(nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void Patch(Player __instance)
        {
            if (ServerComVars.initDone) return;
            ServerComVars.client = new UdpClient();

            // Fixes problems with non local server access
            ServerComVars.client.Client.IOControl(
                (IOControlCode)ServerComVars.SIO_UDP_CONNRESET,
                new byte[] { 0, 0, 0, 0 },
                null
            );

            ServerComVars.client.Connect(Plugin.ServerAddress.Value, 4504);
            ServerComVars.thread = new Thread(new ThreadStart(ServerComVars.ComsThread));
            ServerComVars.thread.Start();
            ServerComVars.initDone = true;

            // ServerComVars.client.Send(Encoding.ASCII.GetBytes("E"), Encoding.ASCII.GetBytes("E").Length);
        }

        [HarmonyPatch(nameof(Player.Update))]
        [HarmonyPostfix]
        public static void UpdatePatch(Player __instance)
        {
            if (Vector3.Distance(ServerComVars.LastPos, __instance.transform.position) > 0.2f) {
                ServerComVars.LastPos = __instance.transform.position;
                string ID = "";

                if (ServerComVars.id == -1)
                {
                    ID = "NA";
                } else
                {
                    ID = ServerComVars.id.ToString();
                }

                Byte[] message = Encoding.ASCII.GetBytes($"POSUPDT:{__instance.transform.position.x.ToString()}:{__instance.transform.position.y.ToString()}:{__instance.transform.position.z.ToString()}:{ID}");
                //Plugin.Logger.LogInfo(Encoding.ASCII.GetString(message));
                ServerComVars.client.Send(message, message.Length);
            }

            ServerComVars.posReqTimer -= Time.deltaTime;

            if (ServerComVars.posReqTimer <= 0)
            {
                ServerComVars.posReqTimer = 0.2f;
                Plugin.Logger.LogInfo("Requesting positions");
                ServerComVars.client.Send(Encoding.ASCII.GetBytes("POSREQ"), Encoding.ASCII.GetBytes("POSREQ").Length);
            }

            foreach (string id in ServerComVars.ShadowPositions.Keys)
            {
                foreach (Shadow shadow in ServerComVars.shadows)
                {
                    if (shadow.id == id)
                    {
                        //Plugin.Logger.LogInfo(shadow.id);
                        //Plugin.Logger.LogInfo(id);
                        shadow.transform.position = ServerComVars.ShadowPositions[id];
                    }
                }
            }

            QueuedShadow shadowq = ServerComVars.PopQueue();


            if (shadowq != null)
            {
                //Plugin.Logger.LogInfo("test");
                ServerComVars.shadowids.Add(shadowq.id);
                GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shadow.AddComponent<Shadow>();
                shadow.GetComponent<Collider>().enabled = false;
                shadow.transform.position = shadowq.Pos;
                shadow.name = $"Shadow{shadowq.id}";
                Shadow sh = shadow.GetComponent<Shadow>();
                sh.id = shadowq.id;
                ServerComVars.shadows.Add(sh);
            }
        }
    }
}
