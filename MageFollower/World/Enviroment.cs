﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.World
{
    public class Enviroment
    {
        public string ChunkId; // TODO load via Chunk Id
        public List<EnviromentItem> EnviromentItems = new();
        public bool IsDirty;

        public EnviromentItem AddItem(EnviromentType itemType, Vector2 position)
        {
            var item = new EnviromentItem() { Guid = Guid.NewGuid(), ItemType = itemType, Position = position };
            EnviromentItems.Add(item);
            IsDirty = true;
            return item;
        }

    }

    public class EnviromentItem
    {
        public EnviromentType ItemType;
        public Vector2 Position;
        public Guid Guid;
    }

    public enum EnviromentType
    {
        None,
        Tree01
    }
}
