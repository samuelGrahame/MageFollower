using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.World
{
    public class Enviroment
    {
        public string ChunkId; // TODO load via Chunk Id
        public ConcurrentDictionary<Guid, EnviromentItem> EnviromentItems = new();
        public bool IsDirty;

        public EnviromentItem AddItem(EnviromentType itemType, Vector2 position)
        {
            var item = new EnviromentItem() { Guid = Guid.NewGuid(), ItemType = itemType, Position = position };
            EnviromentItems[item.Guid] = item;
            IsDirty = true;
            return item;
        }

    }

    public class EnviromentItem
    {
        public EnviromentType ItemType;
        public Vector2 Position;
        public Guid Guid;

        public TaskType GetTaskType()
        {
            // TODO Add Dictinary of task types per item.
            if(ItemType == EnviromentType.Tree01 || ItemType == EnviromentType.Tree02)
            {
                return TaskType.ChopTree;
            }
            return TaskType.None;
        }        
    }

    public enum TaskType
    {
        None,
        ChopTree
    }

    public enum EnviromentType
    {
        None,
        Tree01,
        Tree02
    }
}
