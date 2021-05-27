using MageFollower.Client;
using MageFollower.World;
using MageFollower.World.Element;
using MageFollower.World.Skills;
using System;
using System.Threading;

namespace MageFollower
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var game = new Game2D())
                game.Run();

            //var r = new Random();
            //var fireMelee = new Entity() { 
            //    ElementType = ElementType.Fire,
            //    Health = 100,
            //    Name = "The Fire Swordsman"
            //};
            //fireMelee.Melee.AddXp(10000.0f);            
            //fireMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //var waterMelee = new Entity()
            //{
            //    ElementType = ElementType.Water,
            //    Health = 100,
            //    Name = "The Water Swordsman"
            //};
            //waterMelee.Melee.AddXp(10000.0f);
            //waterMelee.RightHand = new World.Items.Item()
            //{
            //    Equipt = World.Items.EquiptType.Physical,
            //    Power = 1.1f,
            //    Type = World.Items.ItemType.Stick
            //};

            //if(r.NextDouble() < 0.5f)
            //{
            //    while (waterMelee.IsAlive && fireMelee.IsAlive)
            //    {
            //        fireMelee.AttackTarget(waterMelee,
            //            r.NextDouble());
            //        Thread.Sleep(100);
            //        if (waterMelee.IsAlive)
            //        {
            //            waterMelee.AttackTarget(fireMelee,
            //                r.NextDouble());

            //            Thread.Sleep(100);
            //        }
            //    }
            //}
            //else
            //{
            //    while (waterMelee.IsAlive && fireMelee.IsAlive)
            //    {
            //        waterMelee.AttackTarget(fireMelee,
            //            r.NextDouble());
            //        Thread.Sleep(100);
            //        if (fireMelee.IsAlive)
            //        {
            //            fireMelee.AttackTarget(waterMelee,
            //                r.NextDouble());

            //            Thread.Sleep(100);
            //        }
            //    }
            //}


            //Console.WriteLine($"The winner is: {(waterMelee.Health > 0 ? waterMelee.Name : fireMelee.Name)}");
            //Console.Read();
        }
    }
}
