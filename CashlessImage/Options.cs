
namespace CashlessImage
{
    public enum ProcessingDirection
    {
        ToImage,
        ToData
    }

    public class Options
    {

        public string ImgInputFile { get; set; }

        public string DataFile { get; set; }

        public string ImgMergedDataFile { get; set; }

        public bool DeleteOutputFile { get; set; }

        public ProcessingDirection Direction { get; set; } = ProcessingDirection.ToImage;

        public int BitsPerColor { get; set; }

        public int[] PixelRange { get; set; }

    }
}