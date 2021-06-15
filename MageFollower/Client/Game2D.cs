using MageFollower.Creator;
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

        public SpriteFont Font;
        public SpriteFont FontBold;
        public SpriteFont FontSmall;

        public SpriteFont DefaultFont => Font;
        private string _server;

        public InputHandler Input;

        private GameState _activeGameState = null;

        public GameState ActiveGameState => _activeGameState;

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
            Input = new InputHandler();
            _activeGameState = new WorldGameState(this);

            base.Initialize();
        }

        public void ChangeGameState(GameState gameState)
        {
            if(gameState == _activeGameState)
            {
                return;
            }
            var prev = _activeGameState;
            if(prev != null)
            {
                prev.Unload();
            }
            
            if(gameState != null)
            {
                gameState.Load();
            }
            _activeGameState = gameState;            
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            _activeGameState?.Unload();

            Environment.Exit(-1);
        }
        protected override void LoadContent()
        {
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1010;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            IsMouseVisible = true;
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<SpriteFont>("Fonts/Arial");
            FontBold = Content.Load<SpriteFont>("Fonts/ArialBold");
            FontSmall = Content.Load<SpriteFont>("Fonts/ArialSmall");

            _activeGameState?.Load();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                UnloadContent();

            Input.MouseState = Mouse.GetState();
            Input.KeyboardState = Keyboard.GetState();

            _activeGameState?.Update(gameTime);

            Input.PrevMouseState = Input.MouseState;
            Input.PrevKeyboardState = Input.KeyboardState;

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            _activeGameState?.Draw(gameTime, _spriteBatch);

            base.Draw(gameTime);
        }
    }
}
