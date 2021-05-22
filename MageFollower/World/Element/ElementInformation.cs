using System;
using System.Collections.Generic;
using System.Text;

namespace MageFollower.World.Element
{
    public class ElementInformation
    {
        public ElementType Element { get; set; }
        public Dictionary<ElementType, double> 
            DamageMultiplier = 
            new Dictionary<ElementType, double>(); // 0.75 - 1 - 1.25
        public static Dictionary<ElementType, ElementInformation> ElementDamageMultiplier;

        public ElementInformation AddEffect(ElementType type, double damageMultiplier)
        {
            DamageMultiplier[type] = damageMultiplier;
            return this;
        }

        public ElementInformation AddEffects(params (ElementType type, double damageMultiplier)[] types)
        {
            foreach (var item in types)
            {
                DamageMultiplier[item.type] = item.damageMultiplier;
            }
            
            return this;
        }


        static ElementInformation()
        {
            ElementDamageMultiplier = new Dictionary<ElementType, ElementInformation>();

            void AddTo(ElementInformation elementInformation)
            {
                ElementDamageMultiplier.Add(elementInformation.Element, elementInformation);
            }

            AddTo(new ElementInformation()
            {
                Element = ElementType.Fire
            }.AddEffects( // Good Against
                (ElementType.Air, 1.25f),
                (ElementType.Air_Ice, 1.25f),
                (ElementType.Air_Lightning, 1.25f),
                (ElementType.Disbander, 1.25f)
            ).AddEffects( // Neutral Against
                (ElementType.Fire, 1f),
                (ElementType.Earth, 1f)
            ).AddEffects( // Bad Against
                (ElementType.Water, 0.75f),
                (ElementType.Fire_Light, 0.90f),
                (ElementType.Fire_Dark, 0.90f),
                (ElementType.Water_Life, 0.75f),
                (ElementType.Water_Poision, 0.75f),
                (ElementType.Earth_Steel, 0.75f),
                (ElementType.Earth_Crystal, 0.75f)
            ));

            AddTo(new ElementInformation()
            {
                Element = ElementType.Water
            }.AddEffects( // Good Against
                (ElementType.Fire, 1.25f),
                (ElementType.Fire_Light, 1.25f),
                (ElementType.Fire_Dark, 1.25f),
                (ElementType.Disbander, 1.25f)
            ).AddEffects( // Neutral Against
                (ElementType.Air, 1f),
                (ElementType.Water, 1f)
            ).AddEffects( // Bad Against
                (ElementType.Earth, 0.75f),
                (ElementType.Air_Ice, 0.75f),
                (ElementType.Air_Lightning, 0.75f),
                (ElementType.Water_Life, 0.90f),
                (ElementType.Water_Poision, 0.90f),
                (ElementType.Earth_Steel, 0.75f),
                (ElementType.Earth_Crystal, 0.75f)
            ));
        }
    }
}
