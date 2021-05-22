using MageFollower.World.Element;
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
