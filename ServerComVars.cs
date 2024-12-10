using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace SubnauticaShadows
{
    public static class ServerComVars
    {
        public static List<Shadow> shadows = new();

        public static List<string> shadowids = new();

        public static Thread thread;

        public static bool initDone = false;

        public static UdpClient client;

        public static Vector3 LastPos = Vector3.zero;

        static List<QueuedShadow> queue = new();

        public static float posReqTimer = 0.2f;

        public static ConcurrentDictionary<string, Vector3> ShadowPositions = new ConcurrentDictionary<string, Vector3>();

        public static int id = -1;

        public static QueuedShadow PopQueue()
        {
            if (queue.Count == 0)
            {
                return null;
            }
            QueuedShadow shadow = queue[0];
            queue.RemoveAt(0);
            return shadow;
        }

        public static void ComsThread()
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    Plugin.Logger.LogInfo("e");
                    Byte[] recv = client.Receive(ref RemoteIpEndPoint);

                    String message = Encoding.ASCII.GetString(recv);
                    String[] splitmessage = message.SplitByChar(';');

                    Plugin.Logger.LogInfo(message);
                    //Plugin.Logger.LogInfo(splitmessage[0]);
                    //Plugin.Logger.LogInfo(splitmessage[0] == "POSUPDTCL");
                    //Plugin.Logger.LogInfo(splitmessage[0].Equals("POSUPDTCL"));



                    if (splitmessage[0] == "POSUPDTCL")
                    {
                        Plugin.Logger.LogInfo("Get positions");

                        int counter = 1;

                        //Plugin.Logger.LogInfo(counter);
                        //Plugin.Logger.LogInfo(splitmessage.Length);

                        while (counter < splitmessage.Length)
                        {
                            string[] clientpos = splitmessage[counter].SplitByChar(':');

                            Vector3 pos = new Vector3(float.Parse(clientpos[1]), float.Parse(clientpos[2]), float.Parse(clientpos[3]));

                            if (!shadowids.Contains(clientpos[0]))
                            {
                                if (int.Parse(clientpos[0]) != ServerComVars.id)
                                {
                                    Plugin.Logger.LogInfo("e");
                                    QueuedShadow queuedShadow = new QueuedShadow();
                                    queuedShadow.id = clientpos[0];
                                    queuedShadow.Pos = pos;
                                    queue.Add(queuedShadow);
                                    counter++;
                                    continue;
                                }
                            }

                            foreach (Shadow shadow in shadows)
                            {
                                if (shadow.id == clientpos[0])
                                {
                                    //Plugin.Logger.LogInfo(shadow.id);
                                    ShadowPositions[shadow.id] = pos;
                                }
                            }

                            counter++;
                        }
                    } else if (splitmessage[0] == "CLIENTID")
                    {
                         id = int.Parse(splitmessage[1]);
                    }
                }
            } catch (Exception e)
            {
                Plugin.Logger.LogError(e.Message);
                Plugin.Logger.LogError(e.StackTrace);
            }
        }
    }
}
