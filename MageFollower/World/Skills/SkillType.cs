using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World.Skills
{
    public enum SkillType
    {
        // Profession
        /// <summary>
        /// Allows the character to create new items with collected materials used for crafting. (includes smelting, cloth making, woodwork, smithing) Blacksmith
        /// </summary>
        Crafting,
        /// <summary>
        /// Allows the character to chop trees from blocking paths and collecting wood.Lumberjack
        /// </summary>
        Wood_Cutting,
        /// <summary>
        /// Allows the character to clear overgrown grassy areas and collect fibre used for crafting.Farmer
        /// </summary>
        Fibre_Harvesting,
        /// <summary>
        /// Allows the character to clear rocks from caves and collect ore used for crafting.Miner
        /// </summary>
        Mining,
        /// <summary>
        /// Allows the character to collect meat and hide for their village/town/city.Hunter
        /// </summary>
        Hunting,
        /// <summary>
        /// Allows the character to collect berries/mushrooms and sticks for their village/town/city.Gatherer
        /// </summary>
        Gathering,
        /// <summary>
        /// Allows the character to carry given resources from their village/town/city to another village/town/city and back to their village/town/city.Also makes maps between them, increases trade and different resources.Map navigator
        /// </summary>
        Transporting,
        /// <summary>
        /// Allows the character to read words/pages/books/maps which can be used to help the player and others.Librarian, Teacher (requires library/school)
        /// </summary>
        Reading,
        /// <summary>
        /// Allows the character to sound more intellectual, logical and influence on others.Politician, Pope (requires kingdom/church)
        /// </summary>
        Speech,
        /// <summary>
        /// Allows the character to impress others with their skill of sword swinging, magic displaying or trophies.This increases influence on others.	Entertainer, Knight (requires skill/blessing)
        /// </summary>
        Entertainment,        
        //Combat 
        /// <summary>
        /// Allows the character to effectively use spells in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        Magic,
        /// <summary>
        /// Allows the character to effectively use melee in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        Melee,
        /// <summary>
        /// Allows the character to effectively use ranged weapons in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        Ranged,
        /// <summary>
        /// Allows the character to critical strike unsuspected enemies, steal items and hide.Thief/Detective
        /// </summary>
        Sneak,
        /// <summary>
        /// stance Allows the character to take more damage without being knocked over or stumbled by its effects. (Increases with shield/heavy armour)	Royal guard
        /// </summary>
        Defence,
        /// <summary>
        /// Allows the character to dash further or roll further away from taking damage. (Increases with no helmet/light armour) Royal archer
        /// </summary>
        Evasion,
        /// <summary>
        /// resourcing Allows the character to use more magic in a fight with less fatigue. increases with jewellery/belt)	Royal mage
        /// </summary>
        Mana
    }

}
