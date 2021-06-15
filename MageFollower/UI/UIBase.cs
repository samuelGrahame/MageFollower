using MageFollower.Client;
using Microsoft.Xna.Framework;
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
        public bool Hidden;
        public UIContainer Parent;
        public Vector2 Position;

        public virtual Vector2 GetSize()
        {
            return Vector2.Zero;
        }

        public Vector2 GetGlobalLocation()
        {
            return Position + Parent?.GetGlobalLocation() ?? Vector2.Zero;
        }

        public virtual bool DoesBlockMouseClick()
        {
            return false;
        }

        public UIBase(Game2D gameClient)
        {
            GameClient = gameClient;
        }

        public virtual void OnClick(InputHandler inputHandler)
        {

        }

        public virtual void OnMouseDownMove(InputHandler inputHandler)
        {

        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        
        }

        public virtual void Update(InputHandler inputHandler)
        {

        }

    }
}
