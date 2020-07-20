using System;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CashlessImage
{

    /// <summary>
    /// Combines source image with source data and puts data
    /// into low order bits of target image 
    /// </summary>
    public class ImageMaker : BaseMaker
    {

        public string ImgInputFile { get; set; }

        public string DataFile { get; set; }

        public string ImgOutputFile { get; set; }

        public override void Run()
        {
            MergeDataIntoImage();
        }

        public void MergeDataIntoImage()
        {
            SKBitmap inputImage = LoadBmpFromFile(ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;
            BitArray dataToInject = BitArrayFromFile(DataFile);

            IEnumerable<int> pixels = PixelsFromBitmap(inputImage);
            IEnumerable<int> injectedPixels = InjectData(pixels, dataToInject, width, height);

            var bmp = BitmapFromPixels(injectedPixels, width, height);

            SaveBmpToPng(bmp, ImgOutputFile);

            var info = new FileInfo(ImgOutputFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public IEnumerable<int> InjectData(
            IEnumerable<int> pixelsEnum,
            BitArray data,
            int width,
            int height)
        {
            var pixels = pixelsEnum.ToArray();
            var pixelData = new BitArray(pixels);


            int pixelPtr;
            pixelData = WriteHeader(pixelData,
                dataLength: data.Length,
                bitsPer: BitsPer,
                out pixelPtr);

            int dataPtr = 0; 
            int pixelBitPtr = -1;
            while (pixelPtr < pixels.Length)
            {
                if (IsWritablePixel(pixelPtr, width, height))
                {
                    if (pixelBitPtr==-1)
                        pixelBitPtr = pixelPtr*32;
                    
                    pixelData[pixelBitPtr] = data[dataPtr++];
                    pixelBitPtr = NextWriteableBit(pixelBitPtr);
                    pixelPtr = pixelBitPtr / 32;
                }
                else
                {
                    pixelBitPtr = -1;
                    pixelPtr++;
                }

                // if we run out of data to write just
                // write it again (for visual symmetry)
                //if (injectPtr == injectData.Length)
                //    injectPtr = 0;

                // once we run out of data to write just stop
                if (dataPtr == data.Length)
                    break;
            }

            var retVal = new int[pixels.Length];
            pixelData.CopyTo(retVal, 0);
            //Dump(pixelData, "pixel data");
            return retVal;
        }

        public BitArray WriteHeader(BitArray pixelData, int dataLength, int bitsPer, out int pixelPtr)
        {
            BitArray header = new BitArray(new int[] { dataLength, bitsPer });
            //Dump(header, "Header to write");
            var dataPtr = 0;
            int pixelDataPtr = NextWriteableBitHeader(-1);
            while (dataPtr < HEADER_LENGTH)
            {
                pixelData[pixelDataPtr] = header[dataPtr++];
                pixelDataPtr = NextWriteableBitHeader(pixelDataPtr);
            }
            // finish off this pixel, then go to next pixel
            pixelPtr = (pixelDataPtr / 32) + 1 + 1;
            //Dump(pixelData, "Wrote header");
            return pixelData;
        }

        public SKBitmap BitmapFromPixels(IEnumerable<int> pixelData, int width, int height)
        {
            SKBitmap bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unknown);

            //var pixels = pixelData.ToArray();

            //var imageBuffer = new byte[pixels.Length * sizeof(int)];
            //Buffer.BlockCopy(pixels, 0, imageBuffer, 0, imageBuffer.Length);
            //var data = SKData.CreateCopy(imageBuffer);
            //bmp.InstallPixels(new SKImageInfo(bmp.Width, bmp.Height), data.Data);
            //return bmp;

            //var pixels = pixelData.Select(i => ((SKColor)(uint)i).WithAlpha(255)).ToArray();

            var enumerator = pixelData.GetEnumerator();
            bool pixelsLeft = true;
            int y = 0;
            while (pixelsLeft && y < bmp.Height)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    SKColor nextPixel;
                    if (enumerator.MoveNext())
                    {
                        nextPixel = (SKColor)(uint)enumerator.Current;
                        Debug.Assert((uint)nextPixel == enumerator.Current);
                    }
                    else
                    {
                        pixelsLeft = false;
                        nextPixel = SKColor.Empty;
                    }
                    Console.Out.WriteLine(nextPixel);
                    bmp.SetPixel(x, y, nextPixel.WithAlpha(255));
                    
                }
                y++;
            }
            return bmp;
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