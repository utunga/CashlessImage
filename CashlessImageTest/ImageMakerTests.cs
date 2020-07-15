using NUnit.Framework;
using CashlessImage;
using System.IO;
using System.Text;
using System;
using System.Drawing;
using SkiaSharp;
using System.Linq;
using System.Collections;

namespace CashlessImageTest
{
    public class ImageMakerTests
    {
        private Options TestOptions
        {
            get {
                return new Options()
                {
                    ImgInputFile = "../../test_input.png",
                    DataInputFile = "../../test_data.txt",
                    BitsPerColor = 2
                };
            }
        }

        ImageMaker _target;

        [SetUp]
        public void Setup()
        {
            _target = new ImageMaker(TestOptions);
        }

        [Test]
        [Ignore("Skip writeable visual area logic for now")]
        public void TestWriteableVisualArea()
        {
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(0, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(51 + 51 * 100, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(78 + 78 * 100, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(51 + 78 * 100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(81 + 51 * 100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(51 + 81 * 100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(90 + 90 * 100, 100, 100));
        }

        [Test]
        public void RoundTripDataSimpleBits()
        {
            //var srcData = "a";  //around the ragged rocks the ragged rascal ran";
            //var stream = _StreamFromString(srcData);
            //var data = _target.BitArrayFromStream(stream);

            var srcData = new byte[] {1, 2, 3, 4};
            var data = new BitArray(srcData);
            var bmp = _FilledSquare(SKColors.White, 4, 4);
            var pixels = _target.PixelDataFromImage(bmp);

            var blended = _target.InjectData(pixels, data, bmp.Width, bmp.Height);
            //Console.Out.WriteLine("-- injected data --");
            //foreach (var pixel in blended)
            //{
            //    Console.Out.WriteLine(Convert.ToString(pixel, 2).PadLeft(32, '0'));
            //}

            var resultData = _target.ExtractData(blended, bmp.Width, bmp.Height);

            //Console.Out.WriteLine("-- extracted data --");
            //foreach (var byt in resultData)
            //{
            //    Console.Out.WriteLine(Convert.ToString(byt, 2).PadLeft(8, '0'));
            //}

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
            var data = _target.BitArrayFromStream(stream);

            var bmp = _FilledSquare(SKColors.White, 10, 10);
            var pixels = _target.PixelDataFromImage(bmp);

            var blended = _target.InjectData(pixels, data, bmp.Width, bmp.Height);
          
            var resultData = _target.ExtractData(blended, bmp.Width, bmp.Height);

            Assert.AreEqual(srcData, Encoding.UTF8.GetString(resultData));
        }

        [Test]
        public void RoundTripHeader()
        {
            var srcData = new int[] { 0, 0, 0, 0};
            var bitArray = new BitArray(srcData);

            bitArray = _target.AddHeader(bitArray);

            int length;
            int bitsPer;

            _target.ReadHeader(bitArray, out length, out bitsPer);

            Assert.AreEqual(4 * 32, length);
            Assert.AreEqual(TestOptions.BitsPerColor, bitsPer);
        }

        [Test]
        public void TestBitArrayFromStream()
        {
            var stream = _StreamFromString("abcdefghij");
            var bitArray = _target.BitArrayFromStream(stream);
            var bytes = new byte[2];
            bitArray.Length = 16;
            bitArray.CopyTo(bytes, 0);
            Assert.AreEqual("ab", Encoding.UTF8.GetString(bytes));
        }

        [Test]
        public void TestPixelDataFromImage()
        {
            // should get 100 pixels of pure white
            var pixels = _target.PixelDataFromImage(_FilledSquare(SKColors.White, 10, 10));

            Assert.AreEqual(100, pixels.Count());
            foreach (var pixel in pixels)
            {
                //111111111111111111111 === all white
                Assert.AreEqual(
                    "11111111111111111111111111111111",
                    Convert.ToString(pixel, 2));
            }

            // should get 100 pixels of pure black
            pixels = _target.PixelDataFromImage(_FilledSquare(SKColors.Black, 10, 10));

            Assert.AreEqual(100, pixels.Count());
            foreach (var pixel in pixels)
            {
                //all black == 000 except alpha channel which is all 111s
                Assert.AreEqual(
                    "11111111000000000000000000000000",
                    Convert.ToString(pixel,2));
            }
        }

        [Test]
        public void TestSKColorsInt()
        {
            var pixel = SKColor.Parse("#F000FF").WithAlpha(14);
            // 14 == 00001110 == alpha channel, then r,g,b
            string expected = "00001110111100000000000011111111";
            Assert.AreEqual(expected, Convert.ToString((int)(UInt32) pixel, 2).PadLeft(32, '0'));
        }

        public void AssertNextWriteable(int from, int toExpected)
        {
            int toActual  = _target.NextWriteableBit(from);
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

        private static SKBitmap _FilledSquare(SKColor color, int width, int height)
        {
            SKBitmap bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using (SKCanvas canvas = new SKCanvas(bmp))
            {
                SKRect rect = new SKRect(0, 0, width, height);
                canvas.DrawRect(rect, new SKPaint()
                {
                    Style = SKPaintStyle.Fill,
                    Color = color
                });
            }

            return bmp;
        }

        #endregion
    }
}


