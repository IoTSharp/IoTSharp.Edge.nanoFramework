using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace IoTSharp.nanoDevice
{
    public class Program
    {
        private const string _token = "581e918118a34c9faf2b9ede8245be33";
        private const string   BrokerAddress = "192.168.0.23";

        public static void Main()
        {
            MqttClient client = null;
            string clientId;

            // Wait for Wifi/network to connect (temp)
            SetupAndConnectNetwork();

            // Loop forever
            while (true)
            {
                try
                {
                    client = new MqttClient(BrokerAddress);
                    // register a callback-function (we have to implement, see below) which is called by the library when a message was received
                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                    client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                    // use a unique id as client id, each time we start the application
                    //clientId = Guid.NewGuid().ToString();
                    clientId =  Guid.NewGuid ().ToString();

                    Debug.WriteLine("Connecting MQTT");

                    client.Connect(clientId, _token, "") ;

                    Debug.WriteLine("Connected MQTT");
                    // Subscribe topics
                    //     client.Subscribe(new string[] { "Test1", "Test2" }, new byte[] { 2, 2 });
                    var buffer = nanoFramework.Hardware.Stm32.Utilities.UniqueDeviceId;
                    byte[] message = Encoding.UTF8.GetBytes("{\"DeviceId\":\""+ Encoding.UTF8.GetString(buffer,0, buffer.Length) +"\"}");
                    client.Publish("devices/"+ clientId + "/attributes", message, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                    string[] SubTopics = new string[]
                    {
                        "/devices/"+clientId+"/rpc/request/+/+",
                        "/devices/"+clientId+"/attributes/update/"
                    };

                    Debug.WriteLine("Subscribe attributes and request");
                    client.Subscribe(SubTopics, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    
                    Debug.WriteLine("Enter wait loop");
                    while (client.IsConnected)
                    {
                        try
                        {
                            Thread.Sleep(10000);
                            message = Encoding.UTF8.GetBytes("{\"NowDateTime\":\"" + DateTime.UtcNow.ToString() + "\"}");
                           var result=  client.Publish("devices/" + clientId + "/telemetry", message, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            Debug.WriteLine(result.ToString());
                        }
                        catch (Exception ex)
                        {

                            Debug.WriteLine("Publish exception " + ex.Message);
                        }
                    }

                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    // Do whatever please you with the exception caught
                    Debug.WriteLine("Main exception " + ex.Message);
                }

                // Wait before retry
                Thread.Sleep(10000);
            }
        }

        private static void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Debug.WriteLine("Client_MqttMsgSubscribed ");
        }

        private static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic;

            string message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            Debug.WriteLine("Publish Received Topic:" + topic + " Message:" + message);

        }
        public static void SetupAndConnectNetwork()
        {
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            if (nis.Length > 0)
            {
                // get the first interface
                NetworkInterface ni = nis[0];

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    // network interface is Wi-Fi
                    Debug.WriteLine("Network connection is: Wi-Fi");
                }
                else
                {
                    // network interface is Ethernet
                    Debug.WriteLine("Network connection is: Ethernet");
                    ni.EnableDhcp();
                }

                // wait for DHCP to complete
                WaitIP();
            }
            else
            {
                throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
            }
        }

        static void WaitIP()
        {
            Debug.WriteLine("Waiting for IP...");

            while (true)
            {
                NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
                {
                    if (ni.IPv4Address[0] != '0')
                    {
                        Debug.WriteLine($"We have an IP: {ni.IPv4Address}");
                        break;
                    }
                }

                Thread.Sleep(500);
            }
        }


    }
}