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
    public class UIColorPicker : UIBase
    {
        private Texture2D texture;
        public Action<UIColorPicker, InputHandler> Clicked = null;
        public Action<UIColorPicker, InputHandler> MouseDownMoved = null;
        public UIColorPicker(Game2D gameClient) : base(gameClient)
        {
        }

        public Texture2D Texture => texture;

        public override bool DoesBlockMouseClick()
        {
            return true;
        }
        public override Vector2 GetSize()
        {
            return new Vector2(240, 220);
        }
        public override void OnClick(InputHandler inputHandler)
        {
            Clicked?.Invoke(this, inputHandler);
        }
        public override void OnMouseDownMove(InputHandler inputHandler)
        {
            Clicked?.Invoke(this, inputHandler);
        }
        bool _created = false;
        public override void Draw(SpriteBatch spriteBatch)
        {
            if(!_created)
            {
                var pixelcolor = Color.Red;
                HSLColor hslColor = HSLColor.FromRgb(pixelcolor.R, pixelcolor.G, pixelcolor.B);

                texture = new Texture2D(GameClient.GraphicsDevice, 240, 220);

                var colors = Creator.Texture2DHelper.GetPixels(texture);

                for (int y = 0; y < texture.Height; y++)
                {
                    hslColor.H = 0;
                    for (int x = 0; x < texture.Width; x++)
                    {                        
                        Creator.Texture2DHelper.SetPixel(ref colors, new Color(hslColor.ToRgbColor(),1.0f), (int)x, (int)y, texture.Width);
                        hslColor.H += (1.0f / 255.0f);
                    }
                    hslColor.S -= ((y / 255.0f) * 0.01f);
                }

                texture.SetData(colors);

                _created = true;
            }

            var location = GetGlobalLocation();

            spriteBatch.Draw(texture,
                            new Rectangle((int)location.X, (int)location.Y, texture.Width, texture.Height),
                            null, Color.White);

            base.Draw(spriteBatch);
        }
    }
}
