using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;


namespace IoTSharp.nanoDevice
{
    public class Program
    {
        private const string _token = "be2b6208db0742d99be85c2d74715cba";
        private const string   BrokerAddress = "139.9.232.10";

        public static void Main()
        {
            MqttClient client = null;
            string clientId;

            // Wait for Wifi/network to connect (temp)
            SetupAndConnectNetwork();
            long dida = 0;
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
                    client.Publish("devices/"+ clientId + "/attributes", message, MqttQoSLevel.ExactlyOnce, false);

                    string[] SubTopics = new string[]
                    {
                        "/devices/"+clientId+"/rpc/request/+/+",
                        "/devices/"+clientId+"/attributes/update/"
                    };

                    Debug.WriteLine("Subscribe attributes and request");
                    client.Subscribe(SubTopics, new MqttQoSLevel[] { MqttQoSLevel.ExactlyOnce, MqttQoSLevel.ExactlyOnce });
                    
                    Debug.WriteLine("Enter wait loop");
                    while (client.IsConnected)
                    {
                        try
                        {
                            Thread.Sleep(10000);
                            dida++;
                            message = Encoding.UTF8.GetBytes("{\"NowDateTime\":\"" + DateTime.UtcNow.ToString() + "\",\"Dida\":" + dida.ToString() + "}");
                           var result=  client.Publish("devices/" + clientId + "/telemetry", message, MqttQoSLevel.ExactlyOnce, false);
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