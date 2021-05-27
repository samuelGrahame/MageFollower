using MageFollower.World;
using MageFollower.World.Element;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

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
        private float WorldZoom = 1.0f;

        private float MouseScale = 0.0f;

        private Entity Player;
        private List<Entity> Entities;

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

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;

            _graphics.ApplyChanges();

            Entities = new List<Entity>();

            var fireMelee = new Entity()
            {
                ElementType = ElementType.Fire,
                Health = 100,
                MaxHealth = 100,
                Name = "The Fire Swordsman",
                Color = Color.LightSalmon
            };
            fireMelee.Melee.AddXp(10000.0f);
            fireMelee.RightHand = new World.Items.Item()
            {
                Equipt = World.Items.EquiptType.Physical,
                Power = 1.1f,
                Type = World.Items.ItemType.Stick
            };

            Entities.Add(fireMelee);

            Player = new Entity()
            {
                ElementType = ElementType.Water,
                Health = 100,
                MaxHealth = 100,
                Name = "The Water Swordsman",
                Color = Color.LightBlue
            };
            Player.Melee.AddXp(10000.0f);
            Player.RightHand = new World.Items.Item()
            {
                Equipt = World.Items.EquiptType.Physical,
                Power = 1.1f,
                Type = World.Items.ItemType.Stick
            };

            Entities.Add(Player);

            base.Initialize();
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
            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-Player.Position.X, -Player.Position.Y, 0)) *
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


            if (keyboard.IsKeyDown(Keys.Space))
                Player.AttackTarget(Player, 10.0f * (float)gameTime.ElapsedGameTime.TotalSeconds);

            var mouseState = Mouse.GetState();

            if(mouseState.LeftButton == ButtonState.Pressed)
            {
                var ms = mouseState.Position;
                Matrix inverseTransform = Matrix.Invert(_transform);
                targetPos = Vector2.Transform(new Vector2(ms.X, ms.Y), inverseTransform);

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

                Vector2.Distance(targetPos.Value, Player.Position);

                if(AreInRange(3.0f, Player.Position, targetPos.Value))
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
                        1.0f);

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
                    item == Player ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);

                //_spriteBatch.Draw(healthBarBase,
                //    item.Position, null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0.0f);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y, 100, 10),
                //    null, Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0);

                //_spriteBatch.Draw(whiteRectangle, new Rectangle((int)item.Position.X, (int)item.Position.Y,
                //    (int)(100.0f * (item.Health / item.MaxHealth)), 10),
                //    null, item == Player ? Color.Yellow : Color.Red, 0.0f, Vector2.Zero, SpriteEffects.None, 0);
            }

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
