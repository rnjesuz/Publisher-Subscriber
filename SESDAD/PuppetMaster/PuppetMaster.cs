using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.IO;

namespace SESDAD
{
    class PuppetMaster
    {
        static string input;
        static List<string> inputParsed;
        static string firstToken;
        static string thirdToken;
        static string processname;
        static string topicname;
        static int numberofevents;
        static int sleepInterval;
        static bool active = true;

        //HashTable with the the sites and it's parents <site,parent>
        static Dictionary<string, string> siteTree = new Dictionary<string, string>();

        //HashTable with the the brokers and it's URL. <brokers, URL>
        static internal Dictionary<string, string> brokerTable = new Dictionary<string, string>();

        //HashTable with the the publishers and it's URL. <publishers, URL>
        static internal Dictionary<string, string> publisherTable = new Dictionary<string, string>();

        //HashTable with the the subsribers and it's URL. <subsribers, URL>
        static internal Dictionary<string, string> subscriberTable = new Dictionary<string, string>();

        //Dictionary from ints to string. the int is the abstraction of the site number. the string is the URL of the site broker
        //By arquitecture rule which site has a broker and its only 1.
        static Dictionary<string, string> SiteToBroker = new Dictionary<string, string>();

        //boolean for the event routing .  0 = FLOODING, 1 = FILTER; Default is FLOODING
        static int eventRouting = 0;

        //boolean for the ordering . -1=NO, 0 = FIFO, 1 = TOTAL; Default is FIFO
        static int Ordering = 0;
        static int eventNumber = 0;

        //boolean for log level. 0 = LIGHT, 1 = FULL; Default is LIGHT logging
        static internal int Loglevel = 0;

        static void Main(string[] args)
        {
            //System.Diagnostics.Process.Start("https://www.youtube.com/watch?v=xnKhsTXoKCI");

            string directory = Directory.GetCurrentDirectory();
            File.WriteAllBytes(@"" + directory + "\\..\\..\\Log.txt", new byte[] { 0 });
   
            ReadConfigFile();

            TcpChannel channel = new TcpChannel(8069);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotePM), "puppetmaster", WellKnownObjectMode.Singleton);

