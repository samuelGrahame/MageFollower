using MageFollower.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.UI
{
    public class UIContainer : UIBase
    {
        public UIContainer(Game2D gameClient) : base(gameClient)
        {

        }

        public List<UIBase> Children = new List<UIBase>();
        public UIBase GetControlFromScreenPos(Point point)
        {
            foreach (var item in Children)
            {
                if (item.DoesBlockMouseClick())
                {
                    var size = item.GetSize();
                    if (size != Vector2.Zero)
                    {
                        var globalPos = item.GetGlobalLocation();
                        if (new Rectangle((int)globalPos.X, (int)globalPos.Y, (int)size.X, (int)size.Y).Contains(point))
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        public override void Update(InputHandler inputHandler)
        {
            if (Children == null || Hidden)
                return;

            var focusedControlStart = GameClient.ActiveGameState.FocusedControl;

            foreach (var item in Children)
            {
                if (item is UIContainer container)
                    container.Update(inputHandler);
            }

            if(focusedControlStart == GameClient.ActiveGameState.FocusedControl && inputHandler.IsLeftMousePressed())
            {
                var item = GetControlFromScreenPos(inputHandler.MouseState.Position);
                GameClient.ActiveGameState.SetFocusedControl(item);

                item?.OnClick(inputHandler);
            }
            // if Mouse Contains:
            //SetFocusedControl
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Children == null || Hidden)
                return;

            foreach (var item in Children)
            {
                item.Parent = this;
                if (item.Hidden)
                    continue;
                item.Draw(spriteBatch);
            }
        }

    }
}
