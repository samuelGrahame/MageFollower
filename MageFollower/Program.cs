using MageFollower.Client;
using MageFollower.Utilities;
using MageFollower.World;
using MageFollower.World.Element;
using MageFollower.World.Skills;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MageFollower
{
    public class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        static void Main(string[] args)
        {            
            //if(args != null && args.Length > 0 || Debugger.IsAttached) // 
            //{
            //    if (Debugger.IsAttached || args[0] == "server") // 
            //    {
            //        AllocConsole();
            //        StartServer();
            //        return;
            //    }
            //}      

            //if(Debugger.IsAttached)
            //    AllocConsole();

            using (var game = new Game2D())
                game.Run();

            //var r = new Random();
            //var fireMelee = new Entity() { 
            //    ElementType = ElementType.Fire,
            //    Health = 100,
            //    Name = "The Fire Swordsman"
            //};
            //fireMelee.Melee.AddXp(10000.0f);            
            //fireMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //var waterMelee = new Entity()
            //{
            //    ElementType = ElementType.Water,
            //    Health = 100,
            //    Name = "The Water Swordsman"
            //};
            //waterMelee.Melee.AddXp(10000.0f);
            //waterMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //if(r.NextDouble() < 0.5f)
            //{
            //    while (waterMelee.IsAlive && fireMelee.IsAlive)
            //    {
            //        fireMelee.AttackTarget(waterMelee,
            //            r.NextDouble());
            //        Thread.Sleep(100);
            //        if (waterMelee.IsAlive)
            //        {
            //            waterMelee.AttackTarget(fireMelee,
            //                r.NextDouble());

            //            Thread.Sleep(100);
            //        }
            //    }
            //}
            //else
            //{
            //    while (waterMelee.IsAlive && fireMelee.IsAlive)
            //    {
            //        waterMelee.AttackTarget(fireMelee,
            //            r.NextDouble());
            //        Thread.Sleep(100);
            //        if (fireMelee.IsAlive)
            //        {
            //            fireMelee.AttackTarget(waterMelee,
            //                r.NextDouble());

            //            Thread.Sleep(100);
            //        }
            //    }
            //}


            //Console.WriteLine($"The winner is: {(waterMelee.Health > 0 ? waterMelee.Name : fireMelee.Name)}");
            //Console.Read();
        }

        public class DamageToTarget
        {            
            [JsonProperty("h")]
            public double HealthToSet;
            [JsonProperty("d")]
            public double DamageDone;
        }
        public class Transform
        {
            [JsonProperty("p")]    
            public Vector2 Position;
            [JsonProperty("r")]
            public float Rotation;
        }

        static string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new();
            Random rnd = new();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        //
        public static void StartServer()
        {
            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new(ipAddress, 11000);




            var listOfEntities = new List<Entity>();            
            var listOfSockets = new List<Socket>();
            var SocketToEntity = new Dictionary<Socket, Entity>();
            var PassCodeToSocket = new Dictionary<string, (Socket socket, Entity entity)>();
            var IdToSocket = new Dictionary<string, (Socket socket, Entity entity)>();
            var worldEnviorment = new Enviroment();
            var idToEntity = new Dictionary<string, Entity>();

            var listOfEnemies = new List<Entity>();

            var enemy = new Entity()
            {
                MaxHealth = 100,
                Health = 100,
                Color = Color.Red,
                Id = "-1",
                Name = "Bot Red",
                Speed = 250,
                ElementType = ElementType.Fire,
                RightHand = new World.Items.Item()
                {
                    Equipt = World.Items.EquiptType.Physical,
                    Power = 1.1f,
                    Type = World.Items.ItemType.Stick
                }
            };
            enemy.Melee.AddXp(10000.0f);
            listOfEnemies.Add(enemy);

            idToEntity.Add(enemy.Id, enemy);

            //var fireMelee = new Entity() { 
            //    ElementType = ElementType.Fire,
            //    Health = 100,
            //    Name = "The Fire Swordsman"
            //};
            //fireMelee.Melee.AddXp(10000.0f);            
            //fireMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            // load worlds;

            try
            {
                // TODO Load all chunks
                var world = JsonConvert.DeserializeObject<Enviroment>(System.IO.File.ReadAllText("World.Json"));
                if(world != null)
                {
                    worldEnviorment = world;
                }
            }
            catch (Exception)
            {

            }
            

            try
            {

                // Create a Socket that will use Tcp protocol      
                Socket listener = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method  
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.  
                // We will listen 10 requests at a time  
                listener.Listen(100000);
                var dataToSend = new ConcurrentQueue<(string data, Socket exclude)>();

                var saveGameWorld = new Thread(() =>
                {
                    while (true)
                    {
                        // Save every 30 seconds
                        Thread.Sleep(5 * 1000);

                        try
                        {
                            if(worldEnviorment.IsDirty)
                            {
                                Console.WriteLine("Saving game world");

                                System.IO.File.WriteAllText("World.Json", JsonConvert.SerializeObject(worldEnviorment));
                            }
                            else
                            {
                                Console.WriteLine("Was going to save but world has not changed.");
                            }
                            
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Failed to save game world");
                        }
                        
                    }
                });
                saveGameWorld.Start();

                var playersDispatchThread = new Thread(() =>
                {
                    var r = new Random();
                    var st = Stopwatch.StartNew();
                    while (true)
                    {
                        Thread.Sleep(1000 / 30);
                        foreach (var item in listOfEntities)
                        {
                            if (item.AttackSleep > 0)
                            {
                                item.AttackSleep -= st.ElapsedMilliseconds;
                                if (item.AttackSleep < 0)
                                    item.AttackSleep = 0;
                            }
                            if (item.TargetEntity == null)
                                continue;
                            
                            if (!item.TargetEntity.IsAlive || !item.IsAlive)
                            {
                                item.TargetEntity = null;                                
                            }
                            else
                            {
                                if (VectorHelper.AreInRange(75.0f, item.Position, item.TargetEntity.Position))
                                {
                                    //targetPos = null;
                                    if (item.AttackSleep == 0)
                                    {
                                        var startingHealth = item.TargetEntity.Health;
                                        item.AttackTarget(item.TargetEntity, r.NextDouble());
                                        var newHealth = item.TargetEntity.Health;
                                        item.AttackSleep = 1000; // 3 second cool down for aa
                                        dataToSend.Enqueue(($"DMG:{item.TargetEntity.Id}:{JsonConvert.SerializeObject(new DamageToTarget() { DamageDone = startingHealth - newHealth, HealthToSet = newHealth })}<EOF>", null));
                                    }
                                }
                                else
                                {
                                    // PROCESSED ON CLIENT SO FAR - need to workout how to create smooth lerp. / PROB if we used a fixed game loop.
                                    //Vector2 dir = item.TargetEntity.Position - item.Position;

                                    //Vector2 dPos = item.Position - item.TargetEntity.Position;

                                    //dir.Normalize();
                                    //item.Position += dir * item.Speed * (float)st.ElapsedMilliseconds / 1000.0f;
                                    //item.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);

                                    //dataToSend.Enqueue(($"POS:{item.Id}:{JsonConvert.SerializeObject(new Transform() { Rotation = item.Rotation, Position = item.Position })}<EOF>", null));
                                }
                            }
                        }

                        st.Restart();
                    }
                });
                playersDispatchThread.Start();

                var processAIThread = new Thread(() =>
                {
                    var r = new Random();

                    var st = Stopwatch.StartNew();
                    while(true)
                    {
                        Thread.Sleep(1000 / 30);
                        foreach (var item in listOfEnemies)
                        {
                            if(item.AttackSleep > 0)
                            {
                                item.AttackSleep -= st.ElapsedMilliseconds;
                                if (item.AttackSleep < 0)
                                    item.AttackSleep = 0;
                            }

                            if (item.TargetEntity == null || !item.TargetEntity.IsAlive)
                            {
                                item.TargetEntity = null;
                                float LowersDistance = int.MaxValue;
                                Entity entity = null;

                                foreach (var player in listOfEntities)
                                {
                                    if(player.IsAlive)
                                    {
                                        var distance = Vector2.Distance(player.Position, item.Position);
                                        if (distance < LowersDistance)
                                        {
                                            entity = player;
                                            break;
                                        }
                                    }                                    
                                }

                                if(entity == null)
                                {
                                    // idle walk maybe
                                }
                                else
                                {
                                    item.TargetEntity = entity;
                                }
                            }
                            else
                            {
                                if (VectorHelper.AreInRange(75.0f, item.Position, item.TargetEntity.Position))
                                {
                                    //targetPos = null;
                                    if(item.AttackSleep == 0)
                                    {
                                        var startingHealth = item.TargetEntity.Health;
                                        item.AttackTarget(item.TargetEntity, r.NextDouble());
                                        var newHealth = item.TargetEntity.Health;
                                        item.AttackSleep = 1000; // 3 second cool down for aa
                                        dataToSend.Enqueue(($"DMG:{item.TargetEntity.Id}:{JsonConvert.SerializeObject(new DamageToTarget() { DamageDone = startingHealth - newHealth, HealthToSet = newHealth })}<EOF>", null));
                                    }
                                }
                                else
                                {
                                    Vector2 dir = item.TargetEntity.Position - item.Position;

                                    Vector2 dPos = item.Position - item.TargetEntity.Position;

                                    dir.Normalize();
                                    item.Position += dir * item.Speed * (float)st.ElapsedMilliseconds / 1000.0f;
                                    item.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                                    
                                    dataToSend.Enqueue(($"POS:{item.Id}:{JsonConvert.SerializeObject(new Transform() { Rotation = item.Rotation, Position = item.Position } )}<EOF>", null));
                                }
                            }
                        }

                        st.Restart();
                    }                    
                });
                processAIThread.Start();

                var sendUpdatesToClients = new Thread(() =>
                {
                    while(true)
                    {
                        if(dataToSend.Count > 0)
                        {                            
                            if(dataToSend.TryDequeue(out (string data, Socket exclude) dataPack) && dataPack.data != null)
                            {
                                var keys = SocketToEntity.Keys;
                                byte[] msg = Encoding.ASCII.GetBytes(dataPack.data);

                                if(dataPack.exclude == null)
                                {
                                    foreach (var item in keys)
                                    {
                                        item.Send(msg);
                                    }
                                }
                                else
                                {
                                    foreach (var item in keys)
                                    {
                                        if(item != dataPack.exclude)
                                            item.Send(msg);

                                    }                                    
                                }
                                
                            }

                        }
                        if(dataToSend.Count == 0)
                            Thread.Sleep(1);
                    }
                });
                sendUpdatesToClients.Start();
                int i = 0;
                while(true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    Socket handler = listener.Accept();

                    listOfSockets.Add(handler);
                    i++;

                    var thrd = new Thread((socketPass) =>
                    {
                        var socketToUse = (Socket)socketPass;

                        try
                        {
                            string leftOver = "";
                            while (true)
                            {
                                
                                // Incoming data from the client.    
                                string data = leftOver;
                                byte[] bytes = null;

                                while (true)
                                {
                                    bytes = new byte[1028];
                                    int bytesRec = socketToUse.Receive(bytes);
                                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                    if (data.IndexOf("<EOF>") > -1)
                                    {
                                        break;
                                    }
                                }

                                var packets = data.Split("<EOF>");
                                int length = packets.Length;

                                if (!data.EndsWith("<EOF>"))
                                {
                                    leftOver = packets[length - 1];
                                    length--;
                                }
                                else
                                {
                                    leftOver = "";
                                }

                                for (int index = 0; index < length; index++)
                                {
                                    var item = packets[index];

                                    if (item.StartsWith("NEW:"))
                                    {
                                        var newEntity = new Entity()
                                        {
                                            Health = 100,
                                            MaxHealth = 100,
                                            Name = "Human " + i.ToString(),
                                            Speed = 400
                                        };

                                        newEntity.Id = i.ToString();

                                        var newPass = CreatePassword(10);

                                        listOfEntities.Add(newEntity);

                                        byte[] msg = Encoding.ASCII.GetBytes($"PASS:{newPass}:{newEntity.Id}<EOF>");

                                        SocketToEntity.Add(socketToUse, newEntity);
                                        PassCodeToSocket.Add(newPass, (socketToUse, newEntity));
                                        IdToSocket.Add(newEntity.Id, (socketToUse, newEntity));
                                        idToEntity.Add(newEntity.Id, newEntity);

                                        socketToUse.Send(msg);
                                        dataToSend.Enqueue(("NEW:" + JsonConvert.SerializeObject(newEntity) + "<EOF>", null));

                                        // Send All Connected
                                        foreach (var entity in listOfEntities)
                                        {
                                            if (entity != newEntity)
                                            {
                                                msg = Encoding.ASCII.GetBytes("NEW:" + JsonConvert.SerializeObject(entity) + "<EOF>");
                                                socketToUse.Send(msg);
                                            }
                                        }

                                        foreach (var enemies in listOfEnemies)
                                        {
                                            msg = Encoding.ASCII.GetBytes("NEW:" + JsonConvert.SerializeObject(enemies) + "<EOF>");
                                            socketToUse.Send(msg);
                                        }

                                        if(worldEnviorment.EnviromentItems != null)
                                        {
                                            foreach (var itemToSpawn in worldEnviorment.EnviromentItems)
                                            {
                                                msg = Encoding.ASCII.GetBytes($"SPAWN:{JsonConvert.SerializeObject(itemToSpawn)}<EOF>");
                                                socketToUse.Send(msg);
                                            }
                                        }                                        
                                    }
                                    else if (item.StartsWith("POS:"))
                                    {
                                        var newPos = item.Substring("POS:".Length, item.Length - "POS:".Length);
                                        var transform = JsonConvert.DeserializeObject<Transform>(newPos);
                                        var entitiy = SocketToEntity[socketToUse];
                                        // TODO/Lasttime moved + workout distance . to see if teleport to ban...
                                        entitiy.Position = transform.Position;
                                        entitiy.Rotation = transform.Rotation;

                                        dataToSend.Enqueue(($"POS:{entitiy.Id}:{JsonConvert.SerializeObject(transform)}<EOF>", socketToUse));
                                    }else if(item.StartsWith("TARGET:"))
                                    {
                                        var newTarget = item.Substring("TARGET:".Length, item.Length - "TARGET:".Length);
                                        var entitiy = SocketToEntity[socketToUse];

                                        if (string.CompareOrdinal(newTarget, "NULL") != 0 && idToEntity.ContainsKey(newTarget))
                                        {
                                            var targetEntity = idToEntity[newTarget];
                                            
                                            if (targetEntity != null && targetEntity != entitiy && targetEntity.IsAlive && entitiy.IsAlive)
                                            {
                                                entitiy.TargetEntity = targetEntity;
                                            }
                                            else
                                            {
                                                entitiy.TargetEntity = null;
                                            }
                                        }
                                        else
                                        {
                                            entitiy.TargetEntity = null;
                                        }

                                    }
                                    else if (item.StartsWith("SPAWN:"))
                                    {
                                        var itemToSpawn = item.Substring("SPAWN:".Length, item.Length - "SPAWN:".Length);

                                        try
                                        {
                                            var enviormentItem = JsonConvert.DeserializeObject<EnviromentItem>(itemToSpawn);
                                            enviormentItem = worldEnviorment.AddItem(enviormentItem.ItemType, enviormentItem.Position);
                                            
                                            dataToSend.Enqueue(($"SPAWN:{JsonConvert.SerializeObject(enviormentItem)}<EOF>", null));
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                }

                                Console.WriteLine("Text received : {0}", data);
                            }

                        }
                        catch (Exception)
                        {

                        }
                        finally
                        {
                            if(SocketToEntity.ContainsKey(socketToUse))
                            {
                                var entityToRemove = SocketToEntity[socketToUse];

                                IdToSocket.Remove(entityToRemove.Id);
                                idToEntity.Remove(entityToRemove.Id);
                                SocketToEntity.Remove(socketToUse);

                                listOfSockets.Remove(socketToUse);
                                listOfEntities.Remove(entityToRemove);

                                foreach (var item in PassCodeToSocket)
                                {
                                    if(item.Value.entity == entityToRemove)
                                    {
                                        PassCodeToSocket.Remove(item.Key);
                                        break;
                                    }
                                }
                                dataToSend.Enqueue(($"DEL:{entityToRemove.Id}<EOF>", socketToUse));
                            }
                            //var listOfEntities = new List<Entity>();
                            //var listOfSockets = new List<Socket>();
                            
                            //var IdToSocket = new Dictionary<string, (Socket socket, Entity entity)>();
                            //var ListOfTrees = new List<Vector2>();
                        }

                    });
                    thrd.Start(handler);
                }
                
                //Console.WriteLine("Text received : {0}", data);

                //byte[] msg = Encoding.ASCII.GetBytes(data);
                //handler.Send(msg);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }
    }
}
