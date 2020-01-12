﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;

namespace SESDAD
{
    class PuppetMaster
    {
        static string input;
        static List<string> inputParsed;
        static string firstToken;
        static string processname;
        static string topicname;
        static int numberofevents;
        static int sleepInterval;
        static bool active = true;

        //HashTable with the the sites and it's parents <site,parent>
        static internal Dictionary<string, string> siteTree = new Dictionary<string, string>();

        //HashTable with the the brokers and it's URL. <brokers, URL>
        static internal Dictionary<string, string> brokerTable = new Dictionary<string, string>();

        //HashTable with the the publishers and it's URL. <publishers, URL>
        static internal Dictionary<string, string> publisherTable = new Dictionary<string, string>();

        //HashTable with the the subsribers and it's URL. <subsribers, URL>
        static internal Dictionary<string, string> subscriberTable = new Dictionary<string, string>();

        //Dictionary from string to string. the 1st string sitename. the 2nd string is the URL of the site's broker
        //By arquitecture rule which site has a broker and its only 1.
        static internal Dictionary<string, string> SiteToBroker = new Dictionary<string, string>();

        //boolean for the event routing .  0 = FLOODING, 1 = FILTER; Default is FLOODING
        static int eventRouting = 0;

        //boolean for the ordering . -1 = NO, 0 = FIFO, 1 = TOTAL; Default is FIFO
        static int Ordering = 0;

        //boolean for log level. 0 = LIGHT, 1 = FULL; Default is LIGHT logging
        static internal int Loglevel = 0;

        internal static string directory = Directory.GetCurrentDirectory();

        static void Main(string[] args)
        {

            File.WriteAllText(@"" + Directory.GetCurrentDirectory() + "\\..\\..\\Ticket.txt", "0");

            File.WriteAllBytes(@"" + directory + "\\..\\..\\Log.txt", new byte[] { 0 });
   
            ReadConfigFile();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "BrokerTicket.exe";
            Process.Start(startInfo);

            TcpChannel channel = new TcpChannel(8069);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotePM), "puppetmaster", WellKnownObjectMode.Singleton);

            //boolean to read from a textfile. false = no script, true = with script
            bool readCommands = false;


        
            Console.WriteLine("Do you wish to read a Commands File?");
            Console.WriteLine("Type \"YES\" to read, or anything else not to");
            string readfile = Console.ReadLine();
            if (readfile.Equals("YES"))
                readCommands = true;

