using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace SESDAD
{
    class PuppetMaster
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

            //HashTable withe the sites and it's parents <site,parent>
            Dictionary<string, string> siteTree = new Dictionary<string, string>();

            //HashTable withe the brokers and it's URL. <brokers, URL>
            Dictionary<string, string> brokerTable = new Dictionary<string, string>();

            //HashTable withe the publishers and it's URL. <publishers, URL>
            Dictionary<string, string> publisherTable = new Dictionary<string, string>();

            //HashTable withe the subsribers and it's URL. <subsribers, URL>
            Dictionary<string, string> subscriberTable = new Dictionary<string, string>();

            //boolean for log level. 0 = LIGHT, 1 = FULL; Default is LIGHT logging
            int Loglevel = 0;
            int eventNumber = 0;

            //read all lines from the config file. split ea line into an array
            //string[] lines = System.IO.File.ReadAllLines(@"C:\Users\Public\TestFolder\WriteLines2.txt");
            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"ConfigFile.txt");
            string[] lines = System.IO.File.ReadAllLines(configPath);
            string[] parsedLine; //Line from config file that has the site information
            foreach (string line in lines)
            {
                parsedLine = line.Split(null);
                switch (parsedLine[0])
                {
                    case "Site":
                        siteTree.Add(parsedLine[1], parsedLine[3]); //adds site to the hastable(tree of sites)
                        break;
                    case "Process":
                        switch (parsedLine[3])
                        {
                            case "broker":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    brokerTable.Add(parsedLine[1], parsedLine[7]);
                                   // new Broker(parsedLine[1], parsedLine[7]); //enviar o nome do processo e o URL em que ele tem de se ligar
                                }
                                break;
                            case "publisher":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    publisherTable.Add(parsedLine[1], parsedLine[7]);
                                    // new Publisher(parsedLine[1]);
                                }
                                break;
                            case "subscriber":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    subscriberTable.Add(parsedLine[1], parsedLine[7]);
                                    // new Subscriber(parsedLine[1]);
                                }
                                break;
                        }      
                       break;
                }
            }

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
