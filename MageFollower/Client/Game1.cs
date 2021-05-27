using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MageFollower.Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D texture;
        private Texture2D person01;
        private Texture2D moveToX;

        private Vector2 playerPos;
        private Vector2 origin = new Vector2(128, 128);
        private float Speed = 100;
        private float rotation;
        private float NintyRadius = 90.0f * (float)Math.PI / 180.0f; // (float)Math.PI; // x*pi/180
        private float WorldRotation;
        private float WorldZoom = 1.0f;
        private float MouseScale = 0.0f;


        private Vector2? targetPos = null;

        private Matrix _transform;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferMultiSampling = true;

            IsFixedTimeStep = true;  //Force the game to update at fixed time intervals
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0f);  //Set the time interval to 1/30th of a second            
            
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;

            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            IsMouseVisible = true;
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            texture = Content.Load<Texture2D>("Trees/Tree01");
            person01 = Content.Load<Texture2D>("People/Person01_Idle");
            moveToX = Content.Load<Texture2D>("Other/MoveToX");
            // TODO: use this.Content to load your game content here
        }


        public Matrix get_transformation(GraphicsDevice graphicsDevice)
        {
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

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            var keyboard = Keyboard.GetState();
            Vector2 vectorToMove = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W))
                vectorToMove.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S))
                vectorToMove.Y += 1;
            if (keyboard.IsKeyDown(Keys.A))
                vectorToMove.X -= 1;
            if (keyboard.IsKeyDown(Keys.D))
                vectorToMove.X += 1;

            var mouseState = Mouse.GetState();

            
            if(Vector2.Zero != vectorToMove)
            {
                targetPos = null;

                playerPos += ((vectorToMove * Speed) *
                    (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if(mouseState.LeftButton == ButtonState.Pressed)
            {
                var ms = mouseState.Position;
                Matrix inverseTransform = Matrix.Invert(_transform);
                targetPos = Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);

                MouseScale = 1.0f;
            }

           

            if(targetPos != null)
            {
                if (MouseScale > 0.7f)
                {
                    MouseScale -= 2f *
                        (float)gameTime.ElapsedGameTime.TotalSeconds;
                }

                Vector2.Distance(targetPos.Value, playerPos);

                if(AreInRange(3.0f, playerPos, targetPos.Value))
                {
                    targetPos = null;
                }
                else
                {                    
                    Vector2 dir = targetPos.Value - playerPos;

                    Vector2 dPos = playerPos - targetPos.Value;

                    rotation = (float)Math.Atan2(dPos.Y, dPos.X);

                    dir.Normalize();
                    playerPos += dir * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;                    

                    rotation = (float)Math.Atan2(dPos.Y, dPos.X);
                }
            }

            if (targetPos == null)
            {
                var ms = mouseState.Position;
                Matrix inverseTransform = Matrix.Invert(_transform);
                
                Vector2 dPos = playerPos - Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);

                rotation = (float)Math.Atan2(dPos.Y, dPos.X);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            _spriteBatch.Begin(SpriteSortMode.BackToFront, 
                BlendState.AlphaBlend, 
                SamplerState.AnisotropicClamp, 
                null, null, null, get_transformation(GraphicsDevice));


            // _spriteBatch.Draw(person01, playerPos, Color.White);

            _spriteBatch.Draw(person01,
                     playerPos - new Vector2(15, 15),
                     null,
                     new Color(Color.Black, 0.2f),
                     rotation - NintyRadius,
                     origin,
                     1.0f,
                     SpriteEffects.None,
                     0f);

            _spriteBatch.Draw(person01,
                     playerPos,
                     null,
                     Color.White,
                     rotation - NintyRadius,
                     origin,
                     1.0f,
                     SpriteEffects.None,
                     0f);

            _spriteBatch.Draw(texture, Vector2.Zero - new Vector2(15, 15), new Color(Color.Black, 0.2f));
            _spriteBatch.Draw(texture, Vector2.Zero, Color.White); // new Color(Color.White, 0.7f));

            if(targetPos != null)
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
