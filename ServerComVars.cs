using Oculus.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubnauticaShadows
{
    public static class ServerComVars
    {
        // List of all shadows
        public static List<Shadow> shadows = new();

        // List of all shadow ids
        public static List<string> shadowids = new();

        // Udp thread
        public static Thread thread_udp;

        // Tcp Thread
        public static Thread thread_tcp;

        // Is init done
        public static bool initDone = false;

        // Udp client socket
        public static UdpClient client_udp;

        // Tcp client socket
        public static TcpClient client_tcp;

        // Current client's last position
        public static Vector3 LastPos = Vector3.zero;

        // Queue of shadows to be created in unity's thread for thread safety
        static List<QueuedShadow> queue = new();

        static List<Shadow> deleteQueue = new();

        // Timer for requesting positions
        public static float posReqTimer = 0.2f;

        // Thread safe disctionary for shadow position updates in unity
        public static ConcurrentDictionary<string, Vector3> ShadowPositions = new ConcurrentDictionary<string, Vector3>();

        public static int id = -1;

        // Chat print queue
        private static List<string> chatprintqueue = new();




        public static int SIO_UDP_CONNRESET = -1744830452;

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

        public static void PopAndDeleteShadows()
        {
            if (deleteQueue.Count == 0)
            {
                return;
            }
            Shadow shadow = deleteQueue[0];
            deleteQueue.RemoveAt(0);

            shadowids.RemoveAll((String s) =>
            {
                Plugin.Logger.LogInfo($"{int.Parse(s)}:{int.Parse(shadow.id)}");
                return int.Parse(s) == int.Parse(shadow.id);
            });

            shadows.Remove(shadow);
            Object.Destroy(shadow.gameObject);
        }

        public static void Disconnect()
        {
            Plugin.Logger.LogInfo("Disconnect");
            client_tcp.GetStream().Write(Encoding.ASCII.GetBytes("DISCONN"), 0, Encoding.ASCII.GetBytes("DISCONN").Length);
            client_tcp.Close();
            client_udp.Close();

            shadows.Clear();
            shadowids.Clear();
            thread_udp.Abort();
            thread_tcp.Abort(); // TODO: fix bad error handling

            initDone = false;

            LastPos = Vector3.zero;

            queue.Clear();

            posReqTimer = 0.2f;

            ShadowPositions.Clear();

            id = -1;
        }

        public static void ComsThread_udp()
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    //Plugin.Logger.LogInfo("e");
                    Byte[] recv = client_udp.Receive(ref RemoteIpEndPoint);

                    String message = Encoding.ASCII.GetString(recv);
                    String[] splitmessage = message.SplitByChar(';');

                    //Plugin.Logger.LogInfo(message);
                    //Plugin.Logger.LogInfo(splitmessage[0]);
                    //Plugin.Logger.LogInfo(splitmessage[0] == "POSUPDTCL");
                    //Plugin.Logger.LogInfo(splitmessage[0].Equals("POSUPDTCL"));



                    if (splitmessage[0] == "POSUPDTCL")
                    {
                        //Plugin.Logger.LogInfo("Get positions");

                        int counter = 1;

                        //Plugin.Logger.LogInfo(counter);
                        //Plugin.Logger.LogInfo(splitmessage.Length);

                        while (counter < splitmessage.Length)
                        {
                            //Plugin.Logger.LogInfo(splitmessage[counter]);
                            string[] clientpos = splitmessage[counter].SplitByChar(':');

                            Vector3 pos = new Vector3(float.Parse(clientpos[1]), float.Parse(clientpos[2]), float.Parse(clientpos[3]));

                            if (!shadowids.Contains(clientpos[0]))
                            {
                                if (int.Parse(clientpos[0]) != ServerComVars.id)
                                {
                                    //Plugin.Logger.LogInfo("e");
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
                    }
                } 
            }
            catch (ThreadAbortException e) {
                ; // Just fail gracefully
            } catch (Exception e)
            {
                Plugin.Logger.LogError(e.Message);
                Plugin.Logger.LogError(e.StackTrace);
            }
        }

        public static async void ComsThread_tcp()
        {
            NetworkStream stream = client_tcp.GetStream();
            while (true) {
                Byte[] recv = new byte[1024];
                await stream.ReadAsync(recv, 0, recv.Length);

                string message = Encoding.ASCII.GetString(recv).Replace("\0", "");
                string[] splitmessage = message.SplitByChar(';');

                Plugin.Logger.LogInfo(message);


                if (splitmessage[0] == "CLIENTID")
                {
                    id = int.Parse(splitmessage[1]);
                    initDone = true;
                } else if (splitmessage[0] == "DISCONNCL")
                {
                    foreach (Shadow shadow in shadows)
                    {
                        Plugin.Logger.LogInfo($"{int.Parse(shadow.id)}:{int.Parse(splitmessage[1])}");
                        if (int.Parse(shadow.id) == int.Parse(splitmessage[1]))
                        {
                            Plugin.Logger.LogInfo("Removed shadow object");
                            deleteQueue.Add(shadow);
                        }
                    }
                } else if (splitmessage[0] == "CHATCL")
                {
                    char[] separator = { ';' };

                    chatprintqueue.Add(message.Split(separator, 2)[1]);
                }
            }
        }

        public static string PopChat()
        {
            if (chatprintqueue.Count == 0)
            {
                return null;
            }
            string chat = chatprintqueue[0];
            chatprintqueue.RemoveAt(0);
            return chat;
        }
    }
}
