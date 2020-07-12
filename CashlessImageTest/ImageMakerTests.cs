using NUnit.Framework;
using CashlessImage;

namespace CashlessImageTest
{
    public class ImageMakerTests
    {
        private Options TestOptions()
        {
            return new Options()
            {
                ImgInputFile = "../../test_input.png",
                DataInputFile = "../../test_data.txt",
                BitsPerColor = 2
            };
        }

        ImageMaker _target;

        [SetUp]
        public void Setup()
        {
            _target = new ImageMaker(TestOptions());
        }

        [Test]
        public void NextWritableTest()
        {
            Assert.AreEqual(1, _target.NextWriteableBit(0));
            Assert.AreEqual(8, _target.NextWriteableBit(1));
            Assert.AreEqual(9, _target.NextWriteableBit(8));
            Assert.AreEqual(16, _target.NextWriteableBit(9));
            Assert.AreEqual(17, _target.NextWriteableBit(16));
            Assert.AreEqual(24, _target.NextWriteableBit(17));
            Assert.AreEqual(25, _target.NextWriteableBit(24));
            Assert.AreEqual(26, _target.NextWriteableBit(25));
            Assert.AreEqual(27, _target.NextWriteableBit(26));
            Assert.AreEqual(28, _target.NextWriteableBit(27));
            Assert.AreEqual(29, _target.NextWriteableBit(28));
            Assert.AreEqual(30, _target.NextWriteableBit(29));
            Assert.AreEqual(31, _target.NextWriteableBit(30));
            Assert.AreEqual(32, _target.NextWriteableBit(31));

        }

        [Test]
        public void TestWriteableVisualArea()
        {
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(0, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(51 + 51*100, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(78 + 78*100, 100, 100));
            Assert.AreEqual(true, _target
                .IsWriteableVisualArea(51 + 78 * 100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(81 + 51*100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(51 + 81*100, 100, 100));
            Assert.AreEqual(false, _target
                .IsWriteableVisualArea(90 + 90*100, 100, 100));
        }

    }
}


