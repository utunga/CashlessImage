using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CashlessImage
{

    /// <summary>
    /// Combines source image with source data and puts data
    /// into low order bits of target image 
    /// </summary>
    public class ImageMaker
    {
        private readonly Options _options;
        public Options Options
        {
            get { return _options; }
        }

        public ImageMaker(Options options)
        {
            _options = options;
        }

        public void Run()
        {
            Bitmap inputImage = new Bitmap(_options.ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;
            BitArray dataToInject = BitArrayFromFile(_options.DataInputFile);

            IEnumerable<int> pixels = PixelDataFromImage(inputImage);
            IEnumerable<int> injectedPixels = InjectData(pixels, dataToInject, width, height);

            var image = ImageFromPixels(injectedPixels, width, height);

            // save to disk
            image.Save(_options.OutputFile, ImageFormat.Png);

            var info = new FileInfo(_options.OutputFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        private IEnumerable<int> InjectData(
            IEnumerable<int> pixelsEnum,
            BitArray injectData,
            int width,
            int height)
        {
            var pixels = pixelsEnum.ToArray();
            var pixelData = new BitArray(pixels);

            var injectPtr = 0;
            var pixelPtr = 0;
            int pixelDataPtr = -1;

            while (pixelPtr<pixels.Length)
            {
                if (IsWriteableVisualArea(pixelPtr, width, height))
                {
                    if (pixelDataPtr==-1)
                        pixelDataPtr = pixelPtr*32;
                    
                    pixelData[pixelDataPtr] = injectData[injectPtr++];
                    pixelDataPtr = NextWriteableBit(pixelDataPtr);
                    pixelPtr = pixelDataPtr / 32;

                }
                else
                {
                    pixelDataPtr = -1;
                    pixelPtr++;
                }

                // if we run out of data to write just
                // write it again (for symmetry)
                if (injectPtr == injectData.Length)
                    injectPtr = 0;
            }

            var retVal = new int[pixelData.Length];
            pixelData.CopyTo(retVal, 0);
            return retVal;
        }

        // Use this to ensure we only write to a rectangle within the image
        public bool IsWriteableVisualArea(int pixelPtr, int width, int height)
        {
            var x = pixelPtr % width;
            var y = pixelPtr / width;
            bool xInRange = (x > (width / 2) && x < (4 * width / 5));
            bool yInRange = (y > (height / 2) && y < (4 * height / 5));
            return (xInRange && yInRange);
        }

        public int NextWriteableBit(int pixelPtr)
        {
            // number of low bits to write per pixel
            int bits_per = _options.BitsPerColor;

            int ptr = pixelPtr % 32;
            bool isAlpha = ptr >= 24;
            bool nextBitWritable =
                isAlpha ? ptr < 31
                : (ptr % 8) < (bits_per - 1);

            if (nextBitWritable)
            {
                // stay within the current component 
                return pixelPtr + 1;
            }
            else if (isAlpha) // && !nextBitWritable 
            {
                // stay within the current component
               
                return pixelPtr + 1;
            }
            else // !isAlpha && !nextBitWritable 
            {
                // jump to start of next component (rgba) within the color 
                return pixelPtr + (8 - (bits_per - 1));
            }
        }

        private Bitmap ImageFromPixels(IEnumerable<int> pixels, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var enumerator = pixels.GetEnumerator();
            bool pixelsLeft = true;
            int y = 0;
            while (pixelsLeft && y < bmp.Height)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color nextPixel;
                    if (enumerator.MoveNext())
                    {
                        nextPixel = Color.FromArgb(enumerator.Current);
                    }
                    else
                    {
                        pixelsLeft = false;
                        nextPixel = Color.Black;
                    }
                    bmp.SetPixel(x, y, nextPixel);
                }
                y++;
            }
            return bmp;
        }

        private Bitmap ResizeBitmap(Bitmap bmp, int desiredWidth, int desiredHeight)
        {
            var rect = new Rectangle(new Point(0, 0), new Size(desiredWidth, desiredHeight));
            return bmp.Clone(rect, bmp.PixelFormat);
        }

        public IEnumerable<int> PixelDataFromImage(Bitmap bmp)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
            {
                bmp = ARGBBitmapFromImage(bmp);
            }

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);
                    yield return pixel.ToArgb();
                }
            }
        }

        public Bitmap ARGBBitmapFromImage(Bitmap orig)
        {
            Bitmap clone = new Bitmap(orig.Width, orig.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }
            return clone;
        }

        public BitArray BitArrayFromFile(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return new BitArray(bytes);
            }
        }
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