using MageFollower.Client;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.UI
{
    public abstract class UIBase
    {
        public Game2D GameClient; // TODO Make Interface.

        public UIBase(Game2D gameClient)
        {
            GameClient = gameClient;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        
        }

        public virtual void Update(InputHandler inputHandler)
        {

        }

    }
}
