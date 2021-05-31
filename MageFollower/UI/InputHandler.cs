using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.UI
{
    public class InputHandler
    {
        public MouseState MouseState;
        public MouseState PrevMouseState;

        public KeyboardState KeyboardState;
        public KeyboardState PrevKeyboardState;

        /// <summary>
        /// Check when key is down
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsKeyPressed(Keys key)
        {
            return KeyboardState.IsKeyDown(key) && PrevKeyboardState.IsKeyUp(key);
        }

        /// <summary>
        /// Check after key is pressed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool WasKeyPressed(Keys key)
        {
            return KeyboardState.IsKeyUp(key) && PrevKeyboardState.IsKeyDown(key);
        }
    }
}
