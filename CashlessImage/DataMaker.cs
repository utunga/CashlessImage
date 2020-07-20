using System;
using System.Collections;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

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
            Image<Rgba32> inputImage = LoadBmpFromFile(ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;

            int[] pixels = PixelsFromImage(inputImage);
            byte[] extractedData = ExtractData(pixels, width, height);

            using (var fs = new FileStream(DataFile, FileMode.Create, FileAccess.Write))
            {
                fs.Write(extractedData, 0, extractedData.Length);
            }

            var info = new FileInfo(DataFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public byte[] ExtractData(
            int[] pixels,
            int width,
            int height)
        {
            var pixelData = new BitArray(pixels);

            int pixelPtr;
            HeaderStruct header = 
                ReadHeader(pixelData, out pixelPtr);
            BitsPer = header.BitsPerPixel;
            int dataLength = header.DataLength;
            BitArray data = new BitArray(dataLength);
            
            var dataPtr = 0;
            int pixelBitPtr = -1;
            while (pixelPtr < pixels.Length && dataPtr < dataLength)
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

        public HeaderStruct ReadHeader(BitArray pixelData, out int pixelPtr)
        {
            var dataPtr = 0;
            int pixelDataPtr = NextWriteableBitHeader(-1);
            BitArray data = new BitArray(HEADER_LENGTH);
            while (dataPtr < HEADER_LENGTH)
            {
                data[dataPtr++] = pixelData[pixelDataPtr];
                pixelDataPtr = NextWriteableBitHeader(pixelDataPtr);
            }
            // finish off this pixel, then go to next pixel
            pixelPtr = (pixelDataPtr / 32) + 1 + 1;
            Dump(data, "Read header");
            var bytes = new byte[data.Length / 8];
            data.CopyTo(bytes, 0);
            return HeaderStruct.FromBytes(bytes);
        }
    }
}
