using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.PacketData
{
    public class Transform
    {
        [JsonProperty("p")]
        public Vector2 Position;
        [JsonProperty("r")]
        public float Rotation;
    }
}
