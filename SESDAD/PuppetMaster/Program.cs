using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            string input;
            List<string> inputParsed;
            input = Console.ReadLine();
            inputParsed = ParseInput(input);
            string first = inputParsed.First();
            switch (first)
            {
                case "Subscriber":
                    string processname = inputParsed.ElementAt(1);
                    string topicname = inputParsed.ElementAt(3);

                    break;
                case "Publisher":
                    break;
                case "Status":
                    break;
                default:
                    Console.WriteLine("Invalid Input");
                    break;
            }
        }

        private static List<string> ParseInput(string input)
        {
            List<string> parse = new List<string>();
            string[] parsedBySpace = input.Split(null);
            parse = parsedBySpace.OfType<string>().ToList();

            return parse;

        }


    }
}