            while (active)
            {
                //get user input
                input = Console.ReadLine();
                inputParsed = ParseInput(input);
                ExecuteCommand(inputParsed);
            }
           
        }

        //method that reads a designated file that has configuration instruction for the system
        //executes each line and creates a running everyronment of interacting servers
        private static void ReadConfigFile()
        {
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
                        Console.WriteLine("Site :" + parsedLine[1] + parsedLine[3]);
                        break;

                    case "Process":
                        switch (parsedLine[3])
                        {
                            case "broker":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    SiteToBroker.Add(parsedLine[5], parsedLine[7]);
                                    brokerTable.Add(parsedLine[1], parsedLine[7]);

                                    if (siteTree[parsedLine[5]].Equals("none"))
                                    {
                                        //new Broker(parsedLine[1], parsedLine[7]); //enviar o nome do processo e o URL em que ele tem de se ligar
                                        string[] args = new string[2] { parsedLine[1], parsedLine[7]};
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", args);
                                        Process.Start(startInfo);
                                    }
                                    else
                                    {
                                        // new Broker(parsedLine[1], parsedLine[7], SiteToBroker[siteTree[parsedLine[5]]]);
                                        string[] args = new string[3] { parsedLine[1], parsedLine[7], SiteToBroker[siteTree[parsedLine[5]]] };
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", args);
                                        Process.Start(startInfo);
                                    }
                                }
                                break;

                            case "publisher":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    publisherTable.Add(parsedLine[1], parsedLine[7]);
                                    //new Publisher(parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]]);
                                    string[] args = new string[3] { parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]] };
                                    ProcessStartInfo startInfo = new ProcessStartInfo(parsedLine[1]+".exe");
                                    startInfo.FileName = "publisher.exe";
                                    startInfo.Arguments = String.Join(" ", args);
                                    Process.Start(startInfo);
                                }
                                break;

                            case "subscriber":
                                if (parsedLine[0].Equals("Process") && parsedLine[2].Equals("Is") && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    subscriberTable.Add(parsedLine[1], parsedLine[7]);
                                    //new Subscriber(parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]]);
                                    string[] args = new string[3] { parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]] };
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.FileName = "subscriber.exe";
                                    startInfo.Arguments = String.Join(" ", args);
                                    Process.Start(startInfo);
                                }
                                break;
                        }
                        break;

                    case "LoggingLevel":
                        switch (parsedLine[1])
                        {
                            case "full":
                                Loglevel = 1;
                                break;

                            case "light":
                                Loglevel = 0;
                                break;
                        }
                        break;

                    case "RoutingPolicy":
                        switch (parsedLine[1])
                        {
                            case "flooding":
                                eventRouting = 0;
                                break;

                            case "filter":
                                eventRouting = 1;
                                break;
                        }
                        break;

                    case "Ordering":
                        switch (parsedLine[1])
                        {
                            case "NO":
                                Ordering = -1;
                                break;

                            case "FIFO":
                                Ordering = 0;
                                break;

                            case "TOTAL":
                                Ordering = 1;
                                break;
                        }
                        break;
                }
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

        //Reads the input command and executes the expected action
        //changes internal state of system classes
        //prints to console in case of error
        private static void ExecuteCommand(List<string> inputParsed)
        {
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
                case "Quit":
                    active = false;
                    break;
                default:
                    Console.WriteLine("Invalid Input");
                    break;
            }
        }
        
    }

    class RemotePM : MarshalByRefObject, PMInterface
    {
        private int eventnumber = 0;
        private string text;
        string directory = Directory.GetCurrentDirectory();

        //HashTable with the the brokers and it's URL. <brokers, URL>
        private Dictionary<string, string> brokerTable = PuppetMaster.brokerTable;
        //HashTable with the the publishers and it's URL. <publishers, URL>
        private Dictionary<string, string> publisherTable = PuppetMaster.publisherTable;
        //HashTable with the the subsribers and it's URL. <subsribers, URL>
        private Dictionary<string, string> subscriberTable = PuppetMaster.subscriberTable;


        /*
        write the received update into the Log file
        creates file is not existant. appends text if already existent
        every call to this update increments a counter ( eventnumber )
        @eventlabel the label for the event. BroEbent, SubEvent, PubEvent
        @p1 URL of the process that trigerred the event. Can be a broker, subscriber or publisher
        @p2 URL of the publisher that sent the publication
        @topicname topicname
        */
        public void UpdateEventLog(string eventlabel, string p1, string p2, string topicname)
        {
            string process1Name;
            string process2Name;

            Console.WriteLine("Received request for event Log");
            //p1 can be a subscriber, publisher or broker.
            //to generalize the method there's no way to know which one it is
            //we test all the tables to find the processname for the given URL (p1)
            if (eventlabel.Equals("BroEvent"))
            {
                process1Name = brokerTable.FirstOrDefault(x => x.Value.Contains(p1)).Key;
            }
            else if(eventlabel.Equals("PubEvent"))
            {
                process1Name = publisherTable.FirstOrDefault(x => x.Value.Contains(p1)).Key;
            }
            else
            {
                process1Name = subscriberTable.FirstOrDefault(x => x.Value.Contains(p1)).Key;
            }

            process2Name = publisherTable.FirstOrDefault(x => x.Value.Contains(p2)).Key;

            if(PuppetMaster.Loglevel == 1)
            {
                text = eventlabel + " " + process1Name + ", " + process2Name + ", " + topicname + ", " + eventnumber++;
            }

            if (PuppetMaster.Loglevel != 1 && (eventlabel.Equals("PubEvent") || eventlabel.Equals("SubEvent"))){
                text = eventlabel + " " + process1Name + ", " + process2Name + ", " + topicname + ", " + eventnumber++;
            }

            string path = @"" + directory + "\\..\\..\\Log.txt";
            
            Console.WriteLine("Starting Log of event: "+eventlabel);
            if (!File.Exists(path))
            {
                File.Create(path);
                TextWriter tw = new StreamWriter(path);
                tw.WriteLine(text);
                tw.Close();
            }
            else if (File.Exists(path))
            {
                TextWriter tw = new StreamWriter(path, true);
                tw.WriteLine(text);
                tw.Close();
            }
            Console.WriteLine("Ending Log of event: " + eventlabel);
        }
    }
}
