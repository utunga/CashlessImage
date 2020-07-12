
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

        public string DataInputFile { get; set; }

        public string OutputFile { get; set; }

        public bool DeleteOutputFile { get; set; }

        public ProcessingDirection Direction { get; set; } = ProcessingDirection.ToImage;

        public int BitsPerColor { get; set; } = 6;
    }
}