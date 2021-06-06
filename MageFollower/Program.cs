using MageFollower.Client;
using MageFollower.PacketData;
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
            string server = "";
            if(args != null && args.Length > 0)
            {
                server = args[0];
            }
            using (var game = new Game2D(server))
                game.Run();
        }

        static string createPassword(int length)
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
        static Random r = new Random();
        public static void StartServer(string serverHost = "")
        {
            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            IPAddress ipAddress = null;
            if(string.IsNullOrWhiteSpace(serverHost))
            {
                IPHostEntry host = Dns.GetHostEntry("localhost");
                ipAddress = host.AddressList[0];
            }
            else
            {
                ipAddress = IPAddress.Parse(serverHost);
            }
            
            IPEndPoint localEndPoint = new(ipAddress, 11000);

            var listOfEntities = new List<Entity>();            
            var listOfSockets = new List<Socket>();
            var SocketToEntity = new Dictionary<Socket, Entity>();
            var PassCodeToSocket = new Dictionary<string, (Socket socket, Entity entity)>();
            var IdToSocket = new Dictionary<string, (Socket socket, Entity entity)>();
            var worldEnviorment = new Enviroment();
            var idToEntity = new Dictionary<string, Entity>();
            var listOfProjectTiles = new List<ProjectTile>();
            var listOfEnemies = new List<Entity>();

            var enemy = new Entity()
            {
                MaxHealth = 20,
                Health = 20,
                Color = Color.Red,
                Id = "-1",
                Name = "Bot Red",
                Speed = 250,
                ElementType = ElementType.Fire,
                RightHand = new World.Items.Item()
                {
                    Equipt = World.Items.EquiptType.Magic,
                    Power = 1.1f,
                    Type = World.Items.ItemType.Wood_Ring
                }
            };            
            listOfEnemies.Add(enemy);

            idToEntity.Add(enemy.Id, enemy);

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

                void AttackTarget(Entity item, Entity targetItem, bool processProjectTile = true)
                {
                    var projectTile = item.GetProjectTileType();

                    if (projectTile == Projectiles.ProjectileTypes.None || !processProjectTile)
                    {
                        // instant attack.. // todo init animation
                        var startingHealth = targetItem.Health;
                        item.AttackTarget(targetItem, r.NextDouble());
                        var newHealth = targetItem.Health;
                        if (!targetItem.IsAlive)
                        {
                            Skill skill = null;
                            string nameofSkill = "";
                            switch (projectTile)
                            {
                                case Projectiles.ProjectileTypes.None:
                                    skill = item.Melee;
                                    nameofSkill = nameof(item.Melee);
                                    item.Melee.AddXp(100);                                    
                                    break;
                                case Projectiles.ProjectileTypes.EnergyBall:
                                    skill = item.Magic;
                                    nameofSkill = nameof(item.Magic);
                                    break;
                                case Projectiles.ProjectileTypes.Arrow:
                                    skill = item.Ranged;                                    
                                    nameofSkill = nameof(item.Ranged);
                                    break;
                                default:
                                    break;
                            }

                            if (skill != null)
                            {
                                skill.AddXp(100);
                                dataToSend.Enqueue(($"ADDXP:{item.Id}:{JsonConvert.SerializeObject(new XpToTarget() { Level = nameofSkill, Xp = 100 })}<EOF>", null));
                            }                            
                        }
                        dataToSend.Enqueue(($"DMG:{targetItem.Id}:{JsonConvert.SerializeObject(new DamageToTarget() { DamageDone = startingHealth - newHealth, HealthToSet = newHealth })}<EOF>", null));
                    }
                    else
                    {
                        // get distance.
                        var preTarget = item.TargetEntity;
                        var distance = Vector2.Distance(item.Position, targetItem.Position);
                        var newProjectTile = new ProjectTile() { 
                            ExpireMs = (distance / item.GetProjectTileSpeed()) * 1000.0f,
                            FromId = item.Id,
                            Guid = Guid.NewGuid(),
                            OnExpire = () => {
                                AttackTarget(item, preTarget, false);
                            },
                            ProjectileTypes = projectTile,
                            ToId = targetItem.Id
                        };

                        if(item.ElementType == ElementType.Fire)
                        {
                            newProjectTile.Color = Color.Red;
                        }else if (item.ElementType == ElementType.Water)
                        {
                            newProjectTile.Color = Color.DodgerBlue;
                        }
                        else if (item.ElementType == ElementType.Air)
                        {
                            newProjectTile.Color = Color.WhiteSmoke;
                        }

                        listOfProjectTiles.Add(newProjectTile);
                        dataToSend.Enqueue(($"NEW-P:{JsonConvert.SerializeObject(newProjectTile)}<EOF>", null));
                    }                    
                }

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
                            if(item.TaskSleep > 0)
                            {
                                item.TaskSleep -= st.ElapsedMilliseconds;
                                if (item.TaskSleep < 0)
                                    item.TaskSleep = 0;
                            }

                            if (item.TargetEntity != null)
                            {
                                if (!item.TargetEntity.IsAlive || !item.IsAlive)
                                {
                                    item.TargetEntity = null;
                                }
                                else
                                {
                                    if (VectorHelper.AreInRange(item.GetAttackRange(), item.Position, item.TargetEntity.Position))
                                    {
                                        //targetPos = null;
                                        if (item.AttackSleep == 0)
                                        {
                                            item.AttackSleep = item.GetAttackSpeed(); // 1 second cool down for aa
                                            AttackTarget(item, item.TargetEntity);
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
                            else if(item.TaskTarget != null)
                            {
                                if (VectorHelper.AreInRange(Entity.MeleeRange, item.Position, item.TaskTarget.Position))
                                {
                                    //targetPos = null;
                                    if (item.TaskSleep == 0)
                                    {
                                        switch (item.TaskTarget.GetTaskType())
                                        {
                                            case TaskType.None:
                                                break;
                                            case TaskType.ChopTree:
                                                item.TaskSleep = 2500;

                                                if (r.Next(1, 100) <= item.Wood_Cutting.Level)
                                                {
                                                    // Make Server class and add func
                                                    item.TaskSleep = 0;
                                                    item.TaskTarget = null;

                                                    try
                                                    {
                                                        if (worldEnviorment.EnviromentItems.TryRemove(item.TaskTarget.Guid, out EnviromentItem enviormentItem))
                                                        {
                                                            dataToSend.Enqueue(($"DESPAWN:{JsonConvert.SerializeObject(enviormentItem)}<EOF>", null));
                                                        }
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                    if (item.Wood_Cutting != null)
                                                    {
                                                        // TODO: Make Func
                                                        item.Wood_Cutting.AddXp(100);
                                                        dataToSend.Enqueue(($"ADDXP:{item.Id}:{JsonConvert.SerializeObject(new XpToTarget() { Level = nameof(item.Wood_Cutting), Xp = 100 })}<EOF>", null));
                                                    }
                                                }
                                                else
                                                {
                                                    // send message failed..
                                                }
                                                break;
                                            default:
                                                break;
                                        }
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

                        for (int i = listOfProjectTiles.Count - 1; i >= 0; i--)
                        {
                            var item = listOfProjectTiles[i];

                            item.ExpireMs -= st.ElapsedMilliseconds;
                            if(item.ExpireMs <= 0)
                            {
                                item.ExpireMs = 0;
                                listOfProjectTiles.RemoveAt(i);
                                item.OnExpire?.Invoke();
                            }
                        }

                        st.Restart();
                    }
                });
                playersDispatchThread.Start();

                var processAIThread = new Thread(() =>
                {
                    var r = new Random();
                    var listToDelete = new List<Entity>();
                    var st = Stopwatch.StartNew();
                    while(true)
                    {
                        Thread.Sleep(1000 / 30);
                        
                        foreach (var item in listOfEnemies)
                        {
                            if(!item.IsAlive)
                            {
                                listToDelete.Add(item);
                                continue;
                            }

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
                                                               
                                if (VectorHelper.AreInRange(item.GetAttackRange(), item.Position, item.TargetEntity.Position))
                                {
                                    //targetPos = null;
                                    if(item.AttackSleep == 0)
                                    {
                                        item.AttackSleep = item.GetAttackSpeed(); // 1 second cool down for aa
                                        // support ranged attack.
                                        AttackTarget(item, item.TargetEntity);                                        
                                    }

                                    if (item.GetProjectTileType() != Projectiles.ProjectileTypes.None)
                                    {
                                        Vector2 dPos = item.Position - item.TargetEntity.Position;
                                        var newRotation = (float)Math.Atan2(dPos.Y, dPos.X);
                                        if(newRotation != item.Rotation)
                                        {
                                            item.Rotation = newRotation;
                                            dataToSend.Enqueue(($"POS:{item.Id}:{JsonConvert.SerializeObject(new Transform() { Rotation = item.Rotation, Position = item.Position })}<EOF>", null));
                                        }                                        
                                    }
                                }
                                else
                                {
                                    Vector2 dir = item.TargetEntity.Position - item.Position;

                                    Vector2 dPos = item.Position - item.TargetEntity.Position;

                                    dir.Normalize();
                                    item.Position += dir * item.Speed * (float)st.ElapsedMilliseconds / 1000.0f;
                                    item.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);

                                    dataToSend.Enqueue(($"POS:{item.Id}:{JsonConvert.SerializeObject(new Transform() { Rotation = item.Rotation, Position = item.Position })}<EOF>", null));
                                }
                            }
                        }

                        if(listToDelete.Count > 0)
                        {
                            foreach (var item in listToDelete)
                            {
                                dataToSend.Enqueue(($"DEL:{item.Id}<EOF>", null));
                                listOfEnemies.Remove(item);
                            } // maybe spawn random new one?

                            listToDelete.Clear();
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
                                        if(item.Connected)
                                        {
                                            try
                                            {
                                                item.Send(msg);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }                                            
                                    }
                                }
                                else
                                {
                                    foreach (var item in keys)
                                    {
                                        if(item != dataPack.exclude && item.Connected)
                                        {
                                            try
                                            {
                                                item.Send(msg);
                                            }
                                            catch (Exception)
                                            {

                                            }
                                            
                                        }
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
                                        var chance = r.NextDouble();
                                        var newEntity = new Entity()
                                        {
                                            Health = 1000,
                                            MaxHealth = 1000,
                                            Name = "Human " + i.ToString(),
                                            Speed = 350,
                                            Id = i.ToString()
                                        };
                                        if (chance < 0.5f)
                                        {
                                            newEntity.ElementType = ElementType.Fire;
                                            newEntity.Color = Color.OrangeRed;
                                            newEntity.RightHand = new World.Items.Item()
                                            {
                                                Equipt = World.Items.EquiptType.Ranged,
                                                Power = 1.1f,
                                                Type = World.Items.ItemType.Wood_Bow
                                            };
                                            newEntity.Ranged.AddXp(10000);
                                        }
                                        else  if (chance < 1f)
                                        {
                                            newEntity.ElementType = ElementType.Water;
                                            newEntity.Color = Color.DodgerBlue;
                                            newEntity.RightHand = new World.Items.Item()
                                            {
                                                Equipt = World.Items.EquiptType.Magic,
                                                Power = 1.1f,
                                                Type = World.Items.ItemType.Wood_Ring
                                            };
                                            newEntity.Magic.AddXp(10000);
                                        }
                                        else
                                        {
                                            newEntity.ElementType = ElementType.Air;
                                            newEntity.Color = Color.SeaShell;
                                            newEntity.RightHand = new World.Items.Item()
                                            {
                                                Equipt = World.Items.EquiptType.Physical,
                                                Power = 1.2f,
                                                Type = World.Items.ItemType.Stick
                                            };
                                            newEntity.Melee.AddXp(10000);
                                        }

                                        newEntity.LeftHand = new World.Items.Item()
                                        {
                                            Equipt = World.Items.EquiptType.LeftHand,
                                            Power = 0.0f,
                                            Tool = World.Items.ToolType.Axe,
                                            Type = World.Items.ItemType.Stone_Axe
                                        };

                                        var newPass = createPassword(10);

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
                                                msg = Encoding.ASCII.GetBytes($"SPAWN:{JsonConvert.SerializeObject(itemToSpawn.Value)}<EOF>");
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
                                        var entity = SocketToEntity[socketToUse];

                                        entity.TaskTarget = null;

                                        if (string.CompareOrdinal(newTarget, "NULL") != 0 && idToEntity.ContainsKey(newTarget))
                                        {
                                            var targetEntity = idToEntity[newTarget];
                                            
                                            if (targetEntity != null && targetEntity != entity && targetEntity.IsAlive && entity.IsAlive)
                                            {
                                                entity.TargetEntity = targetEntity;
                                            }
                                            else
                                            {
                                                entity.TargetEntity = null;
                                            }
                                        }
                                        else
                                        {
                                            entity.TargetEntity = null;
                                        }

                                    }else if(item.StartsWith("TASK-TARGET:"))
                                    {
                                        var newTarget = item.Substring("TASK-TARGET:".Length, item.Length - "TASK-TARGET:".Length);
                                        var entity = SocketToEntity[socketToUse];
                                        var idGuid = Guid.Parse(newTarget);

                                        entity.TargetEntity = null;

                                        if (string.CompareOrdinal(newTarget, "NULL") != 0 && worldEnviorment.EnviromentItems.ContainsKey(idGuid))
                                        {
                                            var targetEntity = worldEnviorment.EnviromentItems[idGuid];
                                            if (targetEntity != null && entity.IsAlive)
                                            {
                                                // can Entity interact with enviroment
                                                if (entity.CanInteractWithEnviroment(targetEntity))
                                                {
                                                    entity.TaskTarget = targetEntity;
                                                }
                                                else
                                                {
                                                    entity.TaskTarget = null;
                                                }                                                
                                            }
                                            else
                                            {
                                                entity.TaskTarget = null;
                                            }
                                        }
                                        else
                                        {
                                            entity.TaskTarget = null;
                                        }                            
                                        // TODO Send Message - can't do for x reason
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
                                    }else if(item.StartsWith("DESPAWN:"))
                                    {
                                        var itemToSpawn = item.Substring("DESPAWN:".Length, item.Length - "DESPAWN:".Length);

                                        try
                                        {
                                            
                                            var enviormentItem = JsonConvert.DeserializeObject<EnviromentItem>(itemToSpawn);

                                            if (worldEnviorment.EnviromentItems.TryRemove(enviormentItem.Guid, out enviormentItem))
                                            {
                                                dataToSend.Enqueue(($"DESPAWN:{JsonConvert.SerializeObject(enviormentItem)}<EOF>", null));
                                            }
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
                                // TODO MAKE CONCURRENT.
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
