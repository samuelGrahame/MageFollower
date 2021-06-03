using MageFollower.PacketData;
using MageFollower.Projectiles;
using MageFollower.World.Element;
using MageFollower.World.Items;
using MageFollower.World.Skills;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static MageFollower.Program;

namespace MageFollower.World
{
    public class Entity
    {
        [JsonIgnore]
        public Entity TargetEntity;
        [JsonIgnore]
        public long AttackSleep;

        [JsonProperty("i0")]
        public string Id;
        [JsonProperty("i1")]
        public double Health;
        [JsonProperty("i2")]
        public double MaxHealth;
        [JsonProperty("i3")]
        public string Name;
        [JsonProperty("i4")]
        public ElementType ElementType;
        [JsonProperty("i5")]
        public Race Race;
        [JsonProperty("i6")]
        public Vector2 Position;
        [JsonProperty("i7")]
        public float Speed = 100;

        [JsonIgnore]
        public Vector2 TargetPos;
        [JsonIgnore]
        public float TargetRotation;
        [JsonIgnore]
        public bool LerpToTarger;
        [JsonIgnore]
        public double TotalTimeLerp;

        public ProjectileTypes GetProjectTileType()
        {
            if (RightHand == null)
                return ProjectileTypes.None;

            if (RightHand.Equipt == EquiptType.Ranged)
                return ProjectileTypes.Arrow;

            if (RightHand.Equipt == EquiptType.Magic)
                return ProjectileTypes.EnergyBall;

            return ProjectileTypes.None;
        }

        public float GetAttackRange()
        {
            switch (GetProjectTileType())
            {
                default:
                case ProjectileTypes.None:
                    return 75.0f;
                case ProjectileTypes.EnergyBall:
                    return 600.0f;
                case ProjectileTypes.Arrow:
                    return 700.0f;
            }            
        }

        [JsonProperty("i8")]
        public float Rotation;

        public static Vector2 Origin = new Vector2(128, 128);

        [JsonProperty("i9")]
        public Color Color = Color.White;

        public bool IsAlive => Health > 0.0001f;

        #region Skills
        [JsonProperty("i10")]
        // Profession
        /// <summary>
        /// Allows the character to create new items with collected materials used for crafting. (includes smelting { get; set; } cloth making { get; set; } woodwork { get; set; } smithing) Blacksmith
        /// </summary>
        public Skill Crafting { get; set; } = new Skill(SkillType.Crafting, 0);

        [JsonProperty("i11")]
        /// <summary>
        /// Allows the character to chop trees from blocking paths and collecting wood.Lumberjack
        /// </summary>
        public Skill Wood_Cutting { get; set; } = new Skill(SkillType.Wood_Cutting, 0);

        [JsonProperty("i12")]
        /// <summary>
        /// Allows the character to clear overgrown grassy areas and collect fibre used for crafting.Farmer
        /// </summary>
        public Skill Fibre_Harvesting { get; set; } = new Skill(SkillType.Fibre_Harvesting, 0);

        [JsonProperty("i13")]
        /// <summary>
        /// Allows the character to clear rocks from caves and collect ore used for crafting.Miner
        /// </summary>
        public Skill Mining { get; set; } = new Skill(SkillType.Mining, 0);

        [JsonProperty("i14")]
        /// <summary>
        /// Allows the character to collect meat and hide for their village/town/city.Hunter
        /// </summary>
        public Skill Hunting { get; set; } = new Skill(SkillType.Hunting, 0);

        [JsonProperty("i15")]
        /// <summary>
        /// Allows the character to collect berries/mushrooms and sticks for their village/town/city.Gatherer
        /// </summary>
        public Skill Gathering { get; set; } = new Skill(SkillType.Gathering, 0);

        [JsonProperty("i17")]
        /// <summary>
        /// Allows the character to carry given resources from their village/town/city to another village/town/city and back to their village/town/city.Also makes maps between them { get; set; } increases trade and different resources.Map navigator
        /// </summary>
        public Skill Transporting { get; set; } = new Skill(SkillType.Transporting, 0);

        [JsonProperty("i18")]
        /// <summary>
        /// Allows the character to read words/pages/books/maps which can be used to help the player and others.Librarian { get; set; } Teacher (requires library/school)
        /// </summary>
        public Skill Reading { get; set; } = new Skill(SkillType.Reading, 0);

        [JsonProperty("i19")]
        /// <summary>
        /// Allows the character to sound more intellectual { get; set; } logical and influence on others.Politician { get; set; } Pope (requires kingdom/church)
        /// </summary>
        /// 
        public Skill Speech { get; set; } = new Skill(SkillType.Speech, 0);

        [JsonProperty("i20")]
        /// <summary>
        /// Allows the character to impress others with their skill of sword swinging { get; set; } magic displaying or trophies.This increases influence on others.	Entertainer { get; set; } Knight (requires skill/blessing)
        /// </summary>
        public Skill Entertainment { get; set; } = new Skill(SkillType.Entertainment, 0);
        //Combat 

