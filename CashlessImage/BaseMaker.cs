using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace CashlessImage
{
    public abstract class BaseMaker
    {
        public abstract void Run();

        public const int HEADER_LENGTH = 32 * 2;
        public int BitsPer { get; set; }

        public int NextWriteableBit(int pixelDataPtr)
        {
            return NextWriteableLSBFirst(pixelDataPtr);
        }

        /// <summary>
        /// when writing header, use all 8 bits but leave alpha alone 
        /// </summary>
        public int NextWriteableBitHeader(int pixelDataPtr)
        {
            bool isEndOfColor = (pixelDataPtr + 1) % 32 == 0;
            if (isEndOfColor)
            {
                // jump to start of first component of next color
                return pixelDataPtr + 1 + 8;
            }
            else
            {
                return pixelDataPtr + 1;
            }
        }

        public int NextWriteableLSBFirst(int pixelDataPtr)
        {
            // number of low bits to write per pixel
            int bits_per = BitsPer;

            int ptr = pixelDataPtr % 32;
            bool isAlpha = ptr < 8;
            bool isEndOfComponent = (ptr + 1) % 8 == 0;
            bool isEndOfColor = (ptr + 1) % 32 == 0;

            bool stillWithinLOB = (ptr + 1) % 8 < bits_per;

            if (stillWithinLOB)
            {
                // stay within the current component 
                return pixelDataPtr + 1;
            }
            else if (isAlpha && !isEndOfComponent)
            {
                // all of alpha is writeable 
                return pixelDataPtr + 1;
            }
            else if (isEndOfColor)
            {
                // one step should take us into alpha
                return pixelDataPtr + 1;
            }
            else
            {
                // jump to start of next component (rgba)
                return pixelDataPtr + (8 - ptr % 8);
            }
        }

        public int NextWriteableMSBFirst(int pixelDataPtr)
        {
            // number of low bits to write per pixel
            int bits_per = BitsPer;

            int ptr = pixelDataPtr % 32;
            bool isAlpha = ptr < 8;
            bool isEndOfComponent = (ptr + 1) % 8 == 0;
            bool isEndOfColor = (ptr + 1) % 32 == 0;

            if (isEndOfColor)
            {
                // start next component - will be alpha
                return pixelDataPtr + 1;
            }
            else if (isAlpha && !isEndOfComponent)
            {
                // all of alpha is writeable 
                return pixelDataPtr + 1;
            }
            else if (isEndOfComponent)
            {
                return pixelDataPtr + 1 + 8 - bits_per;
            }
            else
            {
                // still within low order bits 
                return pixelDataPtr + 1;
            }
        }

        // Use this to ensure we only write to a rectangle within the image
        public bool IsWritablePixel(int pixelPtr, int width, int height)
        {
            // SKIP FOR NOW
            return true;

            var x = pixelPtr % width;
            var y = pixelPtr / width;
            int minX = width / 2;
            int maxX = 4 * width / 5;
            int minY = 9 * height / 12;
            int maxY = 10 * height / 12;

            while (maxX - minX < 10)
            {
                minX--;
                maxX++;
            }

            while (maxY - minY < 10)
            {
                minY--;
                minY++;
            }

            bool xInRange = (minX <= x) && (x <= maxX);
            bool yInRange = (minY <= y) && (y <= maxY);
            return (xInRange && yInRange);
        }

        #region helper methods 

        public SKBitmap LoadBmpFromFile(string inputFilePath)
        {
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return SKBitmap.Decode(fs);
            }
        }

        public IEnumerable<int> PixelsFromBitmap(SKBitmap bmp)
        {
            if (bmp.Info.ColorType != SKColorType.Rgba8888
                || bmp.Info.AlphaType != SKAlphaType.Premul)
            {
                bmp = ARGBBitmapFromImage(bmp);
            }

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);
                    yield return (int)(uint)pixel;
                }
            }
        }

        /// <summary>
        /// Convert bitmap to expected ARGB format 
        /// </summary>
        /// <param name="orig"></param>
        /// <returns></returns>
        public SKBitmap ARGBBitmapFromImage(SKBitmap orig)
        {
            return orig;
            //FIXME convert format 
        }

        protected static void Dump(BitArray data, string label)
        {
            var tmp = new byte[data.Length / 8];
            data.CopyTo(tmp, 0);
            Console.Out.WriteLine("---------" + label + "---------");
            foreach (var byt in tmp)
            {
                Console.Out.WriteLine(Convert.ToString(byt, 2).PadLeft(8, '0'));
            }
            Console.Out.WriteLine("---------/" + label + "---------");
        }
        #endregion
    }
}


//public IEnumerable<Color> ColorsFromBits(BitArray bitArray)
//{

//    byte[] bytes = new byte[bitArray.Length / 8];
//    bitArray.CopyTo(bytes, 0);

//    int bitDepth = 32;
//    int ptr = 0;
//    foreach (byte b in bytes)
//    {
//        byte[] accum = new byte[4];
//        // swap low end bits here
//        accum[ptr++] = b;
//        if (ptr == 4)
//        {
//            int int32 = BitConverter.ToInt32(accum, 0);
//            yield return Color.FromArgb(int32);
//        }
//        ptr = 0;
//    }
//}