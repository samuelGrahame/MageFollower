using MageFollower.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.Client
{
    public abstract class GameState
    {
        public Game2D Client;

        public GameState(Game2D client)
        {
            Client = client;
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch _spriteBatch)
        {

        }
        public ContentManager Content => Client.Content;
        public InputHandler Input => Client.Input;

        public virtual void Update(GameTime gameTime)
        {

        } 
                
        public virtual void Load()
        {

        }

        public virtual void Unload()
        {

        }
    }
}
