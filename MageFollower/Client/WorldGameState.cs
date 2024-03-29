﻿using MageFollower.Creator;
using MageFollower.PacketData;
using MageFollower.Particle;
using MageFollower.Projectiles;
using MageFollower.UI;
using MageFollower.Utilities;
using MageFollower.World;
using MageFollower.World.BackgroundTile;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MageFollower.Client
{
    public class WorldGameState : GameState
    {
        private Texture2D _person01;
        private Texture2D _personGhost01;
        private Texture2D _moveToX;
        private Texture2D _healthBarBase;
        private Texture2D _targetCircle;
        
        private Dictionary<EnviromentType, Texture2D> enviromentTextures = new();
        private Dictionary<ProjectileTypes, Texture2D> projectileTypesTextures = new();

        private float _nintyRadius = 90.0f * (float)Math.PI / 180.0f; // (float)Math.PI; // x*pi/180
        private float _worldRotation = 0.0f;
        public float WorldZoom = 1f;
        private float _mousePressScale = 0.0f;
        private Entity _player;
        //private List<Entity> _entities;
        private ConcurrentDictionary<string, Entity> _entitiesById = new();
        private ConcurrentDictionary<Guid, ProjectTile> _projectTilesById = new();
        private World.Enviroment _worldEnviroment = new();


        private Vector2? _targetPos = null;
        private Entity _targetEntity = null;
        private Matrix _transform;

        private EnviromentItem _taskTarget = null;

        private Socket _sender;
        private string _passCode;
        private string _playerId;
        //private double _lastTimeSentToServer = 0;

        private ConcurrentQueue<string> _dataToSend = new();
        private ConcurrentDictionary<Guid, FloatingDamageText> _floatingTextList = new();

        //public static double _moveLerpTimer = (1000 / 10.0f);
        //private double _sendToServerTimer = (1000 / 20.0f);
        private Vector2 _prevPos;
        private float _prevRotation;
        private EnviromentType _itemTpAddOnRightClick = EnviromentType.Tree01;
        
        private EditingMode _mode = EditingMode.None;
        private bool _isMouseDown = false;
        private Vector2 _mouseDownStart;

        private UITextBox _commandTextBoxUI;
        private UIColorPicker _chooseColorDrawMode;

        private string _server;

        private bool stopGameLoop = false;

        // TODO CREATE DRAWING LAYER WHILE DRAWING then Join to Mesh after Moue Up... allows for blend layer...
        private BackgroundDisplayTile _backgroundDisplayTile = null;

        public WorldGameState(Game2D client) : base(client)
        {
            
        }

        public override void Unload()
        {
            // Release the socket.    
            stopGameLoop = true;
            try
            {
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
            }
            catch (Exception)
            {

            }
        }

        public Matrix get_transformation(GraphicsDevice graphicsDevice)
        {
            var playerPos = _player == null ? Vector2.Zero : _player.Position;

            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-playerPos.X, -playerPos.Y, 0)) *
                                         Matrix.CreateRotationZ(_worldRotation) *
                                         Matrix.CreateScale(new Vector3(WorldZoom, WorldZoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(graphicsDevice.Viewport.Width * 0.5f,
                                         graphicsDevice.Viewport.Height * 0.5f, 0));
            return _transform;
        }

        public override void Load()
        {
            Container = new UIContainer(this.Client);
            Container.Children.Add(_commandTextBoxUI = new UITextBox(base.Client)
            {
                Position = new Vector2(10, 65),
                Color = Color.White,
                FontScale = 1.0f,
                Font = Client.FontSmall
            });

            Container.Children.Add(_chooseColorDrawMode = new UIColorPicker(base.Client)
            {
                Position = new Vector2(10, 85),
                Hidden = true,
                Clicked = (sender, input) => {
                    if (_mode != EditingMode.DrawBackground)
                        return;
                    DrawColor = sender.GetColorFromInputHandler(DrawColor, input);
                },
                MouseDownMoved = (sender, input) => {
                    if (_mode != EditingMode.DrawBackground)
                        return;
                    DrawColor = sender.GetColorFromInputHandler(DrawColor, input);
                }
            });
            //_chooseColorDrawMode

            enviromentTextures.Add(
                EnviromentType.Tree01,
                Content.Load<Texture2D>("Trees/Tree01"));

            enviromentTextures.Add(
                EnviromentType.Tree02,
                Content.Load<Texture2D>("Trees/Tree02"));

            projectileTypesTextures.Add(
                ProjectileTypes.None,
                null); // allows for get ranged.

            projectileTypesTextures.Add(ProjectileTypes.EnergyBall,
                Content.Load<Texture2D>("Projectiles/EnergyBall01"));

            projectileTypesTextures.Add(ProjectileTypes.Arrow,
                Content.Load<Texture2D>("Projectiles/Arrow01"));

            _person01 = Content.Load<Texture2D>("People/Person01_Idle");
            _personGhost01 = Content.Load<Texture2D>("People/Person01_Ghost_Idle");
            _moveToX = Content.Load<Texture2D>("Other/MoveToX");
            _targetCircle = Content.Load<Texture2D>("Other/TargetCircle");

            _healthBarBase = Content.Load<Texture2D>("Other/HealthBarBase");

            _backgroundDisplayTile = new BackgroundDisplayTile(this);

            StartClient();
        }


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

                IPEndPoint remoteEP;

                if (string.IsNullOrWhiteSpace(_server))
                {
                    remoteEP = new IPEndPoint(ipAddress, 11000);
                }
                else
                {
                    remoteEP = new IPEndPoint(IPAddress.Parse(_server), 11000);
                }

                // Create a TCP/IP  socket.    
                _sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    _sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        _sender.RemoteEndPoint.ToString());
                    // TODO ALLOW TO LOAD FROM PASS CODE
                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes("NEW:<EOF>");
                    // Send the data through the socket.    
                    int bytesSent = _sender.Send(msg);

                    var sendDataToServer = new Thread(() =>
                    {
                        while (!stopGameLoop)
                        {
                            if (_dataToSend.Count > 0)
                            {
                                if(_dataToSend.TryDequeue(out string dataPack))
                                {                                    
                                    byte[] msg = Encoding.ASCII.GetBytes(dataPack);
                                    _sender.Send(msg);
                                }                                
                            }
                            Thread.Sleep(1);
                        }
                    });
                    sendDataToServer.Start();

                    var thrd = new Thread(() =>
                    {
                        try
                        {
                            // Incoming data from the client.
                            string leftOver = "";
                            while (!stopGameLoop)
                            {
                                string data = leftOver;
                                byte[] bytes = null;

                                while (!stopGameLoop)
                                {
                                    bytes = new byte[1024];
                                    int bytesRec = _sender.Receive(bytes);
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

                                        _passCode = arrary[0];
                                        _playerId = arrary[1];

                                        _entitiesById.TryGetValue(_playerId, out _player);                                        
                                    }
                                    else if (item.StartsWith("NEW:"))
                                    {
                                        var newPlayer = item.Substring("NEW:".Length, item.Length - "NEW:".Length);
                                        var newEntity = JsonConvert.DeserializeObject<Entity>(newPlayer, JsonHelper.Config);

                                        if (string.CompareOrdinal(newEntity.Id, _playerId) == 0)
                                        {
                                            _player = newEntity;
                                        }

                                        _entitiesById.TryAdd(newEntity.Id, newEntity);                                        
                                    }
                                    else if (item.StartsWith("NEW-P:"))
                                    {
                                        var newPlayer = item.Substring("NEW-P:".Length, item.Length - "NEW-P:".Length);
                                        var projectTile = JsonConvert.DeserializeObject<ProjectTile>(newPlayer, JsonHelper.Config);

                                        if (!_projectTilesById.ContainsKey(projectTile.Guid) &&
                                            _entitiesById.ContainsKey(projectTile.FromId) &&
                                            _entitiesById.ContainsKey(projectTile.ToId))
                                        {
                                            _projectTilesById[projectTile.Guid] = projectTile;
                                        }
                                    }
                                    else if (item.StartsWith("POS:"))
                                    {
                                        var IdAndTransform = item.Substring("POS:".Length, item.Length - "POS:".Length);
                                        var placeOfSemi = IdAndTransform.IndexOf(":");
                                        var id = IdAndTransform.Substring(0, placeOfSemi);

                                        if (string.CompareOrdinal(id, _playerId) != 0)
                                        {
                                            var transform = JsonConvert.DeserializeObject<Transform>(
                                                IdAndTransform.Substring(placeOfSemi + 1), JsonHelper.Config);

                                            if(_entitiesById.TryGetValue(id, out Entity entity))
                                            {                                                
                                                entity.
                                                    AddTarget(transform.Position, transform.Rotation);
                                            }
                                        }
                                    }
                                    else if (item.StartsWith("DMG:"))
                                    {
                                        var IdAndTransform = item.Substring("DMG:".Length, item.Length - "DMG:".Length);
                                        var placeOfSemi = IdAndTransform.IndexOf(":");
                                        var id = IdAndTransform.Substring(0, placeOfSemi);

                                        var transform = JsonConvert.DeserializeObject<DamageToTarget>(IdAndTransform.Substring(placeOfSemi + 1), JsonHelper.Config);
                                        if (_entitiesById.ContainsKey(id))
                                        {
                                            var entity = _entitiesById[id];
                                            entity.Health = transform.HealthToSet;

                                            var dmgDone = Math.Round(transform.DamageDone, 2).ToString();
                                            var size = Client.Font.MeasureString(dmgDone) * 0.5f;

                                            _floatingTextList.TryAdd(Guid.NewGuid(), new FloatingDamageText()
                                            {
                                                Text = dmgDone,
                                                Position = entity.Position - new Vector2(size.X, size.Y),
                                                Color = Color.Red,
                                                TotalTimeToRemove = 500.0f,
                                                StartingTime = 500.0f,
                                                Scale = 1.5f,
                                                AnimationType = FloatingTextAnimationType.MoveUpAndShrink,
                                                DrawColorBackGround = true,
                                                ColorBackGround = Color.Black
                                            });
                                        }
                                    }
                                    else if (item.StartsWith("ADDXP:"))
                                    {
                                        var IdAndTransform = item.Substring("ADDXP:".Length, item.Length - "ADDXP:".Length);
                                        var placeOfSemi = IdAndTransform.IndexOf(":");
                                        var id = IdAndTransform.Substring(0, placeOfSemi);

                                        var xpToTarget = JsonConvert.DeserializeObject<XpToTarget>(IdAndTransform.Substring(placeOfSemi + 1), JsonHelper.Config);
                                        if (_entitiesById.ContainsKey(id))
                                        {
                                            var entity = _entitiesById[id];

                                            if (entity.AddXpToSkill(xpToTarget, out bool didLevelUp))
                                            {
                                                var caption = $"{xpToTarget.Level}: {xpToTarget.Xp} xp";
                                                var size = Client.Font.MeasureString(caption) * 0.5f;

                                                _floatingTextList.TryAdd(Guid.NewGuid(), new FloatingDamageText()
                                                {
                                                    Text = caption,
                                                    Position = entity.Position - new Vector2(size.X, 150.0f),
                                                    Color = Color.CornflowerBlue,
                                                    TotalTimeToRemove = 2000.0f,
                                                    StartingTime = 2000.0f
                                                });

                                                if(didLevelUp)
                                                {
                                                    size = Client.Font.MeasureString("Leveled Up!") * 0.5f;
                                                    _floatingTextList.TryAdd(Guid.NewGuid(), new FloatingDamageText()
                                                    {
                                                        Text = "Leveled Up!",
                                                        Color = Color.Blue,
                                                        TotalTimeToRemove = 2000.0f,
                                                        StartingTime = 2000.0f,
                                                        Position = entity.Position - new Vector2(size.X, 170.0f)
                                                    });
                                                }                                                
                                            }
                                        }
                                    }
                                    else if (item.StartsWith("SPAWN:"))
                                    {
                                        var itemToSpawn = item.Substring("SPAWN:".Length, item.Length - "SPAWN:".Length);
                                        try
                                        {
                                            var enviromentItem = JsonConvert.DeserializeObject<EnviromentItem>(itemToSpawn, JsonHelper.Config);//ListOfTrees                                            
                                            _worldEnviroment.EnviromentItems[enviromentItem.Guid] = enviromentItem;
                                        }
                                        catch (Exception)
                                        {

                                        }
                                        //dataToSend.Append($"SPAWN:TREE:ME<EOF>");
                                    }
                                    else if (item.StartsWith("DESPAWN:"))
                                    {
                                        var itemToSpawn = item.Substring("DESPAWN:".Length, item.Length - "DESPAWN:".Length);

                                        try
                                        {
                                            var enviormentItem = JsonConvert.DeserializeObject<EnviromentItem>(itemToSpawn);
                                            if (_worldEnviroment.EnviromentItems.TryRemove(enviormentItem.Guid, out enviormentItem))
                                            {
                                                
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                    else if (item.StartsWith("DEL:"))
                                    {
                                        var idToRemove = item.Substring("DEL:".Length, item.Length - "DEL:".Length);
                                        if (string.CompareOrdinal(idToRemove, _playerId) == 0)
                                        {
                                            _player = null;
                                        }

                                        _entitiesById.TryRemove(idToRemove, out _);
                                        //DEL: { entityToRemove.Id}< EOF >
                                    }
                                }
                                // Console.WriteLine("Text received : {0}", data);
                            }
                        }
                        catch (Exception)
                        {

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


        private void DeleteElement(EnviromentItem enviromentItem)
        {
            _dataToSend.Enqueue($"DESPAWN:{JsonConvert.SerializeObject(enviromentItem, JsonHelper.Config)}<EOF>");
        }

        private void SetTargetOnServer(Entity entity)
        {
            if (entity == _player)
                entity = null;

            _targetEntity = entity;
            _mousePressScale = 0.0f;
            _taskTarget = null;
            // TODO: Add Intent / or maybe work out - if foe etc
            if (entity == null)
            {
                _dataToSend.Enqueue($"TARGET:NULL<EOF>");
            }
            else
            {
                _dataToSend.Enqueue($"TARGET:{entity.Id}<EOF>");
            }

        }

        private void SetTargetOnEnviroment(EnviromentItem enviromentItem)
        {            
            _targetEntity = null;
            _mousePressScale = 0.0f;
            _taskTarget = enviromentItem;
            // TODO: Add Intent / or maybe work out - if foe etc
            if (enviromentItem == null)
            {
                _dataToSend.Enqueue($"TASK-TARGET:NULL<EOF>");
            }
            else
            {
                _dataToSend.Enqueue($"TASK-TARGET:{enviromentItem.Guid}<EOF>");
            }

        }

        private void SpawnItemAtPos(EnviromentType type, Vector2 pos)
        {
            _dataToSend.Enqueue($"SPAWN:{JsonConvert.SerializeObject(new EnviromentItem() { Position = pos, ItemType = type }, JsonHelper.Config)}<EOF>");
        }

        private Vector2 GetMouseWorldPos(MouseState mouseState)
        {
            var ms = mouseState.Position;
            Matrix inverseTransform = Matrix.Invert(_transform);
            return Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);
        }

        private static Array _enviromentTypes = Enum.GetValues(typeof(EnviromentType));

        private void processPlacingEnviromentKeyPress()
        {
            if(Input.IsKeyPressed(Keys.Up))
            {
                if((EnviromentType)(_enviromentTypes.Length - 1) == _itemTpAddOnRightClick)
                {
                    _itemTpAddOnRightClick = (EnviromentType)(1);
                }
                else
                {
                    _itemTpAddOnRightClick++;
                }
            }else if (Input.IsKeyPressed(Keys.Down))
            {
                if((int)_itemTpAddOnRightClick > 1)
                {
                    _itemTpAddOnRightClick--;
                }
                else
                {
                    _itemTpAddOnRightClick = (EnviromentType)(_enviromentTypes.Length - 1);
                }
                
            }
        }

        private EnviromentItem GetEnviromentItemFromMouse(Vector2 worldMousePos)
        {
            var entities = _worldEnviroment.EnviromentItems.Values;
            var clickedOn = entities.Where(o =>
                VectorHelper.AreInRange(128.0f, o.Position, worldMousePos))?.ToList(); // todo get region size . did click on pixel.

            if (clickedOn != null && clickedOn.Count > 0)
            {
                if (clickedOn.Count == 0)
                {                    
                    return clickedOn[0];
                }
                else
                {
                    return 
                        clickedOn.OrderBy(o =>
                            Vector2.Distance(o.Position, worldMousePos)).
                        FirstOrDefault();
                }

            }
            return null;
        }

        private Entity GetEntityFromMouse(Vector2 worldMousePos)
        {
            var entities = _entitiesById.Values;
            var clickedOn = entities.Where(o =>
                o != _player && VectorHelper.AreInRange(128.0f, o.Position, worldMousePos))?.ToList();

            if (clickedOn != null && clickedOn.Count > 0)
            {
                if (clickedOn.Count == 0)
                {
                    return clickedOn[0];
                }
                else
                {

                    return clickedOn.OrderBy(o =>
                        Vector2.Distance(o.Position, worldMousePos)).
                    FirstOrDefault();
                }
            }

            return null;
        }

        public Color DrawColor = Color.Black;
        public int Thickness = 0;
        public void PrcessTextCommand(string command)
        {
            try
            {
                if (command.ToLower().StartsWith("setcolor:"))
                {
                    var args = command.Split(':');
                    if (args.Length > 1)
                    {
                        if (args[1].Contains(","))
                        {
                            args = args[1].Split(',');
                            if (args.Length == 3)
                            {
                                DrawColor = new Color(
                                    int.Parse(args[0].Trim()),
                                    int.Parse(args[1].Trim()),
                                    int.Parse(args[2].Trim()));
                            }
                            else if (args.Length == 4)
                            {
                                DrawColor = new Color(
                                    int.Parse(args[0].Trim()),
                                    int.Parse(args[1].Trim()),
                                    int.Parse(args[2].Trim()), int.Parse(args[3].Trim()));
                            }
                        }
                    }
                }else if (command.ToLower().StartsWith("setpen:"))
                {
                    var args = command.Split(':');
                    if (args.Length > 1)
                    {
                        Thickness = int.Parse(args[1].Trim());
                        if (Thickness < 0)
                            Thickness = 0;
                        if (Thickness > 50)
                            Thickness = 50;
                    }
                }
            }
            catch (Exception)
            {

            }
            
        }

        public override void Update(GameTime gameTime)
        {
            // test
            base.Update(gameTime);

            base.Container?.Update(Input);

            if (_player != null)
            {
                //Vector2 vectorToMove = Vector2.Zero;

                //if (keyboardState.IsKeyDown(Keys.W))
                //    vectorToMove.Y -= 1;
                //if (keyboardState.IsKeyDown(Keys.S))
                //    vectorToMove.Y += 1;
                //if (keyboardState.IsKeyDown(Keys.A))
                //    vectorToMove.X -= 1;
                //if (keyboardState.IsKeyDown(Keys.D))
                //    vectorToMove.X += 1;

                if (FocusedControl != null)
                {
                    // do we override...
                    FocusedControl.Update(Input); // draw is taken care of with control structure. // list of children / compontents on game window. if is visible draw / update.
                }

                if (Input.IsKeyPressed(Keys.Enter))
                {
                    if (FocusedControl == null)
                    {                        
                        SetFocusedControl(_commandTextBoxUI);
                    }
                    else
                    {
                        if (FocusedControl == _commandTextBoxUI)
                        {
                            PrcessTextCommand(_commandTextBoxUI.Text);
                            _commandTextBoxUI.Text = "";
                        }
                        // allows override?                        
                        SetFocusedControl(null);
                    }
                }

                if (Input.MouseState.RightButton == ButtonState.Pressed && Input.PrevMouseState.RightButton == ButtonState.Released)
                {
                    _isMouseDown = true;
                    // Mouse Down
                    var worldMousePos = GetMouseWorldPos(Input.MouseState);
                    // TODO CHECK UI First.
                    switch (_mode)
                    {
                        case EditingMode.None:
                            var clickedOn = GetEntityFromMouse(worldMousePos);
                            if (clickedOn != null)
                            {
                                SetTargetOnServer(clickedOn);
                            }
                            else
                            {
                                var clickedOnEnviroment = GetEnviromentItemFromMouse(worldMousePos);
                                if (clickedOnEnviroment != null)
                                {
                                    SetTargetOnEnviroment(clickedOnEnviroment);
                                }
                            }

                            // TODO Have Physics engine do a ray cast. just do hard working loop. 
                            break;
                        case EditingMode.PlacingTerrain:
                            var clickedOnTerrain = GetEnviromentItemFromMouse(worldMousePos);

                            if (clickedOnTerrain != null)
                            {
                                DeleteElement(clickedOnTerrain);

                            }
                            else
                            {
                                SpawnItemAtPos(_itemTpAddOnRightClick, worldMousePos); //Player.AttackTarget(Player, 10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                            }
                            break;
                        case EditingMode.DrawBackground:

                            break;
                        default:
                            break;
                    }
                }
                else if (Input.MouseState.RightButton == ButtonState.Released && Input.PrevMouseState.RightButton == ButtonState.Pressed)
                {
                    // Mouse Up
                    _isMouseDown = false;
                }
                else
                {
                    // Move Move
                    if(_isMouseDown)
                    {
                        if(_mode == EditingMode.DrawBackground)
                        {
                            var currentWorldPos = GetMouseWorldPos(Input.MouseState);
                            var prevWorldPos = GetMouseWorldPos(Input.PrevMouseState);

                            var point1 = new Point((int)currentWorldPos.X, (int)currentWorldPos.Y);
                            var point2 = new Point((int)prevWorldPos.X, (int)prevWorldPos.Y);
                            
                            // what we need to do is get 
                            var backgroundTilesToEdit = _backgroundDisplayTile.GetTexturesFromTwoPoints(
                                point1,
                                point2, Thickness);

                            if(backgroundTilesToEdit != null && backgroundTilesToEdit.Count > 0)
                            {
                                foreach (var item in backgroundTilesToEdit)
                                {                                    
                                    Texture2DHelper.GetPixels(item.Texture, ref BackgroundDisplayTile.ImageCache);
                                    var fromX = point1.X - item.Index.X;
                                    var toX = point2.X - item.Index.X;

                                    var fromY = point1.Y - item.Index.Y;
                                    var toY = point2.Y - item.Index.Y;

                                    if(Thickness == 0)
                                    {
                                        Texture2DHelper.DrawLine(ref BackgroundDisplayTile.ImageCache, item.Texture.Width,
                                            item.Texture.Height,
                                            fromX, fromY,
                                            toX, toY, DrawColor);
                                    }
                                    else
                                    {
                                        Texture2DHelper.DrawLineWithCircles(ref BackgroundDisplayTile.ImageCache, item.Texture.Width,
                                            item.Texture.Height,
                                            fromX, fromY,
                                            toX, toY,
                                            Thickness, DrawColor);
                                    }
                                    

                                    //Texture2DHelper.DrawThickLineSimple(ref BackgroundDisplayTile.ImageCache, item.Texture.Width,
                                    //        item.Texture.Height,
                                    //        fromX, fromY,
                                    //        toX, toY,
                                    //        50, Texture2DHelper.LINE_THICKNESS_MIDDLE, new Color(Color.Black, 0.5f));

                                    //for (int i = -5; i < 10; i++)
                                    //{
                                    //    Texture2DHelper.DrawLine(ref pixels,
                                    //        item.Texture.Width,
                                    //        item.Texture.Height,
                                    //        fromX + i, fromY + i,
                                    //        toX + i, toY + i,
                                    //        Color.Black);
                                    //}

                                    //Texture2DHelper.DrawLine(ref pixels,
                                    //    item.Texture.Width,
                                    //    item.Texture.Height,
                                    //    point1.X - item.Index.X, point1.Y - item.Index.Y,
                                    //    point2.X - item.Index.X, point2.Y - item.Index.Y,
                                    //    Color.Black);

                                    item.Texture.SetData(BackgroundDisplayTile.ImageCache);

                                    //_backgroundDisplayTile.SetTextureFromIndex(item.Index, item.Texture);
                                    // DO WE NEED TO SET PIXEL? TO TILE MASTER OR JUST HERE
                                }
                            }   
                        }
                    }
                }

                if (Input.PrevKeyboardState.IsKeyUp(Keys.G) && Input.KeyboardState.IsKeyDown(Keys.G))
                {
                    switch (_mode)
                    {
                        case EditingMode.None:
                            _mode = EditingMode.PlacingTerrain;
                            break;
                        case EditingMode.PlacingTerrain:
                            _mode = EditingMode.DrawBackground;
                            break;
                        case EditingMode.DrawBackground:
                            _mode = EditingMode.None;
                            break;                        
                    }
                    _chooseColorDrawMode.Hidden = _mode != EditingMode.DrawBackground;
                }

                if (_mode == EditingMode.PlacingTerrain)
                {
                    processPlacingEnviromentKeyPress();
                }

                if (Input.MouseState.LeftButton == ButtonState.Pressed)
                {
                    if(FocusedControl == null || !FocusedControl.DoesBlockMouseClick())
                    {
                        _targetPos = GetMouseWorldPos(Input.MouseState);
                        if (_targetEntity != null)
                        {
                            SetTargetOnServer(null);
                        }
                        if (_taskTarget != null)
                        {
                            SetTargetOnEnviroment(null);
                        }
                        _mousePressScale = 1.0f;
                    }                    
                }

                if (_targetEntity != null)
                {
                    if (!_targetEntity.IsAlive)
                    {
                        _targetPos = null;
                        SetTargetOnServer(null);
                    }
                    else
                    {
                        _targetPos = _targetEntity.Position;
                        // are we in range of attack? do we do it from server.
                        if (VectorHelper.AreInRange(_player.GetAttackRange(), _player.Position, _targetEntity.Position))
                        {
                            _targetPos = null;
                        }
                    }
                    // DO NOT WALK IF IN RANGE.
                }

                if(_taskTarget != null)
                {
                    if(_worldEnviroment.EnviromentItems.ContainsKey(_taskTarget.Guid)){
                        _targetPos = _taskTarget.Position;
                        if (VectorHelper.AreInRange(Entity.MeleeRange, _player.Position, _taskTarget.Position))
                        {
                            _targetPos = null;
                        }
                    }
                    else
                    {
                        _taskTarget = null;
                        SetTargetOnEnviroment(null);
                    }
                }

                //if (Vector2.Zero != vectorToMove)
                //{
                //    targetPos = null;

                //    Player.Position += ((vectorToMove * Player.Speed) *
                //        (float)gameTime.ElapsedGameTime.TotalSeconds);
                //}

                if (_targetPos != null)
                {
                    if (_mousePressScale > 0.7f)
                    {
                        _mousePressScale -= 2f *
                            (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }

                    if (VectorHelper.AreInRange(10.0f, _player.Position, _targetPos.Value))
                    {
                        _targetPos = null;
                    }
                    else
                    {
                        Vector2 dir = _targetPos.Value - _player.Position;

                        Vector2 dPos = _player.Position - _targetPos.Value;

                        dir.Normalize();
                        _player.Position += dir * _player.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                    }
                }

                if (_targetPos == null)
                {
                    if (_targetEntity == null && _taskTarget == null)
                    {
                        if (Client.IsActive)
                        {
                            Vector2 dPos = _player.Position - GetMouseWorldPos(Input.MouseState);

                            _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                        }
                    }
                    else
                    {
                        if (_taskTarget != null)
                        {
                            Vector2 dPos = _player.Position - _taskTarget.Position;
                            _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                        }
                        else
                        {
                            Vector2 dPos = _player.Position - _targetEntity.Position;
                            _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                        }
                        
                    }

                }

                //_lastTimeSentToServer += gameTime.ElapsedGameTime.TotalMilliseconds;

                if ((_prevPos != _player.Position || _prevRotation != _player.Rotation)) // _lastTimeSentToServer > _sendToServerTimer &&
                {
                    //_lastTimeSentToServer = 0;
                    _dataToSend.Enqueue($"POS:{JsonConvert.SerializeObject(new Transform() { Position = _player.Position, Rotation = _player.Rotation }, JsonHelper.Config)}<EOF>");

                    _prevPos = _player.Position;
                    _prevRotation = _player.Rotation;
                }
                //PacketsToSend
            }

            if (Input.PrevMouseState.ScrollWheelValue != Input.MouseState.ScrollWheelValue)
            {
                WorldZoom += (Input.MouseState.ScrollWheelValue - Input.PrevMouseState.ScrollWheelValue) * 0.1f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (WorldZoom > 2)
                    WorldZoom = 2;
                if (WorldZoom < 0.5f)
                    WorldZoom = 0.5f;

            }
            foreach (var itemPair in _entitiesById)
            {
                var item = itemPair.Value;

                if (item == _player)
                    continue;

                if(item.CurrentTarget == null)
                {
                    if(item.TargetActions?.Count > 0)
                    {
                        if(item.TargetActions.TryDequeue(out Entity.MovementToLerp result) 
                            && result != null)
                        {
                            item.CurrentTarget = result;
                        }                        
                    }
                }

                if(item.CurrentTarget != null)
                {
                    var target = item.CurrentTarget;
                    target.LerpTimeLeft -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (target.LerpTimeLeft < 0)
                    {
                        target.LerpTimeLeft = 0;
                        item.CurrentTarget = null;
                    }

                    var percent = target.LerpTimeLeft <= 0 ? 1.0f : target.LerpTimeLeft / Entity.LerpToTargerTime;

                    if (target.TargetPos != item.Position)
                    {
                        item.Position = Vector2.LerpPrecise(
                            item.Position, target.TargetPos, percent);
                    }
                    if (target.TargetRotation != item.Rotation)
                    {
                        item.Rotation = 
                            MathHelper.LerpPrecise(item.Rotation, target.TargetRotation, percent);                        
                    }
                }
            }
            // TODO: Lock
            foreach (var itemPair in _floatingTextList)
            {
                var item = itemPair.Value;
                item.TotalTimeToRemove -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (item.TotalTimeToRemove <= 0f)
                {
                    _floatingTextList.TryRemove(itemPair.Key, out item);
                }
                else
                {
                    switch (item.AnimationType)
                    {
                        case FloatingTextAnimationType.MoveUp:
                            item.Color.A = (byte)(255.0f * (item.TotalTimeToRemove / item.StartingTime));
                            item.Position -= new Vector2(0,
                                25 * (float)gameTime.ElapsedGameTime.TotalSeconds);
                            break;
                        case FloatingTextAnimationType.MoveToRightAndShrink:
                            item.Position += new Vector2(25.0f * (float)gameTime.ElapsedGameTime.TotalSeconds,
                                10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);

                            item.Scale -= 1f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                            break;
                        case FloatingTextAnimationType.MoveUpAndShrink:
                            item.Color.A = (byte)(255.0f * (item.TotalTimeToRemove / item.StartingTime));
                            item.Position -= new Vector2(0,
                                25 * (float)gameTime.ElapsedGameTime.TotalSeconds);

                            item.Scale -= 1f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                            break;
                        default:
                            break;
                    }
                }
            }

            var values = _projectTilesById.Values;

            foreach (var item in values)
            {
                if (item.ProjectileTypes == ProjectileTypes.None)
                {
                    _projectTilesById.TryRemove(item.Guid, out ProjectTile outItem);
                }
                else
                {
                    item.TotalTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                    if (item.TotalTime > item.ExpireMs)
                    {
                        _projectTilesById.Remove(item.Guid, out ProjectTile outItem);
                    }
                    else
                    {
                        if (!_entitiesById.ContainsKey(item.ToId) || !_entitiesById.ContainsKey(item.FromId))
                        {
                            _projectTilesById.Remove(item.Guid, out ProjectTile outItem);
                        }
                        else
                        {
                            var fromEntity = _entitiesById[item.FromId];
                            var toEntity = _entitiesById[item.ToId];

                            var time = item.TotalTime == 0.0f ? 0.0f : item.TotalTime / item.ExpireMs;
                            item.CurrentPos = Vector2.LerpPrecise(fromEntity.Position, toEntity.Position, time);

                            if (item.ProjectileTypes == ProjectileTypes.Arrow)
                            {
                                Vector2 dPos = toEntity.Position - item.CurrentPos;
                                item.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                            }else if (item.ProjectileTypes == ProjectileTypes.EnergyBall)
                            {
                                //Vector2 dPos = item.CurrentPos - toEntity.Position;
                                item.Rotation += 10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                                item.BonusScale += (float)Math.Sin(gameTime.ElapsedGameTime.TotalSeconds);
                            }
                        }
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch _spriteBatch)
        {
            Client.GraphicsDevice.Clear(Color.DarkGreen);
            var transform = get_transformation(Client.GraphicsDevice);

            if (_player != null)
            {

                _spriteBatch.Begin(SpriteSortMode.Immediate,
                    BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp,
                    null, null, null, transform);

                var listToDraw = _backgroundDisplayTile.GetTilesForPos(new Point((int)_player.Position.X, (int)_player.Position.Y)); // _backgroundDisplayTile.GetTilesForPlayer(_player)

                if (listToDraw != null)
                {
                    foreach (var item in listToDraw)
                    {
                        _spriteBatch.Draw(item.Value, 
                            new Rectangle(item.Key.X, item.Key.Y, BackgroundDisplayTile.ChunkSize, BackgroundDisplayTile.ChunkSize), 
                            null, Color.White);
                    }
                }

                _spriteBatch.End();
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp,
                null, null, null, transform);


            // _spriteBatch.Draw(person01, playerPos, Color.White);


            foreach (var itemPair in _entitiesById)
            {
                var item = itemPair.Value;
                var rotation = item.Rotation - _nintyRadius;
                if (item.IsAlive)
                {
                    _spriteBatch.Draw(_person01,
                        item.Position - new Vector2(15, 15),
                        null,
                        new Color(Color.Black, 0.2f),
                        rotation,
                        Entity.Origin,
                        1.0f,
                        SpriteEffects.None,
                        1.0f);
                    _spriteBatch.Draw(_person01,
                             item.Position,
                             null,
                             item.Color,
                             rotation,
                             Entity.Origin,
                             1.0f,
                             SpriteEffects.None,
                             0.9f);

                }
                else
                {
                    _spriteBatch.Draw(_personGhost01,
                             item.Position,
                             null,
                             new Color(new Color(177, 221, 241), 0.3f),
                             rotation,
                             Entity.Origin,
                             1.0f,
                             SpriteEffects.None,
                             0.9f);
                }

                if (item == _targetEntity)
                {
                    _spriteBatch.Draw(_targetCircle,
                            item.Position,
                            null,
                            Color.Red, // TODO Intent is included / quest / follow friend
                            rotation,
                            Entity.Origin,
                            1.0f,
                            SpriteEffects.None,
                            0.9f);
                    // _targetCircle
                }

                // TO DO ON NAME CHANGE?
                var titleName = item.IsAlive ? item.Name : $"[GHOST]{item.Name}";

                titleName = $"{item.GetMaxLevel()}) {titleName}";

                Vector2 size = Client.Font.MeasureString(titleName);
                Vector2 origin = size * 0.5f;

                _spriteBatch.DrawString(Client.Font, titleName, item.Position -
                    new Vector2(origin.X, (120)),
                    Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);

                var heathPos = item.Position - new Vector2(50, 80);

                _spriteBatch.Draw(_healthBarBase,
                    heathPos, null, Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.1f);

                _spriteBatch.Draw(_healthBarBase,
                    heathPos,
                    new Rectangle(0, 0, (int)(100.0f * (item.Health / item.MaxHealth)), 10),
                    item == _player && _player != null ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);

                //_spriteBatch.Draw(healthBarBase,
                //    item.Position, null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0.0f);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y, 100, 10),
                //    null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y,
                //    (int)(100.0f * (item.Health / item.MaxHealth)), 10),
                //    null, item == Player ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, SpriteEffects.None, 0);
            }

            var values = _projectTilesById.Values;
            foreach (var item in values)
            {
                if (item.ProjectileTypes == ProjectileTypes.None)
                    continue;
                var textureProjectTile = projectileTypesTextures[item.ProjectileTypes];

                if(item.ProjectileTypes == ProjectileTypes.Arrow)
                {
                    _spriteBatch.Draw(textureProjectTile,
                             item.CurrentPos - new Vector2(15, 15),
                             null,
                             new Color(Color.Black, 0.2f),
                             item.Rotation,
                             new Vector2(16, 16),
                             (item.ProjectileTypes == ProjectileTypes.Arrow ? 2f : 1.0f) + item.BonusScale,
                             SpriteEffects.None,
                             0.9f);
                }

                _spriteBatch.Draw(textureProjectTile,
                             item.CurrentPos,
                             null,
                             item.Color,
                             item.Rotation,
                             new Vector2(16, 16),
                             (item.ProjectileTypes == ProjectileTypes.Arrow ? 2f : 1.0f) + item.BonusScale,
                             SpriteEffects.None,
                             0.9f);
            }


            Texture2D texture = null;
            EnviromentType enviromentType = EnviromentType.None;

            foreach (var itemPair in _worldEnviroment.EnviromentItems)
            {
                var item = itemPair.Value;

                if (item.ItemType != enviromentType)
                {
                    texture = enviromentTextures[item.ItemType];
                    enviromentType = item.ItemType;
                }


                _spriteBatch.Draw(texture,
                 item.Position - new Vector2(15, 15),
                 null,
                 new Color(Color.Black, 0.2f),
                 1.0f,
                 new Vector2(128, 128),
                 1.0f,
                 SpriteEffects.None,
                 0.2f);

                if(_taskTarget == item)
                {
                    _spriteBatch.Draw(texture,
                             item.Position,
                             null,
                             new Color(Color.White, 0.4f),
                             1.0f,
                             new Vector2(128, 128),
                             1.0f,
                             SpriteEffects.None,
                             0.1f);
                }
                else
                {
                    _spriteBatch.Draw(texture,
                             item.Position,
                             null,
                             Color.White,
                             1.0f,
                             new Vector2(128, 128),
                             1.0f,
                             SpriteEffects.None,
                             0.1f);
                }
                
            }

            var list = _floatingTextList;
            foreach (var itemPair in list)
            {
                var item = itemPair.Value;
                if (item.DrawColorBackGround)
                {
                    _spriteBatch.DrawString(Client.FontBold, item.Text, item.Position - new Vector2(1.0f, 1.0f),
                        item.ColorBackGround, 0.0f, Vector2.Zero, item.Scale + 0.1f, SpriteEffects.None, 0);
                }
                _spriteBatch.DrawString(Client.Font, item.Text, item.Position,
                    item.Color, 0.0f, Vector2.Zero, item.Scale, SpriteEffects.None, 0);
            }

            if (_targetPos != null)
            {
                if (_mousePressScale > 0.7f)
                {
                    _spriteBatch.Draw(_moveToX,
                         _targetPos.Value,
                         null,
                         Color.White,
                         0.0f,
                         new Vector2(16, 16),
                         _mousePressScale,
                         SpriteEffects.None,
                         0f);
                }

            }


            _spriteBatch.End();

            // Draw UI
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp);
            if(_player != null)
            {

                string modeDesc = "Mode: ";

                switch (_mode)
                {
                    case EditingMode.None:
                        modeDesc = "Mode: Game";
                        break;
                    case EditingMode.PlacingTerrain:
                        modeDesc = $"Mode: Placing: {_itemTpAddOnRightClick:G}";
                        break;
                    case EditingMode.DrawBackground:
                        modeDesc = "Mode: Drawing Background";
                        break;
                    default:
                        break;
                }

                _spriteBatch.DrawString(Client.FontSmall, $"Creator Mode: {modeDesc}\r\nX: {_player.Position.X:n2}, Y: {_player.Position.Y:n2}\r\nFPS: {MathF.Round((float)(1.0f / gameTime.ElapsedGameTime.TotalSeconds), 2)}", new Vector2(10, 10),
                    Color.White, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }

            base.Container?.Draw(_spriteBatch);            

            _spriteBatch.End();
        }
    }
}
