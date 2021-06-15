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

        public const int ChunkSize = 512;
        public static int ChunkSizeHalf = ChunkSize / 2;

        public static Color[] ImageCache = null;

        private ConcurrentDictionary<Point, Texture2D> _cacheTextureSystem = new();
        public Point GetChunkIndexByWorldPos(Point pt)
        {

            var xMH = (float)pt.X - ChunkSizeHalf;
            var yMH = (float)pt.Y - ChunkSizeHalf;

            int newX;
            int newY;

            if(pt.X > 0 && pt.X < ChunkSize)
            {
                newX = 0;
            }else if (pt.X < 0 && pt.X >= -ChunkSize)
            {
                newX = -ChunkSize;
            }
            else
            {
                newX = (int)MathF.Round((xMH) / ChunkSize) * ChunkSize;
            }

            if (pt.Y > 0 && pt.Y < ChunkSize)
            {
                newY = 0;
            }
            else if (pt.Y < 0 && pt.Y >= -ChunkSize)
            {
                newY = -ChunkSize;
            }
            else
            {
                newY = (int)MathF.Round((yMH) / ChunkSize) * ChunkSize;
            }

            return new Point(newX, newY);
        }

        public Dictionary<Point, Texture2D> GetTilesForPos(Point pos)
        {            
            var width = _gameState.Client.GraphicsDevice.Viewport.Width;
            var height = _gameState.Client.GraphicsDevice.Viewport.Height;

            width = (int)(width * (1.0f / _gameState.WorldZoom));
            height = (int)(height * (1.0f / _gameState.WorldZoom));

            // lets get view...            
            var half = new Point(
                (int)(width * 0.5f), 
                (int)(height * 0.5f));

            

            var start = new Point(pos.X - half.X, pos.Y - half.Y);

            var startChunk = GetChunkIndexByWorldPos(start);

            var rectangeView = new Rectangle(start, new Point(width, height));
            var list = new Dictionary<Point, Texture2D>();

            var howManyColumns = (int)Math.Max((float)width / ChunkSize, 1) + 2;
            var howManyRows = (int)Math.Max((float)height / ChunkSize, 1) + 2;

            for (int x = -1; x < howManyColumns; x++)
            {
                for (int y = -1; y < howManyRows; y++)
                {
                    var index = startChunk + new Point(x * ChunkSize, y * ChunkSize);
                    if(rectangeView.Intersects(new Rectangle(index, new Point(ChunkSize, ChunkSize))))
                    {
                        if (!list.ContainsKey(index))
                        {
                            list[index] = GetTextureFromIndex(index);
                        }
                        else
                        {

                        }
                    }
                }
            }

            return list;
        }

        public List<(Point Index, Texture2D Texture)> GetTexturesFromTwoPoints(Point point1, Point point2, int penThinkness = 1)
        {
            var list = new List<(Point Index, Texture2D Texture)>();
            var chunkIndex1 = GetChunkIndexByWorldPos(point1);
            var chunkIndex2 = GetChunkIndexByWorldPos(point2);

            if(chunkIndex2 == chunkIndex1)
            {
                list.Add((chunkIndex1, GetTextureFromIndex(chunkIndex1)));
                if (penThinkness > 1)
                {
                    GetTexturesFromNearEdge(list, chunkIndex2, point1, penThinkness);                   
                }
                return list; 
            }

            list.Add((chunkIndex1, GetTextureFromIndex(chunkIndex1)));
            list.Add((chunkIndex2, GetTextureFromIndex(chunkIndex2)));

            if (penThinkness > 1)
            {
                GetTexturesFromNearEdge(list, chunkIndex2, point1, penThinkness);
                GetTexturesFromNearEdge(list, chunkIndex2, point2, penThinkness);                
            }

            return list;            
        }

        public void GetTexturesFromNearEdge(List<(Point Index, Texture2D Texture)> list, Point chunkPoint, Point point, int penThinkness = 1)
        {
            var xNewChunk = GetChunkIndexByWorldPos(point + new Point(penThinkness, 0));
            if (!list.Any(o => o.Index == xNewChunk))
            {
                list.Add((xNewChunk, GetTextureFromIndex(xNewChunk)));
            }
            var xNewChunk2 = GetChunkIndexByWorldPos(point - new Point(penThinkness, 0));
            if (!list.Any(o => o.Index == xNewChunk2))
            {
                list.Add((xNewChunk2, GetTextureFromIndex(xNewChunk2)));
            }

            var xNewChunk3 = GetChunkIndexByWorldPos(point - new Point(penThinkness, penThinkness));
            if (!list.Any(o => o.Index == xNewChunk3))
            {
                list.Add((xNewChunk3, GetTextureFromIndex(xNewChunk3)));
            }


            var xNewChunk4 = GetChunkIndexByWorldPos(point + new Point(penThinkness, penThinkness));
            if (!list.Any(o => o.Index == xNewChunk4))
            {
                list.Add((xNewChunk4, GetTextureFromIndex(xNewChunk4)));
            }

            var xNewChunk5 = GetChunkIndexByWorldPos(point + new Point(-penThinkness, penThinkness));
            if (!list.Any(o => o.Index == xNewChunk5))
            {
                list.Add((xNewChunk5, GetTextureFromIndex(xNewChunk5)));
            }


            var xNewChunk6 = GetChunkIndexByWorldPos(point + new Point(penThinkness, -penThinkness));
            if (!list.Any(o => o.Index == xNewChunk6))
            {
                list.Add((xNewChunk6, GetTextureFromIndex(xNewChunk6)));
            }

            var xNewChunk7 = GetChunkIndexByWorldPos(point - new Point(0, penThinkness));
            if (!list.Any(o => o.Index == xNewChunk7))
            {
                list.Add((xNewChunk7, GetTextureFromIndex(xNewChunk7)));
            }

            var xNewChunk8 = GetChunkIndexByWorldPos(point + new Point(0, penThinkness));
            if (!list.Any(o => o.Index == xNewChunk8))
            {
                list.Add((xNewChunk8, GetTextureFromIndex(xNewChunk8)));
            }            
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
