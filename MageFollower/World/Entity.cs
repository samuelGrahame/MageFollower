using MageFollower.World.Element;
using MageFollower.World.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World
{
    public class Entity
    {
        public string Id { get; set; }
        public double Health { get; set; }
        public string Name { get; set; }
        public ElementType ElementType { get; set; }
        public Race Race { get; set; }

        // Profession
        /// <summary>
        /// Allows the character to create new items with collected materials used for crafting. (includes smelting { get; set; } cloth making { get; set; } woodwork { get; set; } smithing) Blacksmith
        /// </summary>
        public Skill Crafting { get; set; } = new Skill(SkillType.Crafting, 0);
        /// <summary>
        /// Allows the character to chop trees from blocking paths and collecting wood.Lumberjack
        /// </summary>
        public Skill Wood_Cutting { get; set; } = new Skill(SkillType.Wood_Cutting, 0);
        /// <summary>
        /// Allows the character to clear overgrown grassy areas and collect fibre used for crafting.Farmer
        /// </summary>
        public Skill Fibre_Harvesting { get; set; } = new Skill(SkillType.Fibre_Harvesting, 0);
        /// <summary>
        /// Allows the character to clear rocks from caves and collect ore used for crafting.Miner
        /// </summary>
        public Skill Mining { get; set; } = new Skill(SkillType.Mining, 0);
        /// <summary>
        /// Allows the character to collect meat and hide for their village/town/city.Hunter
        /// </summary>
        public Skill Hunting { get; set; } = new Skill(SkillType.Hunting, 0);
        /// <summary>
        /// Allows the character to collect berries/mushrooms and sticks for their village/town/city.Gatherer
        /// </summary>
        public Skill Gathering { get; set; } = new Skill(SkillType.Gathering, 0);
        /// <summary>
        /// Allows the character to carry given resources from their village/town/city to another village/town/city and back to their village/town/city.Also makes maps between them { get; set; } increases trade and different resources.Map navigator
        /// </summary>
        public Skill Transporting { get; set; } = new Skill(SkillType.Transporting, 0);
        /// <summary>
        /// Allows the character to read words/pages/books/maps which can be used to help the player and others.Librarian { get; set; } Teacher (requires library/school)
        /// </summary>
        public Skill Reading { get; set; } = new Skill(SkillType.Reading, 0);
        /// <summary>
        /// Allows the character to sound more intellectual { get; set; } logical and influence on others.Politician { get; set; } Pope (requires kingdom/church)
        /// </summary>
        public Skill Speech { get; set; } = new Skill(SkillType.Speech, 0);
        /// <summary>
        /// Allows the character to impress others with their skill of sword swinging { get; set; } magic displaying or trophies.This increases influence on others.	Entertainer { get; set; } Knight (requires skill/blessing)
        /// </summary>
        public Skill Entertainment { get; set; } = new Skill(SkillType.Entertainment, 0);
        //Combat 
        /// <summary>
        /// Allows the character to effectively use spells in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Magic { get; set; } = new Skill(SkillType.Magic, 0);
        /// <summary>
        /// Allows the character to effectively use melee in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Melee { get; set; } = new Skill(SkillType.Melee, 0);
        /// <summary>
        /// Allows the character to effectively use ranged weapons in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Ranged { get; set; } = new Skill(SkillType.Ranged, 0);
        /// <summary>
        /// Allows the character to critical strike unsuspected enemies { get; set; } steal items and hide.Thief/Detective
        /// </summary>
        public Skill Sneak { get; set; } = new Skill(SkillType.Sneak, 0);
        /// <summary>
        /// stance Allows the character to take more damage without being knocked over or stumbled by its effects. (Increases with shield/heavy armour)	Royal guard
        /// </summary>
        public Skill Defence { get; set; } = new Skill(SkillType.Defence, 0);
        /// <summary>
        /// Allows the character to dash further or roll further away from taking damage. (Increases with no helmet/light armour) Royal archer
        /// </summary>
        public Skill Evasion { get; set; } = new Skill(SkillType.Evasion, 0);
        /// <summary>
        /// resourcing Allows the character to use more magic in a fight with less fatigue. increases with jewellery/belt)	Royal mage
        /// </summary>
        public Skill Mana { get; set; } = new Skill(SkillType.Mana, 0);

        public bool OnHit(Entity fromEntity, double damage)
        {
            // do we have some kind of defence
            // do we have armor?
            this.Health -= damage;
            if (this.Health < 0)
                this.Health = 0;

            return this.Health <= 0;
        }

        public bool AttackTarget(Entity target, double baseDamage)
        {
            if (target == null)
                return false;

            var elementInformation = ElementInformation.ElementDamageMultiplier[ElementType];

            double damageMultiplier = 1.0f;

            if (elementInformation.DamageMultiplier.ContainsKey(target.ElementType))
                damageMultiplier = elementInformation.DamageMultiplier[target.ElementType];

            return OnHit(this, (baseDamage * damageMultiplier));
        }
    }
}
