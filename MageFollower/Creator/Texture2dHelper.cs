using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MageFollower.Creator
{
    public static class Texture2DHelper
    {
        public static Color GetPixel(ref Color[] colors, int x, int y, int width)
        {
            int index = x + y * width;
            if (index >= 0 && index < colors.Length)
                return colors[index];
            return Color.Transparent;
        }

        public static void SetPixel(ref Color[] colors, Color color, int x, int y, int width)
        {
            int index = x + y * width;
            if(index >= 0 && index < colors.Length)
                colors[x + y * width] = color; ;
        }

        public static Color[] GetPixels(this Texture2D texture)
        {
            Color[] colors1D = new Color[texture.Width * texture.Height];
            texture.GetData(colors1D);
            return colors1D;
        }

        public static int LINE_OVERLAP_NONE = 0 ;	// No line overlap, like in standard Bresenham
        public static int LINE_OVERLAP_MAJOR = 0x01; // Overlap - first go major then minor direction. Pixel is drawn as extension after actual line
        public static int LINE_OVERLAP_MINOR = 0x02; // Overlap - first go minor then major direction. Pixel is drawn as extension before next line
        public static int LINE_OVERLAP_BOTH = 0x03 ; // Overlap - both

        public static int LINE_THICKNESS_MIDDLE = 0;             // Start point is on the line at center of the thick line
        public static int LINE_THICKNESS_DRAW_CLOCKWISE = 1;         // Start point is on the counter clockwise border line
        public static int LINE_THICKNESS_DRAW_COUNTERCLOCKWISE = 2;  // Start point is on the clockwise border line


        public static void DrawLineOverlap(ref Color[] colors, int textureWidth, int textureHeight,  int aXStart, int aYStart, int aXEnd, int aYEnd, int aOverlap,
        Color aColor)
        {
            int tDeltaX, tDeltaY, tDeltaXTimes2, tDeltaYTimes2, tError, tStepX, tStepY;

            /*
             * Clip to display size
             */
            if (aXStart >= textureWidth)
            {
                aXStart = textureWidth - 1;
            }
            if (aXStart < 0)
            {
                aXStart = 0;
            }
            if (aXEnd >= textureWidth)
            {
                aXEnd = textureWidth - 1;
            }
            if (aXEnd < 0)
            {
                aXEnd = 0;
            }
            if (aYStart >= textureHeight)
            {
                aYStart = textureHeight - 1;
            }
            if (aYStart < 0)
            {
                aYStart = 0;
            }
            if (aYEnd >= textureHeight)
            {
                aYEnd = textureHeight - 1;
            }
            if (aYEnd < 0)
            {
                aYEnd = 0;
            }

            //calculate direction
            tDeltaX = aXEnd - aXStart;
            tDeltaY = aYEnd - aYStart;
            if (tDeltaX < 0)
            {
                tDeltaX = -tDeltaX;
                tStepX = -1;
            }
            else
            {
                tStepX = +1;
            }
            if (tDeltaY < 0)
            {
                tDeltaY = -tDeltaY;
                tStepY = -1;
            }
            else
            {
                tStepY = +1;
            }
            tDeltaXTimes2 = tDeltaX << 1;
            tDeltaYTimes2 = tDeltaY << 1;
            //draw start pixel            
            DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart, aColor);

            if (tDeltaX > tDeltaY)
            {
                // start value represents a half step in Y direction
                tError = tDeltaYTimes2 - tDeltaX;
                while (aXStart != aXEnd)
                {
                    // step in main direction
                    aXStart += tStepX;
                    if (tError >= 0)
                    {
                        if ((aOverlap & LINE_OVERLAP_MAJOR) != 0)
                        {
                            // draw pixel in main direction before changing                                
                            DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart, aColor);
                        }
                        // change Y
                        aYStart += tStepY;
                        if ((aOverlap & LINE_OVERLAP_MINOR) != 0)
                        {
                            // draw pixel in minor direction before changing                                
                            DrawPixel(ref colors, textureWidth, textureHeight, aXStart - tStepX, aYStart, aColor);
                        }
                        tError -= tDeltaXTimes2;
                    }
                    tError += tDeltaYTimes2;
                    DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart, aColor);
                }
            }
            else
            {
                tError = tDeltaXTimes2 - tDeltaY;
                while (aYStart != aYEnd)
                {
                    aYStart += tStepY;
                    if (tError >= 0)
                    {
                        if ((aOverlap & LINE_OVERLAP_MAJOR) != 0)
                        {
                            // draw pixel in main direction before changing
                            DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart, aColor);
                        }
                        aXStart += tStepX;
                        if ((aOverlap & LINE_OVERLAP_MINOR) != 0)
                        {
                            // draw pixel in minor direction before changing                                
                            DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart - tStepY, aColor);
                        }
                        tError -= tDeltaYTimes2;
                    }
                    tError += tDeltaXTimes2;
                    DrawPixel(ref colors, textureWidth, textureHeight, aXStart, aYStart, aColor);
                }
            }
        }

        public static void DrawThickLine(ref Color[] colors, int textureWidth, int textureHeight, int aXStart, int aYStart, int aXEnd, int aYEnd, int aThickness,
        int aThicknessMode, Color aColor)
        {
            int i, tDeltaX, tDeltaY, tDeltaXTimes2, tDeltaYTimes2, tError, tStepX, tStepY;

            if (aThickness <= 1)
            {
                DrawLineOverlap(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, LINE_OVERLAP_NONE, aColor);
            }
            /*
             * Clip to display size
             */
            if (aXStart >= textureWidth)
            {
                aXStart = textureWidth - 1;
            }
            if (aXStart < 0)
            {
                aXStart = 0;
            }
            if (aXEnd >= textureWidth)
            {
                aXEnd = textureWidth - 1;
            }
            if (aXEnd < 0)
            {
                aXEnd = 0;
            }
            if (aYStart >= textureHeight)
            {
                aYStart = textureHeight - 1;
            }
            if (aYStart < 0)
            {
                aYStart = 0;
            }
            if (aYEnd >= textureHeight)
            {
                aYEnd = textureHeight - 1;
            }
            if (aYEnd < 0)
            {
                aYEnd = 0;
            }

            /**
             * For coordinate system with 0.0 top left
             * Swap X and Y delta and calculate clockwise (new delta X inverted)
             * or counterclockwise (new delta Y inverted) rectangular direction.
             * The right rectangular direction for LINE_OVERLAP_MAJOR toggles with each octant
             */
            tDeltaY = aXEnd - aXStart;
            tDeltaX = aYEnd - aYStart;
            // mirror 4 quadrants to one and adjust deltas and stepping direction
            bool tSwap = true; // count effective mirroring
            if (tDeltaX < 0)
            {
                tDeltaX = -tDeltaX;
                tStepX = -1;
                tSwap = !tSwap;
            }
            else
            {
                tStepX = +1;
            }
            if (tDeltaY < 0)
            {
                tDeltaY = -tDeltaY;
                tStepY = -1;
                tSwap = !tSwap;
            }
            else
            {
                tStepY = +1;
            }
            tDeltaXTimes2 = tDeltaX << 1;
            tDeltaYTimes2 = tDeltaY << 1;
            int tOverlap;
            // adjust for right direction of thickness from line origin
            int tDrawStartAdjustCount = aThickness / 2;
            if (aThicknessMode == LINE_THICKNESS_DRAW_COUNTERCLOCKWISE)
            {
                tDrawStartAdjustCount = aThickness - 1;
            }
            else if (aThicknessMode == LINE_THICKNESS_DRAW_CLOCKWISE)
            {
                tDrawStartAdjustCount = 0;
            }

            // which octant are we now
            if (tDeltaX >= tDeltaY)
            {
                if (tSwap)
                {
                    tDrawStartAdjustCount = (aThickness - 1) - tDrawStartAdjustCount;
                    tStepY = -tStepY;
                }
                else
                {
                    tStepX = -tStepX;
                }
                /*
                 * Vector for draw direction of start of lines is rectangular and counterclockwise to main line direction
                 * Therefore no pixel will be missed if LINE_OVERLAP_MAJOR is used on change in minor rectangular direction
                 */
                // adjust draw start point
                tError = tDeltaYTimes2 - tDeltaX;
                for (i = tDrawStartAdjustCount; i > 0; i--)
                {
                    // change X (main direction here)
                    aXStart -= tStepX;
                    aXEnd -= tStepX;
                    if (tError >= 0)
                    {
                        // change Y
                        aYStart -= tStepY;
                        aYEnd -= tStepY;
                        tError -= tDeltaXTimes2;
                    }
                    tError += tDeltaYTimes2;
                }
                //draw start line
                DrawLine(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, aColor);
                // draw aThickness number of lines
                tError = tDeltaYTimes2 - tDeltaX;
                for (i = aThickness; i > 1; i--)
                {
                    // change X (main direction here)
                    aXStart += tStepX;
                    aXEnd += tStepX;
                    tOverlap = LINE_OVERLAP_NONE;
                    if (tError >= 0)
                    {
                        // change Y
                        aYStart += tStepY;
                        aYEnd += tStepY;
                        tError -= tDeltaXTimes2;
                        /*
                         * Change minor direction reverse to line (main) direction
                         * because of choosing the right (counter)clockwise draw vector
                         * Use LINE_OVERLAP_MAJOR to fill all pixel
                         *
                         * EXAMPLE:
                         * 1,2 = Pixel of first 2 lines
                         * 3 = Pixel of third line in normal line mode
                         * - = Pixel which will additionally be drawn in LINE_OVERLAP_MAJOR mode
                         *           33
                         *       3333-22
                         *   3333-222211
                         * 33-22221111
                         *  221111                     /\
                         *  11                          Main direction of start of lines draw vector
                         *  -> Line main direction
                         *  <- Minor direction of counterclockwise of start of lines draw vector
                         */
                        tOverlap = LINE_OVERLAP_MAJOR;
                    }
                    tError += tDeltaYTimes2;
                    DrawLineOverlap(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, tOverlap, aColor);
                }
            }
            else
            {
                // the other octant
                if (tSwap)
                {
                    tStepX = -tStepX;
                }
                else
                {
                    tDrawStartAdjustCount = (aThickness - 1) - tDrawStartAdjustCount;
                    tStepY = -tStepY;
                }
                // adjust draw start point
                tError = tDeltaXTimes2 - tDeltaY;
                for (i = tDrawStartAdjustCount; i > 0; i--)
                {
                    aYStart -= tStepY;
                    aYEnd -= tStepY;
                    if (tError >= 0)
                    {
                        aXStart -= tStepX;
                        aXEnd -= tStepX;
                        tError -= tDeltaYTimes2;
                    }
                    tError += tDeltaXTimes2;
                }
                //draw start line
                DrawLine(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, aColor);
                // draw aThickness number of lines
                tError = tDeltaXTimes2 - tDeltaY;
                for (i = aThickness; i > 1; i--)
                {
                    aYStart += tStepY;
                    aYEnd += tStepY;
                    tOverlap = LINE_OVERLAP_NONE;
                    if (tError >= 0)
                    {
                        aXStart += tStepX;
                        aXEnd += tStepX;
                        tError -= tDeltaYTimes2;
                        tOverlap = LINE_OVERLAP_MAJOR;
                    }
                    tError += tDeltaXTimes2;
                    DrawLineOverlap(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, tOverlap, aColor);
                }
            }
        }
        /**
         * The same as before, but no clipping to display range, some pixel are drawn twice (because of using LINE_OVERLAP_BOTH)
         * and direction of thickness changes for each octant (except for LINE_THICKNESS_MIDDLE and aThickness value is odd)
         * aThicknessMode can be LINE_THICKNESS_MIDDLE or any other value
         *
         */
        public static void DrawThickLineSimple(ref Color[] colors, int textureWidth, int textureHeight,int aXStart, int aYStart, int aXEnd, int aYEnd,
                int aThickness, int aThicknessMode, Color aColor)
        {
            int i, tDeltaX, tDeltaY, tDeltaXTimes2, tDeltaYTimes2, tError, tStepX, tStepY;

            tDeltaY = aXStart - aXEnd;
            tDeltaX = aYEnd - aYStart;
            // mirror 4 quadrants to one and adjust deltas and stepping direction
            if (tDeltaX < 0)
            {
                tDeltaX = -tDeltaX;
                tStepX = -1;
            }
            else
            {
                tStepX = +1;
            }
            if (tDeltaY < 0)
            {
                tDeltaY = -tDeltaY;
                tStepY = -1;
            }
            else
            {
                tStepY = +1;
            }
            tDeltaXTimes2 = tDeltaX << 1;
            tDeltaYTimes2 = tDeltaY << 1;
            int tOverlap;
            // which octant are we now
            if (tDeltaX > tDeltaY)
            {
                if (aThicknessMode == LINE_THICKNESS_MIDDLE)
                {
                    // adjust draw start point
                    tError = tDeltaYTimes2 - tDeltaX;
                    for (i = aThickness / 2; i > 0; i--)
                    {
                        // change X (main direction here)
                        aXStart -= tStepX;
                        aXEnd -= tStepX;
                        if (tError >= 0)
                        {
                            // change Y
                            aYStart -= tStepY;
                            aYEnd -= tStepY;
                            tError -= tDeltaXTimes2;
                        }
                        tError += tDeltaYTimes2;
                    }
                }
                //draw start line                
                DrawLine(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, aColor);
                // draw aThickness lines
                tError = tDeltaYTimes2 - tDeltaX;
                for (i = aThickness; i > 1; i--)
                {
                    // change X (main direction here)
                    aXStart += tStepX;
                    aXEnd += tStepX;
                    tOverlap = LINE_OVERLAP_NONE;
                    if (tError >= 0)
                    {
                        // change Y
                        aYStart += tStepY;
                        aYEnd += tStepY;
                        tError -= tDeltaXTimes2;
                        tOverlap = LINE_OVERLAP_BOTH;
                    }
                    tError += tDeltaYTimes2;
                    DrawLineOverlap(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, tOverlap, aColor);
                }
            }
            else
            {
                // adjust draw start point
                if (aThicknessMode == LINE_THICKNESS_MIDDLE)
                {
                    tError = tDeltaXTimes2 - tDeltaY;
                    for (i = aThickness / 2; i > 0; i--)
                    {
                        aYStart -= tStepY;
                        aYEnd -= tStepY;
                        if (tError >= 0)
                        {
                            aXStart -= tStepX;
                            aXEnd -= tStepX;
                            tError -= tDeltaYTimes2;
                        }
                        tError += tDeltaXTimes2;
                    }
                }
                //draw start line                
                DrawLine(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, aColor);
                tError = tDeltaXTimes2 - tDeltaY;
                for (i = aThickness; i > 1; i--)
                {
                    aYStart += tStepY;
                    aYEnd += tStepY;
                    tOverlap = LINE_OVERLAP_NONE;
                    if (tError >= 0)
                    {
                        aXStart += tStepX;
                        aXEnd += tStepX;
                        tError -= tDeltaYTimes2;
                        tOverlap = LINE_OVERLAP_BOTH;
                    }
                    tError += tDeltaXTimes2;
                    DrawLineOverlap(ref colors, textureWidth, textureHeight, aXStart, aYStart, aXEnd, aYEnd, tOverlap, aColor);
                }
            }
        }    
        
        public static void DrawPixel(ref Color[] colors, int textureWidth, int textureHeight, int x0, int y0, Color color)
        {
            if (x0 >= 0 && y0 >= 0 && x0 < textureWidth && y0 < textureHeight)
                SetPixel(ref colors, color, x0, y0, textureWidth);
        }

        public static void DrawLine(ref Color[] colors, int textureWidth, int textureHeight, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;
            for (; ; )
            {
                if(x0 >= 0 && y0 >= 0 && x0 < textureWidth && y0 < textureHeight)                
                    SetPixel(ref colors, color, x0, y0, textureWidth);                
                
                if (x0 == x1 && y0 == y1) break;
                e2 = err;
                if (e2 > -dx) { err -= dy; x0 += sx; }
                if (e2 < dy) { err += dx; y0 += sy; }
            }            
        }
    }
}
