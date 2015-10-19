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
            string firstToken;
            string thirdToken;
            string processname;
            string topicname;
            int numberofevents;
            int sleepInterval;

            //read all lines from the config file. split ea line into an array
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\Public\TestFolder\WriteLines2.txt");

            //get user input
            input = Console.ReadLine();
            inputParsed = ParseInput(input);
            firstToken = inputParsed.First();
            thirdToken = inputParsed.ElementAt(2);

            //verify user input and act if valid
            switch (firstToken)
            {
                case "Subscriber":
                    switch (thirdToken)
                    {
                        case "Subscribe":
                            processname = inputParsed.ElementAt(1);
                            topicname = inputParsed.ElementAt(3);
                            //TODO subscribe process to topic
                            break;
                        case "Unsubscribe":
                            processname = inputParsed.ElementAt(1);
                            topicname = inputParsed.ElementAt(3);
                            //TODO unsubscribe process from topic
                            break;
                    }
                    break;
                case "Publisher":
                    //check if input if of type: Publisher p Publish n Ontopic t Interval x
                    if (thirdToken.Equals("Publish") && inputParsed.ElementAt(4).Equals("Ontopic") && inputParsed.ElementAt(6).Equals("Interval"))
                    {
                        numberofevents = Int32.Parse(inputParsed.ElementAt(5));
                        sleepInterval = Int32.Parse(inputParsed.ElementAt(7));
                        //TODO do publishing event with needed protocol
                    }
                    break;
                case "Status":
                    //TODO print system status
                    break;
                case "Crash":
                    processname = inputParsed.ElementAt(1);
                    //TODO crash a node. Use SIGKILL??
                    break;
                case "Freeze":
                    processname = inputParsed.ElementAt(1);
                    //TODO make node sleep until awoken. child.sleep()?
                    break;
                case "Unfreeze":
                    processname = inputParsed.ElementAt(1);
                    //TODO make node wake up. child.snoze()?
                    break;
                case "Wait":
                    sleepInterval = Int32.Parse(inputParsed.ElementAt(1));
                    //TODO go sleep. how do u auto sleep?
                    break;
                default:
                    Console.WriteLine("Invalid Input");
                    break;
            }
        }

        //receive a string and parde it into several tokens
        //string are split by whitespace
        //return a list with all the tokens
        private static List<string> ParseInput(string input)
        {
            List<string> parse = new List<string>();
            string[] parsedBySpace = input.Split(null);
            parse = parsedBySpace.OfType<string>().ToList();

            return parse;

        }


    }
}
