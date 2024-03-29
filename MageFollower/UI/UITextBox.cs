﻿using MageFollower.Client;
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
        public Color Color;
        public SpriteFont Font;

        public float FontScale = 1.0f;

        public string Text
        {
            get { return _text; }
            set {

                _text = value;
                _innerList.Clear();
                if(_text != null)
                {
                    _innerList.AddRange(_text);                    
                }
            }
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
            if(!string.IsNullOrWhiteSpace(Text))
            {
                spriteBatch.DrawString(Font ?? GameClient.Font, Text, GetGlobalLocation(),
                    Color, 0.0f, Vector2.Zero, FontScale, SpriteEffects.None, 0);
            }
                
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
                    if(insertOrAddChar(item, inputHandler.KeyboardState.CapsLock, 
                        inputHandler.KeyboardState.IsKeyDown(Keys.LeftShift) || inputHandler.KeyboardState.IsKeyDown(Keys.RightShift)))
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
        public static Keys[] OtherKeys = new Keys[] {
            Keys.Space,
            Keys.OemSemicolon,
            Keys.OemQuotes,
            Keys.OemComma,
            Keys.OemPeriod,
            Keys.OemMinus,
            Keys.OemPlus
        };
        private bool insertOrAddChar(Keys key, bool capsLock, bool shift)
        {
            // TODO make a helper Func keys to valid char.
            if ((key >= Keys.A && key <= Keys.Z) || (key >= Keys.NumPad0 && key <= Keys.NumPad9) || (key >= Keys.D0 && key <= Keys.D9) || OtherKeys.Any(o => key == o))
            {                
                char charToAdd;

                if(key >= Keys.NumPad0 && key <= Keys.NumPad9)
                {
                    charToAdd = (char)(key - 48);
                }else if (key >= Keys.D0 && key <= Keys.D9)
                {
                    charToAdd = (char)(key);
                }
                else if(key == Keys.OemSemicolon)
                {
                    charToAdd = shift ? ':' : ';';
                }
                else if (key == Keys.OemQuotes)
                {
                    charToAdd = shift ? '\"' : '\'';
                }
                else if (key == Keys.OemComma)
                {
                    charToAdd = shift ? '<' : ',';
                }
                else if (key == Keys.OemPeriod)
                {
                    charToAdd = shift ? '>' : '.';
                }
                else if (key == Keys.OemMinus)
                {
                    charToAdd = shift ? '_' : '-';
                }
                else if (key == Keys.OemPlus)
                {
                    charToAdd = shift ? '=' : '+';
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
            }else if(key == Keys.Back)
            {
                if (CursorPos > 0)
                {
                    _innerList.RemoveAt(CursorPos - 1);
                    CursorPos--;
                    return true;
                }
            }else if(key == Keys.Delete)
            {
                if (CursorPos < _innerList.Count)
                {
                    _innerList.RemoveAt(CursorPos);
                    return true;
                }
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