            if (readCommands)
            {
                Console.WriteLine("Please wait while we read commands from file");
                string commandPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"" + directory + "\\..\\..\\Commands.txt");
                string[] commands = System.IO.File.ReadAllLines(commandPath);
                string[] parsedCommands; //Line from config file that has the site information
                foreach (string command in commands) {
                    parsedCommands = command.Split(null);
                    ExecuteCommand(parsedCommands.ToList());
                }
            }

            Console.WriteLine("Input the Command you which to execute!");
            Console.WriteLine("Type \" Help \" for the list of available commands");

            //Cycle to read input from console
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
            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"" + directory + "\\..\\..\\ConfigFile.txt");
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
                                if (parsedLine[0].Equals("Process") && (parsedLine[2].Equals("Is") | parsedLine[2].Equals("is")) && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    SiteToBroker.Add(parsedLine[5], parsedLine[7]);
                                    brokerTable.Add(parsedLine[1], parsedLine[7]);

                                    if (siteTree[parsedLine[5]].Equals("none"))
                                    {
                                        //last argument: 0/1/2 indicates type of broker instance. 0 for leader, 1/2 for replica
                                        string[] args = new string[5] { parsedLine[1], parsedLine[7], eventRouting.ToString(), "0", Ordering.ToString() };
                                        string[] argsReplica1 = new string[5] { parsedLine[1] + "-1", parsedLine[7], eventRouting.ToString(), "1", Ordering.ToString() };
                                        string[] argsReplica2 = new string[5] { parsedLine[1] + "-2", parsedLine[7], eventRouting.ToString(), "2", Ordering.ToString() };


                                        //create leader
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", args);
                                        Process.Start(startInfo);

                                        //create first replica
                                        startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", argsReplica1);
                                        Process.Start(startInfo);

                                        //create second replica
                                        startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", argsReplica2);
                                        Process.Start(startInfo);
                                    }
                                    else
                                    {
                                        //last argument: 0/1 indicates type of broker instance. 0 for leader, 1 for replica
                                        string[] args = new string[6] { parsedLine[1], parsedLine[7], SiteToBroker[siteTree[parsedLine[5]]], eventRouting.ToString(), "0", Ordering.ToString() };
                                        string[] argsReplica1 = new string[6] { parsedLine[1]+"-1", parsedLine[7], SiteToBroker[siteTree[parsedLine[5]]], eventRouting.ToString(), "1", Ordering.ToString() };
                                        string[] argsReplica2 = new string[6] { parsedLine[1]+"-2", parsedLine[7], SiteToBroker[siteTree[parsedLine[5]]], eventRouting.ToString(), "2", Ordering.ToString() };


                                        //create leader
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", args);
                                        Process.Start(startInfo);

                                        //create first replica
                                        startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", argsReplica1);
                                        Process.Start(startInfo);

                                        //create second replica
                                        startInfo = new ProcessStartInfo();
                                        startInfo.FileName = "broker.exe";
                                        startInfo.Arguments = String.Join(" ", argsReplica2);
                                        Process.Start(startInfo);
                                    }


                                }
                                else
                                    Console.WriteLine("Process broker wrongly formatted");
                                break;

                            case "publisher":
                                if (parsedLine[0].Equals("Process") && (parsedLine[2].Equals("Is") | parsedLine[2].Equals("is")) && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    publisherTable.Add(parsedLine[1], parsedLine[7]);
                                    string[] args = new string[4] { parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]], Ordering.ToString() };
                                    ProcessStartInfo startInfo = new ProcessStartInfo(parsedLine[1] + ".exe");
                                    startInfo.FileName = "publisher.exe";
                                    startInfo.Arguments = String.Join(" ", args);
                                    Process.Start(startInfo);
                                }
                                else
                                    Console.WriteLine("Process publisher wrongly formatted");
                                break;

                            case "subscriber":
                                if (parsedLine[0].Equals("Process") && (parsedLine[2].Equals("Is") | parsedLine[2].Equals("is")) && parsedLine[4].Equals("On") && parsedLine[6].Equals("URL"))
                                {
                                    subscriberTable.Add(parsedLine[1], parsedLine[7]);
                                    string[] args = new string[3] { parsedLine[1], parsedLine[7], SiteToBroker[parsedLine[5]] };
                                    ProcessStartInfo startInfo = new ProcessStartInfo();
                                    startInfo.FileName = "subscriber.exe";
                                    startInfo.Arguments = String.Join(" ", args);
                                    Process.Start(startInfo);
                                }
                                else
                                    Console.WriteLine("Process subscriber wrongly formatted");
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

                            case "filtering":
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

            Console.WriteLine("Reached end of ConfigFile.txt");
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
           
            PMInterface remotePM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");

            //verify user input and act if valid
            switch (firstToken)
            {
                case "Subscriber":
                    switch (inputParsed.ElementAt(2))
                    {
                        case "Subscribe":
                            processname = inputParsed.ElementAt(1);
                            topicname = inputParsed.ElementAt(3);
                            try
                            {
                                string subName = subscriberTable[processname];
                                remotePM.SendSubscribeOrder(subName, topicname);
                                Console.WriteLine("Subcribe Done");
                            }
                            catch (KeyNotFoundException)
                            {
                                Console.WriteLine("There is no subscriber with that name");
                            }
                            break;
                        case "Unsubscribe":
                            processname = inputParsed.ElementAt(1);
                            topicname = inputParsed.ElementAt(3);
                            try {
                                string subName = subscriberTable[processname];
                                remotePM.SendUnsubscribeOrder(subName, topicname);
                                Console.WriteLine("Unsubcribe Done");
                            }
                            catch(KeyNotFoundException)
                            {
                                Console.WriteLine("There is no subscriber with that name");
                            }
                            break;
                    }
                    break;
                case "Publisher":
                    //check if input if of type: Publisher p Publish n Ontopic t Interval x
                    if (inputParsed.ElementAt(2).Equals("Publish") && inputParsed.ElementAt(4).Equals("Ontopic") && inputParsed.ElementAt(6).Equals("Interval") && inputParsed.Count == 7)
                    {
                        processname = inputParsed.ElementAt(1);
                        topicname = inputParsed.ElementAt(5);
                        numberofevents = Int32.Parse(inputParsed.ElementAt(3));
                        sleepInterval = Int32.Parse(inputParsed.ElementAt(7));
                        try {
                            string pubName = publisherTable[processname];
                            remotePM.SendMultiplePublishOrder(pubName, processname, topicname, numberofevents, sleepInterval);
                            Console.WriteLine("Publishing Done");
                        }
                        catch(KeyNotFoundException)
                        {
                            Console.WriteLine("There is no publisher with that name");
                        }
                    } else if (inputParsed.ElementAt(2).Equals("Ontopic") && inputParsed.Count == 4)
                    {
                        processname = inputParsed.ElementAt(1);
                        topicname = inputParsed.ElementAt(2);
                        try {
                            string pubName = publisherTable[processname];
                            remotePM.SendPublishOrder(pubName, processname, topicname);
                            Console.WriteLine("Publishing Done");
                        }
                        catch(KeyNotFoundException)
                        {
                            Console.WriteLine("There is no publisher with that name");
                        }
                    } else {
                        Console.WriteLine("Publisher command incorrectly formated");
                    }
                    break;
                case "Status":
                    remotePM.StatusUpdate();
                    Console.WriteLine("Status Done");
                    break;
                case "Crash":
                    try {
                        processname = inputParsed.ElementAt(1);
                        if (processname.Contains("broker"))
                        {                            
                            remotePM.KillBroker(brokerTable[processname]);
                            //TODO make sure a broker is not allowed to crash more than 3 times
                            //brokerTable.Remove(processname);
                        }
                        else if (processname.Contains("subscriber"))
                        {                            
                            remotePM.KillSubscriber(subscriberTable[processname]);
                            subscriberTable.Remove(processname);
                        }
                        else if (processname.Contains("publisher"))
                        {                            
                            remotePM.KillPublisher(publisherTable[processname]);
                            publisherTable.Remove(processname);
                        }
                        Console.WriteLine("Crash Done");
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine("There is no process with that name");
                    }
                    break;
                case "Freeze":
                    try
                    {
                        processname = inputParsed.ElementAt(1);
                        //TODO make node sleep until awoken. child.sleep()?
                        if (processname.Contains("broker"))
                        {
                            remotePM.FreezeBroker(brokerTable[processname]);
                        }
                        else if (processname.Contains("subscriber"))
                        {
                            remotePM.FreezeSubscriber(subscriberTable[processname]);
                        }
                        else if (processname.Contains("publisher"))
                        {
                            remotePM.FreezePublisher(publisherTable[processname]);
                        }

                        Console.WriteLine("Freeze Done");
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine("There is no process with that name");
                    }
                    break;
                case "Unfreeze":
                    try
                    {
                        processname = inputParsed.ElementAt(1);
                        if (processname.Contains("broker"))
                        {
                            remotePM.UnfreezeBroker(brokerTable[processname]);
                        }
                        else if (processname.Contains("subscriber"))
                        {
                            remotePM.UnfreezeSubscriber(subscriberTable[processname]);
                        }
                        else if (processname.Contains("publisher"))
                        {
                            remotePM.UnfreezePublisher(publisherTable[processname]);
                        }

                        Console.WriteLine("Unfreeze Done");
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine("There is no process with that name");
                    }
                    break;
                case "Wait":
                    Console.WriteLine("Sleeping... zZZzzZ");
                    sleepInterval = Int32.Parse(inputParsed.ElementAt(1));
                    System.Threading.Thread.Sleep(sleepInterval);
                    Console.WriteLine("I'm Awake!");
                    break;
                case "Help":
                    Console.WriteLine("Here's a list of the acceptable commands");
                    Console.WriteLine("Subscriber (processname Subscribe (topicname)");
                    Console.WriteLine("Subscriber (processname) Unsubscribe (topicname)");
                    Console.WriteLine("Publisher (processname) Publish (numberofevents) Ontopic (topicname) Interval (x_ms)");
                    Console.WriteLine("Status");
                    Console.WriteLine("Crash (processname)");
                    Console.WriteLine("Freeze (processname)");
                    Console.WriteLine("Unfreeze (processname)");
                    Console.WriteLine("Wait (x_ms)");
                    Console.WriteLine("Quit");
                    break;
                case "Quit":
                    remotePM.Quit();
                    active = false;
                    break;
                case "Commands":
                    Console.WriteLine("Please wait while we read commands from file");
                    string commandPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"" + directory + "\\..\\..\\Commands.txt");
                    string[] commands = System.IO.File.ReadAllLines(commandPath);
                    string[] parsedCommands; //Line from config file that has the site information
                    foreach (string command in commands)
                    {
                        parsedCommands = command.Split(null);
                        ExecuteCommand(parsedCommands.ToList());
                    }
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

        //HashTable with the the sites and it's parents <site,parent>
        static internal Dictionary<string, string> siteTree = PuppetMaster.siteTree;
        //HashTable with the the brokers and it's URL. <brokers, URL>
        private Dictionary<string, string> brokerTable = PuppetMaster.brokerTable;
        //HashTable with the the publishers and it's URL. <publishers, URL>
        private Dictionary<string, string> publisherTable = PuppetMaster.publisherTable;
        //HashTable with the the subsribers and it's URL. <subsribers, URL>
        private Dictionary<string, string> subscriberTable = PuppetMaster.subscriberTable;
        //Monitor to make the access to the Log file thread safe.
        object logMonitor = new object();
        private static Mutex logEventMut = new Mutex();
               
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
            //bollean that has value true after first iteration of method. On first iteration a new file must be created
            bool fileCreated = false;
            
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

            logEventMut.WaitOne();
                if (PuppetMaster.Loglevel == 1)
                {
                    text = eventlabel + " " + process1Name + ", " + process2Name + ", " + topicname + ", " + eventnumber;
                }

                if (PuppetMaster.Loglevel != 1 && (eventlabel.Equals("PubEvent") || eventlabel.Equals("SubEvent")))
                {
                    text = eventlabel + " " + process1Name + ", " + process2Name + ", " + topicname + ", " + eventnumber;
                }

                eventnumber++;
                logEventMut.ReleaseMutex();

            string path = @"" + directory + "\\..\\..\\Log.txt";
            //TODO change if statements - they're too expensive
            //TODO if file.create even needed?
            lock (logMonitor)
            {
                TextWriter tw = new StreamWriter(path, true);
                tw.WriteLine(text);
                tw.Close();
            }
        }

        public void SendSubscribeOrder(String subURL, string topic)
        {
            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subURL);
            sub.AddSubscriptionRemote(topic);
        }

        public void SendUnsubscribeOrder(String subURL, string topic)
        {
            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subURL);
            sub.RemoveSubscriptionRemote(topic);
        }

        public void SendPublishOrder(string pubURL, string processName, string topicname)
        {
            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), pubURL);
            pub.ChangeTopic(topicname);
            pub.SendPublication(topicname +": Publisher: " + processName + "; event ");
        }

        public void SendMultiplePublishOrder(string pubURL, string processName, string topicname, int numberofevents, int sleepInterval)
        {
            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), pubURL);
            pub.ChangeTopic(topicname);
            pub.MultipleSendPublication(topicname +": Publisher: " + processName + "; event ", sleepInterval, numberofevents);
        }

        public void KillBroker(string URL)
        {

            BrokerInterface bk = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), URL);
            try
            {
                bk.Kill();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void KillSubscriber(string URL)
        {

            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), URL);
            try
            {
                sub.Kill();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void KillPublisher(string URL)
        {

            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), URL);
            try {
                pub.Kill();
            }catch(System.Net.Sockets.SocketException) { }
        }

       public void FreezeBroker(string URL)
        {

            BrokerInterface bk = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), URL);
            try
            {
                bk.Freeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

       public  void FreezeSubscriber(string URL)
        {
            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), URL);
            try
            {
                sub.Freeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void FreezePublisher(string URL)
        {
            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), URL);
            try
            {
                pub.Freeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void UnfreezeBroker(string URL)
        {

            BrokerInterface bk = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), URL);
            try
            {
                bk.Unfreeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void UnfreezeSubscriber(string URL)
        {
            SubscriberInterface sub = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), URL);
            try
            {
                sub.Unfreeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        public void UnfreezePublisher(string URL)
        {
            PublisherInterface pub = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), URL);
            try
            {
                pub.Unfreeze();
            }
            catch (System.Net.Sockets.SocketException) { }
        }

        //calls for a system-wide status report
        //starts on the root of the broker-tree
        //propagation is guaranteed by the nodes.
        public void StatusUpdate()
        {
            string sitename = siteTree.FirstOrDefault(x => x.Value.Contains("none")).Key;
            string brokerURL = PuppetMaster.SiteToBroker[sitename];
            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brokerURL);
            bi.StatusUpdate();

        }

        public void Quit()
        {
            Console.WriteLine("Killing... RIP");
            Console.WriteLine("This may take a few momments");
            foreach (string subURL in subscriberTable.Values)
            {
                KillSubscriber(subURL);
            }
            foreach (string pubURL in publisherTable.Values)
            {
                KillPublisher(pubURL);
            }
            foreach (string brkURL in brokerTable.Values)
            {
                try
                {
                    //use leader URL to get replicas URL
                    string brkReplica1 = transformURL(brokerTable.FirstOrDefault(x => x.Value.Contains(brkURL)).Key, brkURL, 1);
                    KillBroker(brkReplica1);
                    try
                    {
                        //use leader URL to get replicas URL
                        string brkReplica2 = transformURL(brokerTable.FirstOrDefault(x => x.Value.Contains(brkURL)).Key, brkURL, 2);
                        KillBroker(brkReplica2);
                    }
                    finally{ }
                }            
                finally
                {
                    KillBroker(brkURL);
                }
            }
        }

        //method to transform a broker url into one of his replicas
        //done by adding a predefined value into the port
        //and changing the processname for that of the replica
        private string transformURL(string processname, string url, int replicaNum)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";

            //since 2 processes can't sahre same port then replicas add 20 or 21 to port number
            //to prevent collision with other replicas we also add the last number from this specific replica
            if (replicaNum == 1)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 20 + (int.Parse(parsedURLv2[0]) % 100) ).ToString();
            if (replicaNum == 2)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 21 + (int.Parse(parsedURLv2[0]) % 100) ).ToString();
            
            //rejoin modified parsels into the new URL
            string newURLv2 = string.Join("/", parsedURLv2);
            parsedURL[2] = newURLv2;
            string newURL = string.Join(":", parsedURL);
            return newURL;
        }
    }
}
