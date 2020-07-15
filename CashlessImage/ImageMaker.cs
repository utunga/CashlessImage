using System;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
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
            if (_options.Direction == ProcessingDirection.ToImage)
            {
                MergeDataIntoImage();
            }
            else
            {
                ExtractDataFromImage();
            }
        }

        public void MergeDataIntoImage()
        {
            SKBitmap inputImage = LoadBmpFromFile(_options.ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;
            BitArray dataToInject = BitArrayFromFile(_options.DataInputFile);

            IEnumerable<int> pixels = PixelDataFromImage(inputImage);
            IEnumerable<int> injectedPixels = InjectData(pixels, dataToInject, width, height);

            var bmp = BitmapFromPixels(injectedPixels, width, height);

            SaveBmpToPng(bmp, _options.OutputFile);

            var info = new FileInfo(_options.OutputFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public void ExtractDataFromImage()
        {
            SKBitmap inputImage = LoadBmpFromFile(_options.ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;

            IEnumerable<int> pixels = PixelDataFromImage(inputImage);
            byte[] extractedData = ExtractData(pixels, width, height);

            using (var fs = new FileStream(_options.OutputFile, FileMode.Create, FileAccess.Write))
            {
                fs.Write(extractedData, 0, extractedData.Length);
            }

            var info = new FileInfo(_options.OutputFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public IEnumerable<int> InjectData(
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

            //Dump(injectData, "before header");

            injectData = AddHeader(injectData);

            //Dump(injectData, "after header");

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
                // write it again (for visual symmetry)
                if (injectPtr == injectData.Length)
                    injectPtr = 0;

                // once we run out of data to write just stop
                //if (injectPtr == injectData.Length)
                //    break;
            }

            var retVal = new int[pixels.Length];
            pixelData.CopyTo(retVal, 0);

            //Dump(pixelData, "pixel data");
            return retVal;
        }

        private void Dump(BitArray data, string label)
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

        public BitArray AddHeader(BitArray injectData)
        {
            // FIXME check that length is not longer than available data
            int dataLength = injectData.Length;
            int bitsPer = _options.BitsPerColor;
            BitArray header = new BitArray(new int[] { dataLength, bitsPer });

            // this sure looks inefficient, but bitArray's can't copyTo()
            // into another bitArray so bounce it through this array of bits
            byte[] bits = new byte[(header.Length + injectData.Length)/8];
            header.CopyTo(bits, 0);
            injectData.CopyTo(bits, header.Length/8);
            return new BitArray(bits);
        }

        public void ReadHeader(BitArray data, out int dataLength, out int bitsPerColor)
        {
            // kinda inefficient to allocate an array of full length
            // only to use the first to values but so be it
            var vals = new UInt32[data.Length/32];
            Dump(data, "for");
            data.CopyTo(vals, 0);
            dataLength = (int) vals[0];
            bitsPerColor = (int) vals[1];
        }

        public byte[] ExtractData(
            IEnumerable<int> pixels,
            int width,
            int height)
        {
            var pixelsArr = pixels.ToArray();
            var pixelData = new BitArray(pixelsArr);
            BitArray extractData = null;
            var headerData = new BitArray(32 * 2);
            var extractPtr = 0;
            var pixelPtr = 0;
            int pixelDataPtr = -1;

            int dataLength = int.MaxValue;
            int bitsPerColor;

            while (pixelPtr < pixelsArr.Length && extractPtr < dataLength)
            {
                if (IsWriteableVisualArea(pixelPtr, width, height))
                {
                    if (pixelDataPtr == -1)
                        pixelDataPtr = pixelPtr * 32;

                    if (extractData == null)
                    {
                        headerData[extractPtr++] = pixelData[pixelDataPtr];

                        if (extractPtr == 64)
                        {
                            ReadHeader(headerData, out dataLength, out bitsPerColor);
                            extractData = new BitArray(dataLength);
                            extractPtr = 0;
                            //Dump(headerData, "header");
                        }
                    }
                    else
                    {
                        extractData[extractPtr++] = pixelData[pixelDataPtr];
                    }

                    pixelDataPtr = NextWriteableBit(pixelDataPtr);
                    pixelPtr = pixelDataPtr / 32;
                }
                else
                {
                    pixelDataPtr = -1;
                    pixelPtr++;
                }
            }

            var retVal = new byte[extractData.Length / 8];
            extractData.CopyTo(retVal, 0);
            return retVal;
        }

        // Use this to ensure we only write to a rectangle within the image
        public bool IsWriteableVisualArea(int pixelPtr, int width, int height)
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

        public int NextWriteableMSBFirst(int pixelDataPtr)
        {
            // number of low bits to write per pixel
            int bits_per = _options.BitsPerColor;

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

        public int NextWriteableLSBFirst(int pixelDataPtr)
        {
            // number of low bits to write per pixel
            int bits_per = _options.BitsPerColor;

            int ptr = pixelDataPtr % 32;
            bool isAlpha = ptr < 8;
            bool isEndOfComponent = (ptr + 1) % 8 == 0;
            bool isEndOfColor = (ptr + 1) % 32 == 0;

            bool stillWithinLOB = (ptr +1) % 8 < bits_per;

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

        public int NextWriteableBit(int pixelDataPtr)
        {
           return NextWriteableLSBFirst(pixelDataPtr);
        }

        public SKBitmap BitmapFromPixels(IEnumerable<int> pixels, int width, int height)
        {
            SKBitmap bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            var enumerator = pixels.GetEnumerator();
            bool pixelsLeft = true;
            int y = 0;
            while (pixelsLeft && y < bmp.Height)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    SKColor nextPixel;
                    if (enumerator.MoveNext())
                    {
                        nextPixel = (SKColor) (UInt32) enumerator.Current;
                    }
                    else
                    {
                        pixelsLeft = false;
                        nextPixel = SKColor.Empty;
                    }
                    bmp.SetPixel(x, y, nextPixel);
                }
                y++;
            }
            return bmp;
        }

        public IEnumerable<int> PixelDataFromImage(SKBitmap bmp)
        {
            if (bmp.Info.ColorType != SKColorType.Bgra8888
                || bmp.Info.AlphaType != SKAlphaType.Unpremul)
            {
                bmp = ARGBBitmapFromImage(bmp);
            }

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x, y);
                    yield return (int) ((UInt32) pixel);
                }
            }
        }

        public SKBitmap ARGBBitmapFromImage(SKBitmap orig)
        {
            return orig;
            //FIXME convert format 
        }

        public BitArray BitArrayFromFile(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                return BitArrayFromStream(stream);
            }
        }

        public BitArray BitArrayFromStream(Stream stream)
        {   
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return new BitArray(bytes);
        }

        public void SaveBmpToPng(SKBitmap bmp, string outputFileName)
        {
            // create an image and then get the PNG (or any other) encoded data
            using (var image = SKImage.FromBitmap(bmp))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                // save the data to a stream
                using (var stream = File.OpenWrite(outputFileName))
                {
                    data.SaveTo(stream);
                }
            }
        }

        public SKBitmap LoadBmpFromFile(string inputFilePath)
        {
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return SKBitmap.Decode(fs);
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