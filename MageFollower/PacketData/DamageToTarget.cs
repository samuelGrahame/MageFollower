using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.PacketData
{
    public class DamageToTarget
    {
        [JsonProperty("h")]
        public double HealthToSet;
        [JsonProperty("d")]
        public double DamageDone;
    }
}
