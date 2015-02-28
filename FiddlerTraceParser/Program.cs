using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiddlerTraceParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if(!ValidateArgument(args))
            {
                Console.WriteLine("arguments are not in correct format, use as per below:");
                Console.WriteLine("FiddlerTraceParser.exe baseurl secondurl fiddlertracefilepath");
                Console.WriteLine("Output will saved at same location where trace file was extracted");
            }
            ParseTrace(args);
        }

        static bool ValidateArgument(string[] args)
        {
            bool result = false;
            if(args.Length != 3)
            {
                return result;
            }
            else
            {
                // check that both the arguments are urls
                Uri baseUri, secondUri, folderPath;

                if (Uri.TryCreate(args[0], UriKind.Absolute, out baseUri) && Uri.TryCreate(args[1], UriKind.Absolute, out secondUri) && Uri.TryCreate(args[2], UriKind.RelativeOrAbsolute, out folderPath))
                {
                    result = true;
                    Console.WriteLine(" baseUri: {0} \n secondUri: {1} \n trace Dir: {2}", args[0], args[1], args[2]);
                }
            }
            return result;
        }

        static void ParseTrace(string[] args)
        {
            TraceParser tp = new TraceParser(args);
        }
    }
}
