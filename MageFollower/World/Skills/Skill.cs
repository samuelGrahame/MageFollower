using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World.Skills
{
    public class Skill
    {
        public static double XpPerLevel = 1000.0f;

        [JsonProperty("i0")]
        public SkillType Type { get; set; }
        [JsonProperty("i1")]
        public int Level { get; set; }
        [JsonProperty("i2")]
        public double Xp { get; set; }
        [JsonProperty("i3")]
        public double XpToLevel { get; set; }
        [JsonProperty("i4")]
        public double Pending { get; set; }

        public Skill()
        {
            Default();
        }

        private void Default()
        {
            Level = 1;
            XpToLevel = XpPerLevel;
        }

        public Skill(SkillType type, double xp)
        {
            Default();
            Type = type;
            AddXp(xp);
        }

        public void AddXp(double xp)
        {
            Xp += xp;
            while(Xp > XpToLevel)
            {
                Level++;
                XpToLevel += XpPerLevel;
            }
        }
    }
}
