using System;
using System.Diagnostics;
using System.IO;

using MatthiWare.CommandLine;


// System.Drawing.GDIPlus gives errors on Mac
// until you do this...
// brew install mono-libgdiplus
//
// and then i also had to do this
//  sudo ln -s /usr/local/lib/libgdiplus.dylib libgdiplus.dylib
// which is lame!
//
// FIXME move to
//      cf https://www.hanselman.com/blog/HowDoYouUseSystemDrawingInNETCore.aspx


namespace CashlessImage
{
    class Program
    {
        private static Options Configure(string[] args)
        {
            var parser = new CommandLineParser<Options>();

            parser.Configure(opt => opt.ImgInputFile)
                .Name("i", "image_input")
                .Default("thanks_for_helping.png")
                .Description("Input file name")
                .Required();

            parser.Configure(opt => opt.DataFile)
                .Name("d", "data_input")
                .Default("data.json")
                .Description("Data input file")
                .Required();

            parser.Configure(opt => opt.ImgMergedDataFile)
                .Name("o", "output")
                .Default("../../merged_data.png")
                .Description("Output file name")
                .Required();

            parser.Configure(opt => opt.BitsPerColor)
                .Name("b", "bits")
                .Default(3)
                .Description("How many bits per color segment (leave 0 to only use Alpha)");

            parser.Configure(opt => opt.PixelRange)
                .Name("r", "range")
                .Default(new int[] {1100, 1160, 1900, 1280})
                .Description("Writeable pixels (range) minX,minY, maxX,maxY");

            parser.Configure(opt => opt.Direction)
               .Name("direction")
               .Default(ProcessingDirection.ToImage)
               .Description("To data, or to image == processing direction");

            parser.Configure(opt => opt.DeleteOutputFile)
                .Name("f", "force")
                .Default(true)
                .Description("Delete (and recreate) output file if it already exists");

            var result = parser.Parse(args);
            if (result.HasErrors)
            {
                foreach (var exception in result.Errors)
                    HandleParserException(exception);
            }

            var options = result.Result;
            return options;
        }

        private static void HandleParserException(Exception exception)
        {
            Console.WriteLine(exception.Message);
        }

        static void Main(string[] args)
        {
            // try to configure by command line
            var options = Configure(args);
            if (options == null)
            {
                return;
            }

            var targetFile = (options.Direction == ProcessingDirection.ToImage) ?
                 new FileInfo(options.ImgMergedDataFile) :
                 new FileInfo(options.DataFile);
                
            if (targetFile.Exists)
            {
                Console.Out.WriteLine("File " + targetFile + " exists already");
                if (options.DeleteOutputFile)
                {
                    Console.Out.WriteLine("Deleting " + targetFile + " because -d specified..");
                    targetFile.Delete();
                }
                else
                {
                    return;
                }
            }

            if (options.Direction == ProcessingDirection.ToImage)
            {
                if (!File.Exists(options.ImgInputFile))
                {
                    Console.Out.WriteLine("Couldn't find input file " + options.ImgInputFile);
                    return;
                }

                if (!File.Exists(options.DataFile))
                {
                    Console.Out.WriteLine("Couldn't find data input file " + options.DataFile);
                    return;
                }

                var imageMaker = new ImageMaker()
                {
                    ImgInputFile = options.ImgInputFile,
                    ImgOutputFile = options.ImgMergedDataFile,
                    DataFile = options.DataFile
                };

                imageMaker.Header.BitsPerPixel = options.BitsPerColor;
                imageMaker.Header.SetWritableRange(
                    minX: options.PixelRange[0],
                    minY: options.PixelRange[1],
                    maxX: options.PixelRange[2],
                    maxY: options.PixelRange[3]);
                imageMaker.Run();
            }
            else if (options.Direction == ProcessingDirection.ToData)
            {
                if (!File.Exists(options.ImgMergedDataFile))
                {
                    Console.Out.WriteLine("Couldn't find merged image data file " + options.ImgMergedDataFile);
                    return;
                }

                var dataMaker = new DataMaker()
                {
                    ImgInputFile = options.ImgMergedDataFile,
                    DataFile = options.DataFile
                };
                dataMaker.Header.BitsPerPixel = options.BitsPerColor;
                dataMaker.Run();
            }
     
        }
    }
}