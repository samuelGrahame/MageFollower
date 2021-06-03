using MageFollower.PacketData;
using MageFollower.Particle;
using MageFollower.Projectiles;
using MageFollower.UI;
using MageFollower.Utilities;
using MageFollower.World;
using MageFollower.World.Element;
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
using static MageFollower.Program;

namespace MageFollower.Client
{
    public class Game2D : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;        
        private Texture2D _person01;
        private Texture2D _personGhost01;
        private Texture2D _moveToX;
        private Texture2D _healthBarBase;
        private Texture2D _targetCircle;
        private SpriteFont _font;

        private Dictionary<EnviromentType, Texture2D> enviromentTextures = new();
        private Dictionary<ProjectileTypes, Texture2D> projectileTypesTextures = new();

        private float _nintyRadius = 90.0f * (float)Math.PI / 180.0f; // (float)Math.PI; // x*pi/180
        private float _worldRotation = 0.0f;
        private float _worldZoom = 1f;
        private float _mousePressScale = 0.0f;
        private Entity _player;
        //private List<Entity> _entities;
        private ConcurrentDictionary<string, Entity> _entitiesById = new();
        private ConcurrentDictionary<Guid, ProjectTile> _projectTilesById = new();
        private World.Enviroment _worldEnviroment = new();
        private Vector2? _targetPos = null;
        private Entity _targetEntity = null;
        private Matrix _transform;

        private JsonSerializerSettings _config = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        private Socket _sender;
        private string _passCode;
        private string _playerId;
        private double _lastTimeSentToServer = 0;

        private Queue<string> _dataToSend = new Queue<string>();
        private List<FloatingDamageText> _floatingTextList = new List<FloatingDamageText>();

        private double _moveLerpTimer = (1000 / 10.0f);
        private double _sendToServerTimer = (1000 / 20.0f);
        private Vector2 _prevPos;
        private float _prevRotation;
        private EnviromentType _itemTpAddOnRightClick = EnviromentType.Tree01;
        private bool _isCreatorModeOn = false;
        private InputHandler _input = new InputHandler();

        public UIBase FocusedControl = null;

        private UITextBox _commandTextBoxUI;

        public SpriteFont DefaultFont => _font;
        private string _server;

