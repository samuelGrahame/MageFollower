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

    }
}
