using NUnit.Framework;
using CashlessImage;
using System.IO;
using System.Text;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Tga;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace CashlessImageTest
{
    public class ImageMakerTests
    {
        ImageMaker _imageMaker;
        DataMaker _dataMaker;

        [SetUp]
        public void Setup()
        {
            _imageMaker = new ImageMaker()
            {
                BitsPer = 2,
                ImgInputFile = "../../test_input.png",
                DataFile = "../../test_data.txt"
            };
            _dataMaker = new DataMaker()
            {
                BitsPer = 2,
                ImgInputFile = "../../test_input.png",
                DataFile = "../../test_data.txt"
            };
        }

        [Test]
        [Ignore("Skip writeable visual area logic for now")]
        public void TestWriteableVisualArea()
        {
            Assert.AreEqual(false, _imageMaker
                .IsWritablePixel(0, 100, 100));
            Assert.AreEqual(true, _imageMaker
                .IsWritablePixel(51 + 51 * 100, 100, 100));
            Assert.AreEqual(true, _imageMaker
                .IsWritablePixel(78 + 78 * 100, 100, 100));
            Assert.AreEqual(true, _imageMaker
                .IsWritablePixel(51 + 78 * 100, 100, 100));
            Assert.AreEqual(false, _imageMaker
                .IsWritablePixel(81 + 51 * 100, 100, 100));
            Assert.AreEqual(false, _imageMaker
                .IsWritablePixel(51 + 81 * 100, 100, 100));
            Assert.AreEqual(false, _imageMaker
                .IsWritablePixel(90 + 90 * 100, 100, 100));
        }

        [Test]
        public void RoundTripDataSimpleBits()
        {
            var srcData = new byte[] {1, 2, 3, 4};
            var data = new BitArray(srcData);
            var bmp = _FilledSquare(Color.White, 4, 4);
            var pixels = _imageMaker.PixelsFromImage(bmp);

            var blended = _imageMaker.InjectData(pixels, data, bmp.Width, bmp.Height);

            var resultData = _dataMaker.ExtractData(blended, bmp.Width, bmp.Height);

            Assert.AreEqual(srcData.Length, resultData.Length);
            for (int i=0;i<srcData.Length; i++)
            {
                Assert.AreEqual(srcData[i], resultData[i]);
            }
        }

        [Test]
        public void RoundTripDataUtf8String()
        {
            var srcData = "around the ragged rocks the ragged rascal ran";
            var stream = _StreamFromString(srcData);
            var data = _imageMaker.BitArrayFromStream(stream);

            var bmp = _FilledSquare(Color.White, 10, 10);
            var pixels = _imageMaker.PixelsFromImage(bmp);

            var blended = _imageMaker.InjectData(pixels, data, bmp.Width, bmp.Height);
          
            var resultData = _dataMaker.ExtractData(blended, bmp.Width, bmp.Height);

            Assert.AreEqual(srcData, Encoding.UTF8.GetString(resultData));
        }

        [Test]
        public void RoundTripHeader()
        {
            var pixels = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var pixelData = new BitArray(pixels);
            
            int pixelPtr;
            int dataLengthSrc = 1234;
            int bitsPerSrc = 2;
            pixelData = _imageMaker.WriteHeader(pixelData, 
                dataLengthSrc, bitsPerSrc, out pixelPtr);
           
            int dataLengthOut;
            int bitsPerOut;

            _dataMaker.ReadHeader(pixelData, out pixelPtr, out dataLengthOut, out bitsPerOut);
            Assert.AreEqual(BaseMaker.HEADER_LENGTH / 32 + 2, pixelPtr);
            Assert.AreEqual(dataLengthSrc, dataLengthOut);
            Assert.AreEqual(bitsPerSrc, bitsPerOut);
        }

        [Test]
        public void TestBitArrayFromStream()
        {
            var stream = _StreamFromString("abcdefghij");
            var bitArray = _imageMaker.BitArrayFromStream(stream);
            var bytes = new byte[2];
            bitArray.Length = 16;
            bitArray.CopyTo(bytes, 0);
            Assert.AreEqual("ab", Encoding.UTF8.GetString(bytes));
        }

        [Test]
        public void TestPixelDataFromImage()
        {
            // should get 100 pixels of pure white
            var pixels = _imageMaker.PixelsFromImage(_FilledSquare(Color.White, 10, 10));

            Assert.AreEqual(100, pixels.Count());
            foreach (var pixel in pixels)
            {
                //111111111111111111111 === all white
                Assert.AreEqual(
                    "11111111111111111111111111111111",
                    Convert.ToString(pixel, 2).PadLeft(32, '0'));
            }

            // should get 100 pixels of pure black
            pixels = _imageMaker.PixelsFromImage(_FilledSquare(Color.Black, 10, 10));

            Assert.AreEqual(100, pixels.Count());
            foreach (var pixel in pixels)
            {
                //all black == 000 except alpha channel which is all 111s
                Assert.AreEqual(
                    "11111111000000000000000000000000",
                    Convert.ToString(pixel,2).PadLeft(32, '0'));
            }


            // pixels with a starting value 0 to ensure we get uint
            var color = Color.Red;
            pixels = _imageMaker.PixelsFromImage(_FilledSquare(color, 10, 10));

            Assert.AreEqual(100, pixels.Count());
            foreach (var pixel in pixels)
            {
                //all black == 000 except alpha channel which is all 111s
                Assert.AreEqual(
                    "11111111000000000000000011111111",
                    Convert.ToString(pixel, 2).PadLeft(32, '0'));
            }
        }

        //[Test]
        //public void TestColorsInt()
        //{
        //    var color = Color.Parse("#F000FF").WithAlpha(14);
        //    var pixel = color.ToPixel<Rgba32>();
        //    // 14 == 00001110 == alpha channel, then r,g,b
        //    string expected = "00001110111100000000000011111111";
        //    Assert.AreEqual(expected, Convert.ToString(pixel., 2).PadLeft(32, '0'));
        //}

        [Test]
        public void RoundTripDataReadsBitsPerPixel()
        {
            var testData = new byte[] { 1, 2, 3, 4 };
            var dataToInject = new BitArray(testData);
            var bmp = _FilledSquare(Color.White, 10, 10);
            var whitePixels = _imageMaker.PixelsFromImage(bmp);

            var imageMaker = new ImageMaker()
            {
                BitsPer = 2
            };
            var dataMaker = new DataMaker();
          
            var injectedPixels = imageMaker.InjectData(whitePixels, dataToInject, 10, 10);

            byte[] extractedData = dataMaker.ExtractData(injectedPixels, 10, 10);

            Assert.AreEqual(testData, extractedData);
            //var result = Encoding.UTF8.GetString(extractedData);
        }

        [Test]
        public void RoundTripDataThroughBitmap()
        {
            var testPixels = new int[4];
            for (int i =0; i<4; i++)
            {
                testPixels[i] = i+64;
            }
            var bmp = _imageMaker.ImageFromPixels(testPixels, 2, 2);

            var resultPixels = _dataMaker.PixelsFromImage(bmp);
            Dump(testPixels, "testPixels");
            Dump(resultPixels, "resultPixels");
            //for (int i = 0; i < 1000; i++)
            //{
            //    testPixels[i] = i;
            //}
            Assert.AreEqual(testPixels, resultPixels.ToArray());
        }

        [Test]
        public void RoundTripDataThroughBitmapFiles()
        {
            var testPixels = new int[100];
            for (int i = 0; i < 100; i++)
            {
                testPixels[i] = i;
            }
            var testFilePath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

            var bmp = _imageMaker.ImageFromPixels(testPixels, 10, 10);
            _imageMaker.SaveImageToPng(bmp, testFilePath);

            var bmpResult = _dataMaker.LoadBmpFromFile(testFilePath);
            var resultPixels = _dataMaker.PixelsFromImage(bmpResult);
            Assert.AreEqual(testPixels, resultPixels);
        }

        [Test]
        public void RoundTripFiles()
        {
            var testData = @"{ claimData: ""0x00000000000000000000000000000000000000000000000006f05b59d3b200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005f0f1275000000000000000000000000000000000000000000000000000000005f1e54b5000000000000000000000000d90fc89e89e3e5b75256b5aa617f887c583b29a2000000000000000000000000c0c84e49b0d5a82e046914d9a93f9f64bdb41ca387bca611c58e3bc397f0994a4ea54a02d054c81f21f4901eb5788f1098b2a5dd000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001""}";
            var dataFilePath = Path.GetTempFileName();

            var injectedImagePath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

            File.WriteAllText(dataFilePath, testData, Encoding.UTF8);
            var imageMaker = new ImageMaker()
            {
                BitsPer = 2,
                ImgInputFile = "test_input.png",
                ImgOutputFile = injectedImagePath,
                DataFile = dataFilePath
            };
            imageMaker.Run();

            var dataFilePath2 = Path.GetTempFileName();
            var dataMaker = new DataMaker()
            {
                ImgInputFile = injectedImagePath,
                DataFile = dataFilePath2
            };
            dataMaker.Run();

            var resultData = File.ReadAllText(dataFilePath2, Encoding.UTF8);

            Assert.AreEqual(testData, resultData);
        }

        public void AssertNextWriteable(int from, int toExpected)
        {
            int toActual  = _imageMaker.NextWriteableBit(from);
            Console.Out.WriteLine(string.Format("{0}-{1} ({2})", from, toActual, toExpected));
            Assert.AreEqual(toExpected, toActual);
        }


        //[Test]
        //public void TestNextWritableBitsMSBFirst()
        //{
        //    AssertNextWriteable(0, 1);
        //    AssertNextWriteable(1, 2);
        //    AssertNextWriteable(2, 3);
        //    AssertNextWriteable(3, 4);
        //    AssertNextWriteable(4, 5);
        //    AssertNextWriteable(5, 6);
        //    AssertNextWriteable(6, 7);
        //    AssertNextWriteable(7, 14); //8+8-2
        //    AssertNextWriteable(14, 15);
        //    AssertNextWriteable(15, 22);//8+8+8-2
        //    AssertNextWriteable(22, 23);
        //    AssertNextWriteable(23, 30);//8+8+8+8-2
        //    AssertNextWriteable(30, 31);
        //    AssertNextWriteable(31, 32); //
        //}


        [Test]
        public void TestNextWritableBitsLSBFirst() // low order bits first
        {
            AssertNextWriteable(0, 1);
            AssertNextWriteable(1, 2);
            AssertNextWriteable(2, 3);
            AssertNextWriteable(3, 4);
            AssertNextWriteable(4, 5);
            AssertNextWriteable(5, 6);
            AssertNextWriteable(6, 7);
            AssertNextWriteable(7, 8);
            AssertNextWriteable(8, 9);
            AssertNextWriteable(9, 16);
            AssertNextWriteable(16, 17);
            AssertNextWriteable(17, 24);
            AssertNextWriteable(24, 25);
            AssertNextWriteable(25, 32);
        }

        #region helper methods

        private static MemoryStream _StreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private static Image<Rgba32> _FilledSquare(Color color, int width, int height)
        {
            return new Image<Rgba32>(width, height, color);
        }

        protected static void Dump(IEnumerable<int> data, string label)
        {
            Console.Out.WriteLine("---------" + label + "---------");
            foreach (var i in data)
            {
                Console.Out.WriteLine(Convert.ToString(i, 2).PadLeft(32, '0'));
            }
            Console.Out.WriteLine("---------/" + label + "---------");
        }

        #endregion
    }
}


