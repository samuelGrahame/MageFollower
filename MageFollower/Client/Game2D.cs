using MageFollower.World;
using MageFollower.World.Element;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static MageFollower.Program;

namespace MageFollower.Client
{
    public class Game2D : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D texture;
        private Texture2D person01;
        private Texture2D moveToX;
        private Texture2D healthBarBase;

        private SpriteFont font;

        private float NintyRadius = 90.0f * (float)Math.PI / 180.0f; // (float)Math.PI; // x*pi/180
        private float WorldRotation = 0.0f;
        private float WorldZoom = 0.5f;

        private float MouseScale = 0.0f;

        private Entity Player;
        private List<Entity> Entities;
        private Dictionary<string, Entity> EntitiesById = new Dictionary<string, Entity>();


        private List<Vector2> ListOfTrees = new List<Vector2>();

        private Vector2? targetPos = null;

        private Matrix _transform;

        public Game2D()
        {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferMultiSampling = true;

            IsFixedTimeStep = true;  //Force the game to update at fixed time intervals
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0f);  //Set the time interval to 1/30th of a second            
            
        }

        private double LastTimeSentToServer = 0;

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //_graphics.IsFullScreen = true;
            //_graphics.PreferredBackBufferWidth = 1920;
            //_graphics.PreferredBackBufferHeight = 1080;

            //_graphics.ApplyChanges();

            Entities = new List<Entity>();
            
            StartClient();

            //var fireMelee = new Entity()
            //{
            //    ElementType = ElementType.Fire,
            //    Health = 100,
            //    MaxHealth = 100,
            //    Name = "The Fire Swordsman",
            //    Color = Color.LightSalmon
            //};
            //fireMelee.Melee.AddXp(10000.0f);
            //fireMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //Entities.Add(fireMelee);

            //Player = new Entity()
            //{
            //    ElementType = ElementType.Water,
            //    Health = 100,
            //    MaxHealth = 100,
            //    Name = "The Water Swordsman",
            //    Color = Color.LightBlue
            //};
            //Player.Melee.AddXp(10000.0f);
            //Player.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //Entities.Add(Player);

            base.Initialize();
        }
        protected override void UnloadContent()
        {
            base.UnloadContent();

            // Release the socket.    
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
        protected override void LoadContent()
        {
            IsMouseVisible = true;
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            texture = Content.Load<Texture2D>("Trees/Tree01");
            person01 = Content.Load<Texture2D>("People/Person01_Idle");
            moveToX = Content.Load<Texture2D>("Other/MoveToX");

            font = Content.Load<SpriteFont>("Fonts/SegoeUI");

            healthBarBase = Content.Load<Texture2D>("Other/HealthBarBase");            
            // TODO: use this.Content to load your game content here
        }


        public Matrix get_transformation(GraphicsDevice graphicsDevice)
        {
            var playerPos = Player == null ? Vector2.Zero : Player.Position;

            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-playerPos.X, -playerPos.Y, 0)) *
                                         Matrix.CreateRotationZ(WorldRotation) *
                                         Matrix.CreateScale(new Vector3(WorldZoom, WorldZoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(graphicsDevice.Viewport.Width * 0.5f,
                                         graphicsDevice.Viewport.Height * 0.5f, 0));
            return _transform;
        }

        bool AreInRange(float range, Vector2 v1, Vector2 v2)
        {
            var dx = v1.X - v2.X;
            var dy = v1.Y - v2.Y;
            return dx * dx + dy * dy < range * range;
        }

        private MouseState prevMouseState;

        private Socket sender;
        private string passCode;
        private string playerId;

        public void StartClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.    
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());
                    // TODO ALLOW TO LOAD FROM PASS CODE
                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes("NEW:<EOF>");
                    // Send the data through the socket.    
                    int bytesSent = sender.Send(msg);

                    var sendDataToServer = new Thread(() =>
                    {
                        while (true)
                        {
                            if (dataToSend.Count > 0)
                            {
                                var dataPack = dataToSend.Dequeue();
                                byte[] msg = Encoding.ASCII.GetBytes(dataPack);
                                sender.Send(msg);
                            }
                            Thread.Sleep(1);
                        }
                    });
                    sendDataToServer.Start();

                    var thrd = new Thread(() =>
                    {
                        // Incoming data from the client.
                        string leftOver = "";
                        while (true)
                        {
                            string data = leftOver;
                            byte[] bytes = null;

                            while (true)
                            {
                                bytes = new byte[1024];
                                int bytesRec = sender.Receive(bytes);
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

                                if (item.StartsWith("PASS:"))
                                {
                                    // after i connect
                                    var PassAndId = item.Substring("PASS:".Length, item.Length - "PASS:".Length);

                                    var arrary = PassAndId.Split(":");

                                    passCode = arrary[0];
                                    playerId = arrary[1];


                                    if (EntitiesById.ContainsKey(playerId))
                                    {
                                        Player = EntitiesById[playerId];
                                    }
                                }
                                else if (item.StartsWith("NEW:"))
                                {
                                    var newPlayer = item.Substring("NEW:".Length, item.Length - "NEW:".Length);
                                    var newEntity = JsonConvert.DeserializeObject<Entity>(newPlayer);

                                    if (string.CompareOrdinal(newEntity.Id, playerId) == 0)
                                    {
                                        Player = newEntity;
                                    }

                                    if (!EntitiesById.ContainsKey(newEntity.Id))
                                    {
                                        EntitiesById[newEntity.Id] = newEntity;
                                        Entities.Add(newEntity);
                                    }
                                }
                                else if (item.StartsWith("POS:"))
                                {
                                    var IdAndTransform = item.Substring("POS:".Length, item.Length - "POS:".Length);
                                    var placeOfSemi = IdAndTransform.IndexOf(":");
                                    var id = IdAndTransform.Substring(0, placeOfSemi);

                                    if (string.CompareOrdinal(id, playerId) != 0)
                                    {
                                        var transform = JsonConvert.DeserializeObject<Transform>(IdAndTransform.Substring(placeOfSemi + 1));
                                        if (EntitiesById.ContainsKey(id))
                                        {
                                            var entity = EntitiesById[id];
                                            entity.TargetPos = transform.Position;
                                            entity.TargetRotation = transform.Rotation;
                                            entity.TotalTimeLerp = 0;
                                            entity.LerpToTarger = true;
                                        }
                                    }
                                }else if(item.StartsWith("SPAWN:"))
                                {
                                    var itemToSpawn = item.Substring("SPAWN:".Length, item.Length - "SPAWN:".Length);

                                    var indexOfSem = itemToSpawn.IndexOf(":");
                                    if(indexOfSem > -1)
                                    {
                                        string itemType = itemToSpawn.Substring(0, indexOfSem);
                                        string posToSpawn = itemToSpawn.Substring(indexOfSem + 1);

                                        if (String.CompareOrdinal(itemType, "TREE") == 0)
                                        {
                                            try
                                            {
                                                var vectorToPlace = JsonConvert.DeserializeObject<Vector2>(posToSpawn);//ListOfTrees
                                                ListOfTrees.Add(vectorToPlace);                                                
                                            }
                                            catch (Exception)
                                            {

                                            }
                                        }
                                    }

                                    
                                    //dataToSend.Append($"SPAWN:TREE:ME<EOF>");
                                }else if(item.StartsWith("DEL:"))
                                {
                                    var idToRemove = item.Substring("DEL:".Length, item.Length - "DEL:".Length);
                                    if (string.CompareOrdinal(idToRemove, playerId) == 0)
                                    {
                                        Player = null;
                                    }

                                    if (!EntitiesById.ContainsKey(idToRemove))
                                    {
                                        var enttiy = EntitiesById[idToRemove];
                                        Entities.Remove(enttiy);
                                        EntitiesById.Remove(idToRemove);
                                    }
                                    //DEL: { entityToRemove.Id}< EOF >
                                }
                            }                            
                           // Console.WriteLine("Text received : {0}", data);
                        }
                        
                    });
                    thrd.Start();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private Queue<string> dataToSend = new Queue<string>();

        private double MoveTimer = (1000 / 30.0f);
        private Vector2 prevPos;
        private float prevRotation;

        private KeyboardState prevKeyboardState;

        private void SpawnItemAtPos(string type, Vector2 pos)
        {
            if(type == "TREE")
            {
                pos -= new Vector2(-35, 150);
            }
            dataToSend.Enqueue($"SPAWN:{type}:{JsonConvert.SerializeObject(pos)}<EOF>");
            if (type == "TREE")
            {
                ListOfTrees.Add(pos);
            }
        }

        private void SpawnItemAtMe(string type)
        {            
            dataToSend.Enqueue($"SPAWN:{type}:ME<EOF>");
            if(type == "TREE")
            {
                ListOfTrees.Add(Player.Position - new Vector2(-35, 150));
            }
        }

        private Vector2 GetMouseWorldPos(MouseState mouseState)
        {
            var ms = mouseState.Position;
            Matrix inverseTransform = Matrix.Invert(_transform);
            return  Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            var keyboardState = Keyboard.GetState();           
            var mouseState = Mouse.GetState();

            if (Player != null)
            {                
                Vector2 vectorToMove = Vector2.Zero;

                if (keyboardState.IsKeyDown(Keys.W))
                    vectorToMove.Y -= 1;
                if (keyboardState.IsKeyDown(Keys.S))
                    vectorToMove.Y += 1;
                if (keyboardState.IsKeyDown(Keys.A))
                    vectorToMove.X -= 1;
                if (keyboardState.IsKeyDown(Keys.D))
                    vectorToMove.X += 1;

                if (mouseState.RightButton == ButtonState.Pressed && prevMouseState.RightButton == ButtonState.Released)
                        SpawnItemAtPos("TREE", GetMouseWorldPos(mouseState)); //Player.AttackTarget(Player, 10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);

                if (mouseState.LeftButton == ButtonState.Pressed)
                {                    
                    targetPos = GetMouseWorldPos(mouseState);

                    MouseScale = 1.0f;
                }

                if (Vector2.Zero != vectorToMove)
                {
                    targetPos = null;

                    Player.Position += ((vectorToMove * Player.Speed) *
                        (float)gameTime.ElapsedGameTime.TotalSeconds);
                }

                if (targetPos != null)
                {
                    if (MouseScale > 0.7f)
                    {
                        MouseScale -= 2f *
                            (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }

                    if (AreInRange(3.0f, Player.Position, targetPos.Value))
                    {
                        targetPos = null;
                    }
                    else
                    {
                        Vector2 dir = targetPos.Value - Player.Position;

                        Vector2 dPos = Player.Position - targetPos.Value;

                        dir.Normalize();
                        Player.Position += dir * Player.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        Player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                    }
                }

                if (targetPos == null)
                {
                    var ms = mouseState.Position;
                    Matrix inverseTransform = Matrix.Invert(_transform);

                    Vector2 dPos = Player.Position - Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);

                    Player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                }

                LastTimeSentToServer += gameTime.ElapsedGameTime.TotalMilliseconds;

                if (LastTimeSentToServer > MoveTimer && 
                    (prevPos != Player.Position || prevRotation != Player.Rotation))
                {
                    LastTimeSentToServer = 0;
                    dataToSend.Enqueue($"POS:{JsonConvert.SerializeObject(new Transform() { Position = Player.Position, Rotation = Player.Rotation })}<EOF>");

                    prevPos = Player.Position;
                    prevRotation = Player.Rotation;
                }                
                //PacketsToSend
            }

            if (prevMouseState.ScrollWheelValue != mouseState.ScrollWheelValue)
            {
                WorldZoom += (mouseState.ScrollWheelValue - prevMouseState.ScrollWheelValue) * 0.1f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (WorldZoom > 3)
                    WorldZoom = 3;
                if (WorldZoom < 0.5f)
                    WorldZoom = 0.5f;

            }

            foreach (var item in Entities)
            {
                if(item != Player && item.LerpToTarger)
                {
                    item.TotalTimeLerp += (float)gameTime.ElapsedGameTime.TotalMilliseconds;                    
                    var percent = (float)(item.TotalTimeLerp / MoveTimer);
                    if (item.TotalTimeLerp >= MoveTimer)
                    {
                        item.TotalTimeLerp = MoveTimer;
                        item.Position = item.TargetPos;
                        item.LerpToTarger = false;
                    }
                    if (item.TargetPos != item.Position)
                    {
                        item.Position = Vector2.LerpPrecise(item.Position, item.TargetPos, percent);
                    }

                    if (item.TargetRotation != item.Rotation)
                        item.Rotation = item.TargetRotation;//; MathHelper.Lerp(item.Rotation, item.TargetRotation, percent);
                }
            }
            
            prevMouseState = mouseState;
            prevKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);

            // TODO: Add your drawing code here
            _spriteBatch.Begin(SpriteSortMode.BackToFront, 
                BlendState.AlphaBlend, 
                SamplerState.AnisotropicClamp, 
                null, null, null, get_transformation(GraphicsDevice));

            // _spriteBatch.Draw(person01, playerPos, Color.White);
            foreach (var item in Entities)
            {
                var rotation = item.Rotation - NintyRadius;
                _spriteBatch.Draw(person01,
                        item.Position - new Vector2(15, 15),
                        null,
                        new Color(Color.Black, 0.2f),
                        rotation,
                        Entity.Origin,
                        1.0f,
                        SpriteEffects.None,
                        0.9f);

                _spriteBatch.Draw(person01,
                         item.Position,
                         null,
                         item.Color,
                         rotation,
                         Entity.Origin,
                         1.0f,
                         SpriteEffects.None,
                         1.0f);

                Vector2 size = font.MeasureString(item.Name);                
                Vector2 origin = size * 0.5f;

                _spriteBatch.DrawString(font, item.Name, item.Position -
                    new Vector2(128 - origin.X - 25, (100)),
                    Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0);

                var heathPos = item.Position - new Vector2(50, 80);

                _spriteBatch.Draw(healthBarBase,
                    heathPos, null, Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);

                _spriteBatch.Draw(healthBarBase,
                    heathPos, 
                    new Rectangle(0, 0, (int)(100.0f * (item.Health / item.MaxHealth)), 10), 
                    item == Player && Player != null ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.1f);

                //_spriteBatch.Draw(healthBarBase,
                //    item.Position, null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0.0f);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y, 100, 10),
                //    null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y,
                //    (int)(100.0f * (item.Health / item.MaxHealth)), 10),
                //    null, item == Player ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, SpriteEffects.None, 0);
            }

            foreach (var item in ListOfTrees)
            {
                _spriteBatch.Draw(texture,
                item - new Vector2(15, 15),
                 null,
                 new Color(Color.Black, 0.2f),
                 1.0f,
                 Vector2.Zero,
                 1.0f,
                 SpriteEffects.None,
                 0.2f);

                _spriteBatch.Draw(texture,
                             item,
                             null,
                             Color.White,
                             1.0f,
                             Vector2.Zero,
                             1.0f,
                             SpriteEffects.None,
                             0.1f);
            }

            

            if (targetPos != null)
            {
                if (MouseScale > 0.7f)
                {
                    _spriteBatch.Draw(moveToX,
                         targetPos.Value,
                         null,
                         Color.White,
                         0.0f,
                         new Vector2(16, 16),
                         MouseScale,
                         SpriteEffects.None,
                         0f);
                }
                
            }
            

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
