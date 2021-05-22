using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World.Skills
{
    public class Skill
    {
        public static double XpPerLevel = 1000.0f;
        public SkillType Type { get; set; }
        public int Level { get; set; }
        public double Xp { get; set; }
        public double XpToLevel { get; set; }

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
