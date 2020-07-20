using System;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
using System.IO;
using System.Linq;

namespace CashlessImage
{
    public class DataMaker : BaseMaker
    {
        public override void Run()
        {
            ExtractDataFromImage();
        }

        public string ImgInputFile { get; set; }

        public string DataFile { get; set; }


        public void ExtractDataFromImage()
        {
            SKBitmap inputImage = LoadBmpFromFile(ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;

            IEnumerable<int> pixels = PixelsFromBitmap(inputImage);
            byte[] extractedData = ExtractData(pixels, width, height);

            using (var fs = new FileStream(DataFile, FileMode.Create, FileAccess.Write))
            {
                fs.Write(extractedData, 0, extractedData.Length);
            }

            var info = new FileInfo(DataFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public byte[] ExtractData(
            IEnumerable<int> pixels,
            int width,
            int height)
        {
            var pixelsArr = pixels.ToArray();
            var pixelData = new BitArray(pixelsArr);
            
            
            int bitsPerColor;
            int dataLength;
            int pixelPtr;
            ReadHeader(pixelData, out pixelPtr, out dataLength, out bitsPerColor);
            BitsPer = bitsPerColor;
            //pixelPtr = 4;
            
            BitArray data = new BitArray(dataLength);
            //Dump(pixelData, "pixel data to extract");

            var dataPtr = 0;
            int pixelBitPtr = -1;
            while (pixelPtr < pixelsArr.Length && dataPtr < dataLength)
            {
                if (IsWritablePixel(pixelPtr, width, height))
                {
                    if (pixelBitPtr == -1)
                        pixelBitPtr = pixelPtr * 32;

                    data[dataPtr++] = pixelData[pixelBitPtr];    
                    pixelBitPtr = NextWriteableBit(pixelBitPtr);
                    pixelPtr = pixelBitPtr / 32;
                }
                else
                {
                    pixelBitPtr = -1;
                    pixelPtr++;
                }
            }

            var retVal = new byte[data.Length / 8];
            data.CopyTo(retVal, 0);
            Dump(data, "data extracted");
            return retVal;
        }

        public void ReadHeader(BitArray pixelData, 
            out int pixelPtr, out int dataLength, out int bitsPerColor)
        {
            var dataPtr = 0;
            int pixelDataPtr = NextWriteableBitHeader(-1);
            //Dump(pixelData, "read data");
            BitArray data = new BitArray(HEADER_LENGTH);
            while (dataPtr < HEADER_LENGTH)
            {
                data[dataPtr++] = pixelData[pixelDataPtr];
                pixelDataPtr = NextWriteableBitHeader(pixelDataPtr);
            }
            // finish off this pixel, then go to next pixel
            pixelPtr = (pixelDataPtr / 32) + 1 + 1;
            Dump(data, "Read header");
            var vals = new int[data.Length / 32];
            data.CopyTo(vals, 0);
            dataLength = (int)vals[0];
            bitsPerColor = (int)vals[1];
        }

    }
}
