using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World.Skills
{
    public enum EffectSkill
    {
        /// <summary>
        /// Allows the character to participate longer in activities without feeling tired.	The character trips while walking, fumbles in combat, gives up after failing professions (locking progression in that profession)
        /// </summary>
        Tiredness,
        /// <summary>
        /// Allows the character to go without feeling hungry.	The character’s tummy grumbles, experience is gained slower growing with extreme hunger.
        /// </summary>
        Appetite,
        /// <summary>
        /// Allows the character to participate longer in outdoor activities without feeling dehydrated or getting burnt.	The character’s skin changes appearance, can cause the character to tan, decrease health regeneration rate and profession quality.
        /// </summary>
        Sun_Exposure,
        /// <summary>
        /// Allows the character to participate longer in activities without feeling sick.	The character is feared by healthy people, the disease can progress and worsen if not dealt with. The character coughs/sneezes/vomits, getting too close to healthy people is an act of evil.
        /// </summary>
        Disease

    }
}
