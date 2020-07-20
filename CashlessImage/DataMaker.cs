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
            Header = ReadHeader(pixelData, out pixelPtr);
            BitArray data = new BitArray(Header.DataLength);
            
            var dataPtr = 0;
            int pixelBitPtr = -1;
            while (pixelPtr < pixels.Length && dataPtr < data.Length)
            {
                if (IsWritablePixel(Header, pixelPtr, width, height))
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
            return retVal;
        }

        public HeaderStruct ReadHeader(BitArray pixelData, out int pixelPtr)
        {
            var dataPtr = 0;
            int pixelDataPtr = NextWritableBitHeader(-1);
            BitArray data = new BitArray(HeaderStruct.BitSize);
            while (dataPtr < data.Length)
            {
                data[dataPtr++] = pixelData[pixelDataPtr];
                pixelDataPtr = NextWritableBitHeader(pixelDataPtr);
            }
            // finish off this pixel, then go to next pixel
            pixelPtr = (pixelDataPtr / 32) + 1 + 1;
            var bytes = new byte[HeaderStruct.BitSize / 8];
            data.CopyTo(bytes, 0);
            return HeaderStruct.FromBytes(bytes);
        }
    }
}
