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
                //.Default("nico.jpg")
                .Default("test_input.png")
                .Description("Input file name")
                .Required();

            parser.Configure(opt => opt.DataInputFile)
                .Name("d", "data_input")
                .Default("data.json")
                .Description("Data input file")
                .Required();

            parser.Configure(opt => opt.OutputFile)
                .Name("o", "output")
                //.Default("../../outputfile.png")
                .Default("../../test_outputdata.json")
                .Description("Output file name")
                .Required();

            parser.Configure(opt => opt.BitsPerColor)
                .Name("b", "bits")
                .Default(3)
                .Description("How many bits per color segment (leave 0 to only use Alpha)");

            parser.Configure(opt => opt.Direction)
               .Name("direction")
               .Default(ProcessingDirection.ToData)
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
            if (!File.Exists(options.ImgInputFile))
            {
                Console.Out.WriteLine("Couldn't find input file " + options.ImgInputFile);
                return null;
            }

            if (!File.Exists(options.DataInputFile))
            {
                Console.Out.WriteLine("Couldn't find data input file " + options.DataInputFile);
                return null;
            }
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

            var fileInfo = new FileInfo(options.OutputFile);
            if (fileInfo.Exists)
            {
                Console.Out.WriteLine("File " + fileInfo + " exists already");
                if (options.DeleteOutputFile)
                {
                    Console.Out.WriteLine("Deleting " + fileInfo + " because -d specified..");
                    fileInfo.Delete();
                    fileInfo = new FileInfo(options.OutputFile);
                }
                else
                {
                    return;
                }
            }

            var imageMaker = new ImageMaker(options);
            imageMaker.Run();
        }
    }
}