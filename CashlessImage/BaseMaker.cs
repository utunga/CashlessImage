using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Tga;
using System.Runtime.InteropServices;

namespace CashlessImage
{
    public abstract class BaseMaker
    {
        public abstract void Run();

        public HeaderStruct Header = new HeaderStruct();

        public int NextWriteableBit(int pixelDataPtr)
        {
            return NextWriteableLSBFirst(pixelDataPtr);
        }

        /// <summary>
        /// when writing header, use all 8 bits but leave alpha alone 
        /// </summary>
        public int NextWritableBitHeader(int pixelDataPtr)
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
            int bits_per = Header.BitsPerPixel;

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
            int bits_per = Header.BitsPerPixel;

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
        public bool IsWritablePixel(HeaderStruct header, int pixelPtr, int width, int height)
        {
            var x = pixelPtr % width;
            var y = pixelPtr / width;
            int minX = header.MinX;
            int maxX = header.MaxX;
            int minY = header.MinY;
            int maxY = header.MaxY;

            if (minX == maxX && minY == maxY)
                return true; // if not set, dont constrain at all

            bool xInRange = (minX <= x) && (x <= maxX);
            bool yInRange = (minY <= y) && (y <= maxY);
            return (xInRange && yInRange);
        }

        #region helper methods 

        public Image<Rgba32> LoadBmpFromFile(string inputFilePath)
        {
            return Image.Load<Rgba32>(inputFilePath);
        }

        public int[] PixelsFromImage(Image<Rgba32> image)
        {
            //if (image.TryGetSinglePixelSpan(out var pixelSpan))
            //{
            //    var bytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();
            //    int[] result = new int[bytes.Length / 4];
            //    Buffer.BlockCopy(bytes, 0, result, 0, result.Length);
            //    return result;
            //}
            //throw new ApplicationException("Image too big");
            int[] result = new int[image.Width * image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = pixelRowSpan[x];
                    int index = image.Height * y + x;
                    result[index] = (int)pixel.Rgba;
                }
            }
            return result;
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