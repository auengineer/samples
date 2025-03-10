﻿using System;
using DittoSDK;
using System.Collections.Generic;

namespace Tasks
{
    class Program
    {
        static Ditto ditto;
        static bool isAskedToExit = false;
        static List<Task> tasks = new List<Task>();
        static DittoLiveQuery liveQuery;


        public static void Main(params string[] args)
        {
            ditto = new Ditto(identity: DittoIdentity.OfflinePlayground());

            try
            {
                ditto.SetOfflineOnlyLicenseToken("<REPLACE_ME>");
                DittoTransportConfig transportConfig = new DittoTransportConfig();

                // Enable Local Area Network Connections
                transportConfig.EnableAllPeerToPeer();

                // Listen for incoming connections on port 4000
                transportConfig.Listen.Tcp.Enabled = true;
                transportConfig.Listen.Tcp.InterfaceIp = "0.0.0.0";
                transportConfig.Listen.Tcp.Port = 4000;

                // Connect explicitly to a remote device on 
                transportConfig.Connect.TcpServers.Add("135.1.5.5:12345");
                // you can add as many TcpServers as you would like.
                transportConfig.Connect.TcpServers.Add("185.1.5.5:12345");

                ditto.TransportConfig = transportConfig;

                ditto.StartSync();
            }
            catch (DittoException ex)
            {
                Console.WriteLine("There was an error starting Ditto.");
                Console.WriteLine("Here's the following error");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Ditto cannot start sync but don't worry.");
                Console.WriteLine("Ditto will still work as a local database.");
            }

            Console.WriteLine("Welcome to Ditto's Task App");

            liveQuery = ditto.Store["tasks"].Find("!isDeleted").Observe((docs, _event) => {
                tasks = docs.ConvertAll(document => new Task(document));
            });

            ditto.Store["tasks"].Find("isDeleted == true").Evict();

            ListCommands();

            while (!isAskedToExit)
            {
                
                Console.Write("\nYour command: ");
                string command = Console.ReadLine();

                switch (command)
                {
                    
                    case string s when command.StartsWith("--upsert"):
                        string taskBody = s.Replace("--upsert ", "");
                        ditto.Store["tasks"].Upsert(new Task(taskBody, false).ToDictionary());
                        break;
                    case string s when command.StartsWith("--toggle"):
                        string _idToToggle = s.Replace("--toggle ", "");
                        ditto.Store["tasks"]
                            .FindById(new DittoDocumentID(_idToToggle))
                            .Update((mutableDoc) => {
                                if (mutableDoc == null) return;
                                mutableDoc["isCompleted"].Set(!mutableDoc["isCompleted"].BooleanValue);
                            });
                        break;
                    case string s when command.StartsWith("--delete"):
                        string _idToDelete = s.Replace("--delete ", "");
                        ditto.Store["tasks"]
                            .FindById(new DittoDocumentID(_idToDelete))
                            .Update((mutableDoc) => {
                                if (mutableDoc == null) return;
                                mutableDoc["isDeleted"].Set(true);
                            });
                        break;
                    case { } when command.StartsWith("--list"):
                        tasks.ForEach(task =>
                        {
                            Console.WriteLine(task.ToString());
                        });
                        break;
                    case { } when command.StartsWith("--exit"):
                        Console.WriteLine("Good bye!");
                        isAskedToExit = true;
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        ListCommands();
                        break;
                }
            }
        }

        public static void ListCommands()
        {
            Console.WriteLine("************* Commands *************");
            Console.WriteLine("--upsert my new task");
            Console.WriteLine("   Upsert a task");
            Console.WriteLine("   Example: \"--upsert Get Milk\"");
            Console.WriteLine("--toggle myTaskTd");
            Console.WriteLine("   Toggles the isComplete property to the opposite value");
            Console.WriteLine("   Example: \"--toggle 1234abc\"");
            Console.WriteLine("--delete myTaskTd");
            Console.WriteLine("   Deletes a task");
            Console.WriteLine("   Example: \"--delete 1234abc\"");
            Console.WriteLine("--list");
            Console.WriteLine("   List the current tasks");
            Console.WriteLine("--exit");
            Console.WriteLine("   Exits the program");
            Console.WriteLine("************* Commands *************");
        }
    }
}
