using System;
using System.Collections;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

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
            Image<Rgba32> inputImage = LoadBmpFromFile(ImgInputFile);
            int width = inputImage.Width;
            int height = inputImage.Height;
            BitArray dataToInject = BitArrayFromFile(DataFile);

            int[] pixelData = PixelsFromImage(inputImage);

            // check we have a (roughly) big enough target image to contain the data
            int dataSizeInPixels = dataToInject.Length / 32;
            double fillRatio = .4; // this is a approximate guess
            if (dataSizeInPixels > pixelData.Length * fillRatio)
            {
                throw new ApplicationException(
                    string.Format("{0} is not a big enough image to contain data from {1}",
                    ImgInputFile, DataFile));
            }

            int[] injectedData = InjectData(pixelData, dataToInject, width, height);

            var image = ImageFromPixels(injectedData, width, height);

            SaveImageToPng(image, ImgOutputFile);

            var info = new FileInfo(ImgOutputFile);
            Console.Out.WriteLine("Wrote file to " + info.FullName);
        }

        public int[] InjectData(
            int[] pixels,
            BitArray data,
            int width,
            int height)
        {
            var pixelData = new BitArray(pixels);
            Header.DataLength = data.Length;
           
            int pixelPtr;
            pixelData = WriteHeader(pixelData, Header, out pixelPtr);

            int dataPtr = 0; 
            int pixelBitPtr = -1;
            while (pixelPtr < pixels.Length)
            {
                if (IsWritablePixel(Header, pixelPtr, width, height))
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

                // if we run out of data to write, just
                // repeat it (for visual symmetry)
                if (dataPtr == data.Length)
                    dataPtr = 0;

                // once we run out of data to write just stop
                //if (dataPtr == data.Length)
                //    break;
            }

            var retVal = new int[pixels.Length];
            pixelData.CopyTo(retVal, 0);
            return retVal;
        }

        public BitArray WriteHeader(BitArray pixelData, HeaderStruct header, out int pixelPtr)
        {
            var bytes = header.ToBytes();
            BitArray headerBits = new BitArray(bytes);

            var dataPtr = 0;
            int pixelDataPtr = NextWritableBitHeader(-1);
            while (dataPtr < headerBits.Length)
            {
                pixelData[pixelDataPtr] = headerBits[dataPtr++];
                pixelDataPtr = NextWritableBitHeader(pixelDataPtr);
            }
            // finish off this pixel, then go to next pixel
            pixelPtr = (pixelDataPtr / 32) + 1 + 1;
            //Dump(pixelData, "Wrote header");
            return pixelData;
        }

        public Image<Rgba32> ImageFromPixels(int[] pixels, int width, int height)
        {
            Image<Rgba32> img = new Image<Rgba32>(width, height);
            for (int y = 0; y < img.Height; y++)
            {
                Span<Rgba32> pixelRowSpan = img.GetPixelRowSpan(y);
                for (int x = 0; x < img.Width; x++)
                {
                    var intPixel = pixels[y*img.Width + x];
                    pixelRowSpan[x] = new Rgba32((uint)intPixel);
                }
            }
            return img;
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

        public void SaveImageToPng(Image<Rgba32> image, string outputFileName)
        {
            image.Save(outputFileName);
        }
    }
}