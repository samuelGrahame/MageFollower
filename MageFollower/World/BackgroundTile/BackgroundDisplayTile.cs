using MageFollower.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MageFollower.World.BackgroundTile
{
    public class BackgroundDisplayTile
    {
        // Chunk-Size is 512 by 512
        private WorldGameState _gameState;
        public BackgroundDisplayTile(WorldGameState gameState)
        {
            _gameState = gameState;
        }

        public const int ChunkSize = 1024;
        public static int ChunkSizeHalf = ChunkSize / 2;

        private ConcurrentDictionary<Point, Texture2D> _cacheTextureSystem = new();
        public Point GetChunkIndexByWorldPos(Point pt)
        {

            var xMH = (float)pt.X - ChunkSizeHalf;
            var yMH = (float)pt.Y - ChunkSizeHalf;

            int newX;
            int newY;


            newX = (int)MathF.Round((xMH) / ChunkSize) * ChunkSize;
            newY = (int)MathF.Round((yMH) / ChunkSize) * ChunkSize;

            return new Point(newX, newY);
        }

        public Dictionary<Point, Texture2D> GetTilesForPlayer(Entity player)
        {
            if (player == null)
                return null;
            var width = _gameState.Client.GraphicsDevice.Viewport.Width;
            var height = _gameState.Client.GraphicsDevice.Viewport.Height;

            width = (int)(width * (1.0f / _gameState.WorldZoom));
            height = (int)(height * (1.0f / _gameState.WorldZoom));

            // lets get view...            
            var half = new Vector2(
                width * 0.5f, 
                (height * 0.5f));

            var start = player.Position - half;

            var list = new Dictionary<Point, Texture2D>();

            var howManyColumns = (int)Math.Max((float)width / ChunkSize, 1) + 2;
            var howManyRows = (int)Math.Max((float)height / ChunkSize, 1) + 2;

            for (int x = -1; x < howManyColumns; x++)
            {
                for (int y = -1; y < howManyRows; y++)
                {
                   var index =  GetChunkIndexByWorldPos(new Point(
                        (int)(start.X + (x * ChunkSize)),
                        (int)(start.Y + (y * ChunkSize))));

                    if(!list.ContainsKey(index))
                    {
                        list[index] = GetTextureFromIndex(index);
                    }
                }
            }

            return list;
        }

        public List<(Point Index, Texture2D Texture)> GetTexturesFromTwoPoints(Point point1, Point point2)
        {
            var list = new List<(Point Index, Texture2D Texture)>();
            var chunkIndex1 = GetChunkIndexByWorldPos(point1);
            var chunkIndex2 = GetChunkIndexByWorldPos(point2);

            if(chunkIndex2 == chunkIndex1)
            {
                list.Add((chunkIndex1, GetTextureFromIndex(chunkIndex1)));
                return list; 
            }

            list.Add((chunkIndex1, GetTextureFromIndex(chunkIndex1)));
            list.Add((chunkIndex2, GetTextureFromIndex(chunkIndex2)));

            return list;            
        }

        public Texture2D GetTextureFromIndex(Point chunkIndex)
        {
            if (_cacheTextureSystem.ContainsKey(chunkIndex))
            {
                return _cacheTextureSystem[chunkIndex];
            }
            var texture = new Texture2D(_gameState.Client.GraphicsDevice, ChunkSize, ChunkSize);
            _cacheTextureSystem[chunkIndex] = texture;
            return texture;
        }

        public void SetTextureFromIndex(Point chunkIndex, Texture2D texture)
        {
            // Override...
            _cacheTextureSystem[chunkIndex] = texture;
        }
    }
}
