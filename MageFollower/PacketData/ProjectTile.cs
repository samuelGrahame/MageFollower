using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.PacketData
{
    public class ProjectTile
    {
        [JsonProperty("g")]
        public Guid Guid;
        [JsonProperty("e")]
        public float ExpireMs;
        [JsonProperty("p")]
        public Projectiles.ProjectileTypes ProjectileTypes;
        [JsonIgnore]
        public Action OnExpire = null;
        [JsonProperty("t")]
        public string ToId;
        [JsonProperty("s")]
        public string FromId;
        [JsonProperty("c")]
        public Color Color;

        [JsonIgnore]
        public float TotalTime;

        /// <summary>
        /// Client Side Pos
        /// </summary>
        [JsonIgnore]
        public Vector2 CurrentPos;
        [JsonIgnore]
        public float Rotation;
        [JsonIgnore]
        public float BonusScale = 0.0f;
    }
}