        public Game2D(string server = "")
        {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferMultiSampling = true;

            IsFixedTimeStep = true;  //Force the game to update at fixed time intervals
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0f);  //Set the time interval to 1/30th of a second            
            _server = server;
        }

        

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //_graphics.IsFullScreen = true;
            //_graphics.PreferredBackBufferWidth = 1920;
            //_graphics.PreferredBackBufferHeight = 1080;

            //_graphics.ApplyChanges();

            //_entities = new List<Entity>();
            
            StartClient();
            _commandTextBoxUI = new UITextBox(this)
            {
                Color = Color.White,
                CursorPos = 0,
                Position = new Vector2(25, 25)
            };
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
            try
            {
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
            }
            catch (Exception)
            {

            }

            Environment.Exit(-1);
        }
        protected override void LoadContent()
        {
            IsMouseVisible = true;
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            enviromentTextures.Add(
                EnviromentType.None,
                null); // allows for get ranged.

            enviromentTextures.Add(
                EnviromentType.Tree01, 
                Content.Load<Texture2D>("Trees/Tree01"));

            projectileTypesTextures.Add(ProjectileTypes.EnergyBall,
                Content.Load<Texture2D>("Projectiles/EnergyBall01"));

            projectileTypesTextures.Add(ProjectileTypes.Arrow,
                Content.Load<Texture2D>("Projectiles/Arrow01"));

            _person01 = Content.Load<Texture2D>("People/Person01_Idle");
            _personGhost01 = Content.Load<Texture2D>("People/Person01_Ghost_Idle");
            _moveToX = Content.Load<Texture2D>("Other/MoveToX");
            _targetCircle = Content.Load<Texture2D>("Other/TargetCircle");

            _font = Content.Load<SpriteFont>("Fonts/SegoeUI");

            _healthBarBase = Content.Load<Texture2D>("Other/HealthBarBase");
            // TODO: use this.Content to load your game content here
        }


        public Matrix get_transformation(GraphicsDevice graphicsDevice)
        {
            var playerPos = _player == null ? Vector2.Zero : _player.Position;

            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-playerPos.X, -playerPos.Y, 0)) *
                                         Matrix.CreateRotationZ(_worldRotation) *
                                         Matrix.CreateScale(new Vector3(_worldZoom, _worldZoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(graphicsDevice.Viewport.Width * 0.5f,
                                         graphicsDevice.Viewport.Height * 0.5f, 0));
            return _transform;
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

                if(string.IsNullOrWhiteSpace(_server))
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
                        while (true)
                        {
                            if (_dataToSend.Count > 0)
                            {
                                var dataPack = _dataToSend.Dequeue();
                                byte[] msg = Encoding.ASCII.GetBytes(dataPack);
                                _sender.Send(msg);
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
                            while (true)
                            {
                                string data = leftOver;
                                byte[] bytes = null;

                                while (true)
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


                                        if (_entitiesById.ContainsKey(_playerId))
                                        {
                                            _player = _entitiesById[_playerId];
                                        }
                                    }
                                    else if (item.StartsWith("NEW:"))
                                    {
                                        var newPlayer = item.Substring("NEW:".Length, item.Length - "NEW:".Length);
                                        var newEntity = JsonConvert.DeserializeObject<Entity>(newPlayer, _config);

                                        if (string.CompareOrdinal(newEntity.Id, _playerId) == 0)
                                        {
                                            _player = newEntity;
                                        }

                                        if (!_entitiesById.ContainsKey(newEntity.Id))
                                        {
                                            _entitiesById[newEntity.Id] = newEntity;                                            
                                        }
                                    }else if(item.StartsWith("NEW-P:"))
                                    {
                                        var newPlayer = item.Substring("NEW-P:".Length, item.Length - "NEW-P:".Length);
                                        var projectTile = JsonConvert.DeserializeObject<ProjectTile>(newPlayer, _config);

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
                                            var transform = JsonConvert.DeserializeObject<Transform>(IdAndTransform.Substring(placeOfSemi + 1), _config);
                                            if (_entitiesById.ContainsKey(id))
                                            {
                                                var entity = _entitiesById[id];
                                                entity.TargetPos = transform.Position;
                                                entity.TargetRotation = transform.Rotation;
                                                entity.TotalTimeLerp = 0;
                                                entity.LerpToTarger = true;
                                            }
                                        }
                                    }
                                    else if (item.StartsWith("DMG:"))
                                    {
                                        var IdAndTransform = item.Substring("DMG:".Length, item.Length - "DMG:".Length);
                                        var placeOfSemi = IdAndTransform.IndexOf(":");
                                        var id = IdAndTransform.Substring(0, placeOfSemi);

                                        var transform = JsonConvert.DeserializeObject<DamageToTarget>(IdAndTransform.Substring(placeOfSemi + 1), _config);
                                        if (_entitiesById.ContainsKey(id))
                                        {
                                            var entity = _entitiesById[id];
                                            entity.Health = transform.HealthToSet;

                                            var dmgDone = Math.Round(transform.DamageDone, 2).ToString();
                                            var size = _font.MeasureString(dmgDone) * 0.5f;

                                            _floatingTextList.Add(new FloatingDamageText()
                                            {
                                                Text = dmgDone,
                                                Position = entity.Position - new Vector2(size.X, 150.0f),
                                                Color = Color.White,
                                                TotalTimeToRemove = 2000.0f,
                                                StartingTime = 2000.0f
                                            });
                                        }
                                    }else if (item.StartsWith("ADDXP:"))
                                    {
                                        var IdAndTransform = item.Substring("ADDXP:".Length, item.Length - "ADDXP:".Length);
                                        var placeOfSemi = IdAndTransform.IndexOf(":");
                                        var id = IdAndTransform.Substring(0, placeOfSemi);

                                        var xpToTarget = JsonConvert.DeserializeObject<XpToTarget>(IdAndTransform.Substring(placeOfSemi + 1), _config);
                                        if (_entitiesById.ContainsKey(id))
                                        {
                                            var entity = _entitiesById[id];

                                            if(entity.AddXpToSkill(xpToTarget))
                                            {
                                                var caption = $"{xpToTarget.Level}: {xpToTarget.Xp} xp";
                                                var size = _font.MeasureString(caption) * 0.5f;

                                                _floatingTextList.Add(new FloatingDamageText()
                                                {
                                                    Text = caption,
                                                    Position = entity.Position - new Vector2(size.X, 150.0f),
                                                    Color = Color.CornflowerBlue,
                                                    TotalTimeToRemove = 2000.0f,
                                                    StartingTime = 2000.0f
                                                });


                                                size = _font.MeasureString("Leveled Up!") * 0.5f;
                                                _floatingTextList.Add(new FloatingDamageText()
                                                {
                                                    Text = "Leveled Up!",
                                                    Color = Color.Blue,
                                                    TotalTimeToRemove = 2000.0f,
                                                    StartingTime = 2000.0f,
                                                    Position = entity.Position - new Vector2(size.X, 170.0f)
                                                }) ;
                                            }                                            
                                        }
                                    }
                                    else if (item.StartsWith("SPAWN:"))
                                    {
                                        var itemToSpawn = item.Substring("SPAWN:".Length, item.Length - "SPAWN:".Length);
                                        try
                                        {
                                            var enviromentItem = JsonConvert.DeserializeObject<EnviromentItem>(itemToSpawn, _config);//ListOfTrees
                                            _worldEnviroment.EnviromentItems.Add(enviromentItem);
                                        }
                                        catch (Exception)
                                        {

                                        }
                                        //dataToSend.Append($"SPAWN:TREE:ME<EOF>");
                                    }
                                    else if (item.StartsWith("DEL:"))
                                    {
                                        var idToRemove = item.Substring("DEL:".Length, item.Length - "DEL:".Length);
                                        if (string.CompareOrdinal(idToRemove, _playerId) == 0)
                                        {
                                            _player = null;
                                        }

                                        if (_entitiesById.ContainsKey(idToRemove))
                                        {                                                                                        
                                            _entitiesById.Remove(idToRemove, out _);
                                        }
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


        private void SetTargetOnServer(Entity entity)
        {
            if (entity == _player)
                entity = null;

            _targetEntity = entity;
            _mousePressScale = 0.0f;
            // TODO: Add Intent / or maybe work out - if foe etc
            if(entity == null)
            {
                _dataToSend.Enqueue($"TARGET:NULL<EOF>");
            }
            else
            {
                _dataToSend.Enqueue($"TARGET:{entity.Id}<EOF>");
            }
            
        }
        private void SpawnItemAtPos(EnviromentType type, Vector2 pos)
        {
            if(type == EnviromentType.Tree01)
            {
                pos -= new Vector2(-35, 150);
            }
            _dataToSend.Enqueue($"SPAWN:{JsonConvert.SerializeObject(new EnviromentItem() { Position = pos, ItemType = type }, _config)}<EOF>");            
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
            _input.MouseState = Mouse.GetState();
            _input.KeyboardState = Keyboard.GetState();

            // test
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

                if(FocusedControl != null)
                {
                    // do we override...
                    FocusedControl.Update(_input); // draw is taken care of with control structure. // list of children / compontents on game window. if is visible draw / update.
                }

                if(_input.IsKeyPressed(Keys.Enter))
                {
                    if(FocusedControl == null)
                    {
                        FocusedControl = _commandTextBoxUI;
                    }
                    else
                    {
                        if(FocusedControl == _commandTextBoxUI)
                        {
                            _commandTextBoxUI.Text = "";
                        }
                        // allows override?
                        FocusedControl = null;
                    }
                }

                if (_input.MouseState.RightButton == ButtonState.Pressed && _input.PrevMouseState.RightButton == ButtonState.Released)
                {
                    var worldMousePos = GetMouseWorldPos(_input.MouseState);
                    // TODO CHECK UI First.

                    if (_isCreatorModeOn)
                    {
                        SpawnItemAtPos(_itemTpAddOnRightClick, worldMousePos); //Player.AttackTarget(Player, 10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                    else
                    {
                        // TODO Have Physics engine do a ray cast. just do hard working loop.                        
                        var entities = _entitiesById.Values;
                        var clickedOn = entities.Where(o =>
                            o != _player && VectorHelper.AreInRange(128.0f, o.Position, worldMousePos))?.ToList();

                        if(clickedOn != null && clickedOn.Count > 0)
                        {
                            if (clickedOn.Count == 0)
                            {
                                SetTargetOnServer(clickedOn[0]);
                            }
                            else
                            {
                                SetTargetOnServer(
                                    clickedOn.OrderBy(o =>
                                        Vector2.Distance(o.Position, worldMousePos)).
                                    FirstOrDefault());
                            }
                            
                        }
                    }
                }

                if(_input.PrevKeyboardState.IsKeyUp(Keys.G) && _input.KeyboardState.IsKeyDown(Keys.G))
                {
                    _isCreatorModeOn = !_isCreatorModeOn;
                }
                        
                if (_input.MouseState.LeftButton == ButtonState.Pressed)
                {                    
                    _targetPos = GetMouseWorldPos(_input.MouseState);
                    if(_targetEntity != null)
                    {
                        SetTargetOnServer(null);
                    }

                    _mousePressScale = 1.0f;
                }

                if(_targetEntity != null)
                {
                    if(!_targetEntity.IsAlive)
                    {
                        _targetPos = null;
                        SetTargetOnServer(null);
                    }
                    else
                    {
                        _targetPos = _targetEntity.Position;
                        // are we in range of attack? do we do it from server.
                        if(VectorHelper.AreInRange(_player.GetAttackRange(), _player.Position, _targetEntity.Position))
                        {
                            _targetPos = null;
                        }
                    }
                    // DO NOT WALK IF IN RANGE.
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
                    if(_targetEntity == null)
                    {                        
                        Vector2 dPos = _player.Position - GetMouseWorldPos(_input.MouseState);

                        _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                    }
                    else
                    {
                        Vector2 dPos = _player.Position - _targetEntity.Position;
                        _player.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                    }
                    
                }

                _lastTimeSentToServer += gameTime.ElapsedGameTime.TotalMilliseconds;

                if (_lastTimeSentToServer > _sendToServerTimer && 
                    (_prevPos != _player.Position || _prevRotation != _player.Rotation))
                {
                    _lastTimeSentToServer = 0;
                    _dataToSend.Enqueue($"POS:{JsonConvert.SerializeObject(new Transform() { Position = _player.Position, Rotation = _player.Rotation }, _config)}<EOF>");

                    _prevPos = _player.Position;
                    _prevRotation = _player.Rotation;
                }                
                //PacketsToSend
            }

            if (_input.PrevMouseState.ScrollWheelValue != _input.MouseState.ScrollWheelValue)
            {
                _worldZoom += (_input.MouseState.ScrollWheelValue - _input.PrevMouseState.ScrollWheelValue) * 0.1f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_worldZoom > 2)
                    _worldZoom = 2;
                if (_worldZoom < 0.5f)
                    _worldZoom = 0.5f;

            }            
            foreach (var itemPair in _entitiesById)
            {
                var item = itemPair.Value;
                if(item != _player && item.LerpToTarger)
                {
                    item.TotalTimeLerp += (float)gameTime.ElapsedGameTime.TotalMilliseconds;                    
                    var percent = (float)(item.TotalTimeLerp / _moveLerpTimer);
                    if (item.TotalTimeLerp >= _moveLerpTimer)
                    {
                        item.TotalTimeLerp = _moveLerpTimer;
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

            for (int i = _floatingTextList.Count - 1; i >= 0; i--)
            {
                var item = _floatingTextList[i];
                item.TotalTimeToRemove -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (item.TotalTimeToRemove <= 0f)
                {
                    _floatingTextList.RemoveAt(i);
                }
                else
                {
                    item.Color.A = (byte)(255.0f * (item.TotalTimeToRemove / item.StartingTime));
                    item.Position -= new Vector2(0, 
                        25 * (float)gameTime.ElapsedGameTime.TotalSeconds);
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

                            Vector2 dPos = item.CurrentPos - toEntity.Position;
                            item.Rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                        }
                    }
                }
            }


            _input.PrevMouseState = _input.MouseState;
            _input.PrevKeyboardState = _input.KeyboardState;

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGreen);

            //if(BloomGhost == null)
            //{
            //    BloomGhost = _bloomFilter.Draw(_personGhost01,
            //        256,
            //        256);

            //    GraphicsDevice.SetRenderTarget(null);
            //}
            //SpriteSortMode.Deferred, BlendState.Additive

            // TODO: Add your drawing code here
            _spriteBatch.Begin(SpriteSortMode.Deferred, 
                BlendState.AlphaBlend, 
                SamplerState.AnisotropicClamp, 
                null, null, null, get_transformation(GraphicsDevice));

            // _spriteBatch.Draw(person01, playerPos, Color.White);
            foreach (var itemPair in _entitiesById)
            {
                var item = itemPair.Value;
                var rotation = item.Rotation - _nintyRadius;
                if(item.IsAlive)
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

                if(item == _targetEntity)
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

                titleName = $"{item.Melee.Level}) {titleName}";

                Vector2 size = _font.MeasureString(titleName);                
                Vector2 origin = size * 0.5f;

                _spriteBatch.DrawString(_font, titleName, item.Position -
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
                _spriteBatch.Draw(textureProjectTile,
                             item.CurrentPos,
                             null,
                             item.Color,
                             item.Rotation,
                             new Vector2(16, 16),
                             item.ProjectileTypes == ProjectileTypes.Arrow ? 1.5f : 1.0f,
                             SpriteEffects.None,
                             0.9f);
            }


            Texture2D texture = null;
            EnviromentType enviromentType = EnviromentType.None;

            foreach (var item in _worldEnviroment.EnviromentItems)
            {
                if(item.ItemType != enviromentType)
                {
                    texture = enviromentTextures[item.ItemType];
                    enviromentType = item.ItemType;
                }
                

                _spriteBatch.Draw(texture,
                 item.Position - new Vector2(15, 15),
                 null,
                 new Color(Color.Black, 0.2f),
                 1.0f,
                 Vector2.Zero,
                 1.0f,
                 SpriteEffects.None,
                 0.2f);

                _spriteBatch.Draw(texture,
                             item.Position,
                             null,
                             Color.White,
                             1.0f,
                             Vector2.Zero,
                             1.0f,
                             SpriteEffects.None,
                             0.1f);
            }


            foreach (var item in _floatingTextList)
            {
                _spriteBatch.DrawString(_font, item.Text, item.Position,
                    item.Color, 0.0f, Vector2.Zero, 1.6f, SpriteEffects.None, 0);
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
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.DrawString(_font, $"Creator Mode: {(_isCreatorModeOn ? "True" : "False")}", new Vector2(10, 10),
                    Color.White, 0.0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

            _commandTextBoxUI?.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