        [JsonProperty("i21")]
        /// <summary>
        /// Allows the character to effectively use spells in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Magic { get; set; } = new Skill(SkillType.Magic, 0);

        [JsonProperty("i22")]
        /// <summary>
        /// Allows the character to effectively use melee in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Melee { get; set; } = new Skill(SkillType.Melee, 0);

        [JsonProperty("i23")]
        /// <summary>
        /// Allows the character to effectively use ranged weapons in combat. (speed/damage/accuracy)	Bounty hunter/Assassin
        /// </summary>
        public Skill Ranged { get; set; } = new Skill(SkillType.Ranged, 0);

        [JsonProperty("i24")]
        /// <summary>
        /// Allows the character to critical strike unsuspected enemies { get; set; } steal items and hide.Thief/Detective
        /// </summary>
        public Skill Sneak { get; set; } = new Skill(SkillType.Sneak, 0);

        [JsonProperty("i25")]
        /// <summary>
        /// stance Allows the character to take more damage without being knocked over or stumbled by its effects. (Increases with shield/heavy armour)	Royal guard
        /// </summary>
        public Skill Defence { get; set; } = new Skill(SkillType.Defence, 0);

        [JsonProperty("i26")]
        /// <summary>
        /// Allows the character to dash further or roll further away from taking damage. (Increases with no helmet/light armour) Royal archer
        /// </summary>
        public Skill Evasion { get; set; } = new Skill(SkillType.Evasion, 0);

        [JsonProperty("i27")]
        /// <summary>
        /// resourcing Allows the character to use more magic in a fight with less fatigue. increases with jewellery/belt)	Royal mage
        /// </summary>
        public Skill Mana { get; set; } = new Skill(SkillType.Mana, 0);
        #endregion


        [JsonProperty("i28")]
        /// <summary>
        /// For Weapon
        /// </summary>
        public Item RightHand;

        [JsonProperty("i29")]
        /// <summary>
        /// For Defence
        /// </summary>
        public Item LeftHand;

        [JsonProperty("i30")]
        public Item Head;
        [JsonProperty("i31")]
        public Item Body;
        [JsonProperty("i32")]
        public Item Belt;
        [JsonProperty("i33")]
        public Item Feet;
        [JsonProperty("i34")]
        public Item Back;
        private static Dictionary<string, FieldInfo> skillFields = null;
        public bool AddXpToSkill(XpToTarget xpToTarget)
        {
            if(skillFields == null)
            {
                _setupSkillFieldsCache();
            }

            if(skillFields.ContainsKey(xpToTarget.Level))
            {
                return ((Skill)skillFields[xpToTarget.Level].GetValue(this)).AddXp(xpToTarget.Xp) > 0;
            }
            return false;
        }

        private static void _setupSkillFieldsCache()
        {
            skillFields = new Dictionary<string, FieldInfo>();
            var list = new List<FieldInfo>();//
            list.AddRange(typeof(Entity).GetFields());
            list.RemoveAll(o => o.FieldType != typeof(Skill));
            foreach (var item in list)
            {
                if(item == typeof(Skill))
                {
                    skillFields.Add(item.Name, item);
                }
            }

        }

        public bool OnHit(Entity fromTarget, double damage)
        {
            // do we have some kind of defence
            // do we have armor?
            double baseArmor = 0.0f;

            baseArmor += Head?.Power ?? 0.0f;
            baseArmor += Body?.Power ?? 0.0f;
            baseArmor += Belt?.Power ?? 0.0f;
            baseArmor += Feet?.Power ?? 0.0f;
            baseArmor += Back?.Power ?? 0.0f;
            baseArmor += LeftHand?.Power ?? 0.0f;

            baseArmor *= Defence.Level;

            damage *= (baseArmor > 0 ? 
                100.0f / (100.0f + baseArmor) : 
                1.5f - (100.0f / (100.0f + baseArmor)));

            this.Health -= damage;
            if (this.Health < 0)
                this.Health = 0;

            if(this.Health > this.MaxHealth)
            {
                this.Health = this.MaxHealth;
            }

            Console.WriteLine($"{fromTarget.Name} done {damage:n2} damage to {this.Name}");

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

            return target.OnHit(this, (baseDamage + 
                GetDamageBasedOffWeapon(RightHand)) * damageMultiplier);
        }

        public double GetDamageBasedOffWeapon(Item weapon)
        {
            if (weapon == null)
                return 0.0f;
            double baseDamange = 0.0f;
            switch (weapon.Equipt)
            {                
                case EquiptType.Physical:
                    baseDamange = Melee.Level;
                    break;
                case EquiptType.Ranged:
                    baseDamange = Ranged.Level;
                    break;
                case EquiptType.Magic:
                    baseDamange = Magic.Level;
                    break;
            }
            return baseDamange * weapon.Power;
        }
    }
}
