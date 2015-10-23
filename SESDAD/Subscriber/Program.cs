using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    static class Subscriber
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        internal static BrokerInterface broker;
        internal static Form1 form;
        [STAThread]
        static void Main()
        {
            //hardcoded port to 8090
            //TODO change port to be dynamic
            TcpChannel channel = new TcpChannel(8090);
            ChannelServices.RegisterChannel(channel, false);

            RemoteSubscriber rs = new RemoteSubscriber();
            RemotingServices.Marshal(rs, "SubscriberServer", typeof(RemoteSubscriber));

            broker = (BrokerInterface)Activator.GetObject(
                typeof(BrokerInterface),
                "tcp://localhost:8086/BrokerServer");

            try
            {
                broker.ConnectSubscriber();
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }
    }


    delegate void DelegateReceivePublication(string message);

    public class RemoteSubscriber : MarshalByRefObject, SubscriberInterface
    {
        public static Form1 form = Subscriber.form;
        private BrokerInterface broker = Subscriber.broker;

        public void ReceivePublication(string publication)
        {
            form.Invoke(new DelegateReceivePublication(form.UpdatePublication), publication);
        }

        internal void AddSubscription(string topic)
        {
            try
            {
                broker.AddSubscription(this, topic);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }
        }
    }


}
