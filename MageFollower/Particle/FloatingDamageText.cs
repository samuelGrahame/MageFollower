using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.Particle
{
    public class FloatingDamageText
    {
        public string Text;
        public Color Color;
        public float TotalTimeToRemove;
        public float StartingTime;
        public Vector2 Position;
        public FloatingTextAnimationType AnimationType = FloatingTextAnimationType.MoveUp;
        public float Scale = 1.6f;
        public bool DrawColorBackGround = false;
        public Color ColorBackGround;
    }

    public enum FloatingTextAnimationType
    {
        MoveUp,
        MoveToRightAndShrink
    }
}
