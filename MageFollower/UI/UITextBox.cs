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
    public class UITextBox : UIBase
    {
        private List<char> _innerList = new();
        public int CursorPos;
        private string _text;
        public Vector2 Posision;
        public Color Color;

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }
        private void _buildTextFromInnerList()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < _innerList.Count; i++)
            {
                builder.Append(_innerList[i]);
            }
            _text = builder.ToString();
        }

        public UITextBox(Game2D gameClient) : base(gameClient)
        {

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(GameClient.DefaultFont, Text, Posision, Color);
        }

        public override void Update(InputHandler inputHandler)
        {

            // move cursor..

            checkCursorPos();

            var keys = inputHandler.KeyboardState.GetPressedKeys();
            bool didChange = false;
            foreach (var item in keys)
            {
                if(inputHandler.PrevKeyboardState.IsKeyUp(item))
                {
                    if(insertOrAddChar(item, inputHandler.KeyboardState.CapsLock))
                    {
                        didChange = true;
                    }
                }
            }

            if(didChange)
            {
                _buildTextFromInnerList();
            }

            base.Update(inputHandler);
        }

        private bool insertOrAddChar(Keys key, bool capsLock)
        {
            // TODO make a helper Func keys to valid char.
            if ((key >= Keys.A && key <= Keys.Z) || (key >= Keys.NumPad0 && key <= Keys.NumPad9) || key == Keys.Space)
            {                
                char charToAdd;

                if(key >= Keys.NumPad0 && key <= Keys.NumPad9)
                {
                    charToAdd = (char)(key - 48);
                }
                else
                {
                    charToAdd = capsLock ? char.ToLower((char)key) : char.ToUpper((char)key);
                }

                if (CursorPos == _innerList.Count)
                {
                    _innerList.Add(charToAdd);
                }
                else
                {
                    _innerList.Insert(CursorPos, charToAdd);
                }

                CursorPos += 1;
                return true;
            }
            return false;
        }

        private void checkCursorPos()
        {
            if(CursorPos > _innerList.Count)
            {
                CursorPos = _innerList.Count;
            }
            if (CursorPos < 0)
            {
                CursorPos = 0;
            }
        }

    }
}
