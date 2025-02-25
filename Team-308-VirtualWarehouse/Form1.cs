﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;
using System.Globalization;
using System.Drawing.Drawing2D;
using CsvHelper;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Reflection.Emit;
using System.Collections.Specialized;

namespace Team_308_VirtualWarehouse
{
    public partial class Form1 : Form
    {
        // Needed for GetCoordinates()
        string[] result = new string[3];
        string[] result1 = new string[3];

        // since form1 can be the only class object to be started with the application
        // and gridmap relies on form1 to work, gridmap would be instantiated inside of form1
        private GridMap gridmap;

        private bool configured = false;
        public Form1()
        {     
            InitializeComponent();

            // ***** just added testing *****
            getData();
            gridmap = new GridMap(this);
            
        }


        //this function establishes a connection to an MQTT server,
        //subscribes to a specified MQTT topic, and listens for incoming messages
        private async void getData()
        {
            Console.WriteLine("Connecting...");

            //MqttFactory() is used to create a MqttClient instance
            var mqttFactory = new MqttFactory();
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                //The MqttClientOptionsBuilder is used to create an MqttClientOptions object
                //that specifies the MQTT SERVER and PORT to connect to
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(MqttConfig.Server, MqttConfig.Port)
                    //.WithCredentials(MqttConfig.User, MqttConfig.Password)
                    .Build();

                //handleReceivedApplicationMessage function is subscribed to the mqttClient's ApplicationMessageReceivedAsync event
                //whenever an MQTT message is received, the handleReceivedApplicationMessage function will be called to handle it
                mqttClient.ApplicationMessageReceivedAsync += this.handleReceivedApplicationMessage;

                //establish a connection to the MQTT server
                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                //create an MqttSubscribeOptions object that specifies the MQTT TOPIC to subscribe to
                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f =>
                    {
                        f.WithTopic(MqttConfig.Topic);
                    })
                .Build();

                //The mqttClient.SubscribeAsync() method is used to SUBSCRIBE to the specified MQTT TOPIC
                var response = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                this.BeginInvoke((MethodInvoker)delegate { this.Loft_TextBox.Text = "MQTT client subscribed."; });

                //response.DumpToConsole();
                Dumper.Dump(response);

            }
            Console.WriteLine("MQTT Client Created");
        }

        // get angle values from data
        // azimuth, elevation, distance
        private string[] parseApplicationMessage(string payload)
        {
            // string[] result = new string[3];

            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i].ToString() + payload[i + 1].ToString() == "x\"")
                {
                    result[0] = payload.Substring(i + 3, 9);
                }
                else if (payload[i].ToString() + payload[i + 1].ToString() == "y\"")
                {
                    result[1] = payload.Substring(i + 3, 9);
                }
                else if (payload[i].ToString() + payload[i + 1].ToString() == "z\"")
                {
                    result[2] = payload.Substring(i + 3, 9);
                    break;
                }
            }
            return result;
        }

        // get position values from data
        // x,y,z
        private string[] parsesApplicationMessage(string payload)
        {
            Console.WriteLine("GETTING POSITION DATA IN PARSES APP FUNCTION");
            // string[] result = new string[3];

            for (int i = 0; i < payload.Length - 1; i++)
            {
                Console.WriteLine(payload.Substring(i, 1) + i);
                if (payload.Substring(i, 1) == "x")
                {
                    result1[0] = payload.Substring(i + 4, 8);
                }
                else if (payload.Substring(i, 2) == "y")
                {
                    result1[1] = payload.Substring(i + 4, 8);
                }
                else if (payload.Substring(i, 2) == "z")
                {
                    result1[2] = payload.Substring(i + 4, 8);
                    break;
                }
            }
            return result1;
        }

        private string[] getPosValues(string payload)
        {
            // string[] result = new string[3];
            string temp = "";

            for (int i = 0; i < payload.Length - 1; i++)
            {
                if (payload.Substring(i, 1) == "x")
                {
                    for (int j = i + 4; payload.Substring(j, 1) != "."; j++)
                    {
                        temp += payload.Substring(j, 1);
                    }
                    result1[0] = temp;
                }
                else if (payload.Substring(i, 1) == "y")
                {
                    for (int j = i + 4; payload.Substring(j, 1) != "."; j++)
                    {
                        temp += payload.Substring(j, 1);
                    }
                    result1[1] = temp;
                }
                else if (payload.Substring(i, 1) == "z")
                {
                    result1[2] = payload.Substring(i + 4, 8);
                    break;
                }
                temp = "";
            }
            return result1;
        }

        //When the application receives a message from the MQTT broker, this function is executed

        /* When the button is clicked, it starts the application.
         It receives messages from the MQTT broker and prints the received message in the textbox.

         The function parses the received message to get the values of azimuth, elevation, and distance.

         It then calls the parsesApplicationMessage method to get the values of x, y, and z.

         It writes these values to a CSV file using the CSVWriter class.

         Finally, it updates the text of textbox1, textbox2, and textbox3 with the

         values of azimuth, elevation, distance, x, y, and z respectively.*/
        private Task handleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs m)
        {
            // simply sets Textbox3 to a new string value
            this.BeginInvoke((MethodInvoker)delegate { this.Loft_TextBox.Text = "Received application message"; });

            // m is the message recieved from the server and locator
            while (m != null)
            {
                // m.ApplicationMessage.Payload contains actual message sent by mqtt broker
                /*This line of code converts the payload (a byte array) received from the MQTT broker into a
                  readable string using UTF-8 encoding and stores the resulting string in the variable "payload"*/
                string payload = Encoding.UTF8.GetString(m.ApplicationMessage.Payload);
                Console.WriteLine("******payload******: ");
                // this.BeginInvoke((MethodInvoker)delegate { this.X_TextBox.Text = payload; });

                // string[] angleValues = new string[3];
                string[] positionValues = new string[3];
                // angleValues = parseApplicationMessage(payload);
                // positionValues = parsesApplicationMessage(payload);
                positionValues = getPosValues(payload);
                // string azimuth = angleValues[0];
                // string elevation = angleValues[1];
                // string distance = angleValues[2];

                string x = positionValues[0];
                string y = positionValues[1];
                string z = positionValues[2];

                result1[0] = x;
                result1[1] = y;
                result1[2] = z;

                CSVWriter.writeToCSV(new Payload() { Time = DateTime.Now, Row = x, Column = y, Loft = z });

                // double temp1 = double.Parse(azimuth, CultureInfo.InvariantCulture.NumberFormat);
                // double temp2 = double.Parse(elevation, CultureInfo.InvariantCulture.NumberFormat);

                
                // float x1 = 1;
                // float y1 = 1;

                if (x != null)
                {
                    Console.WriteLine("******************* x: " + x);
                    // x1 = float.Parse(x, CultureInfo.InvariantCulture.NumberFormat);
                }
                if (y != null)
                {
                    Console.WriteLine("******************* y: " + y);
                    // y1 = float.Parse(y, CultureInfo.InvariantCulture.NumberFormat);
                }
                //8, 33      //-8, 35
                //7.3, 33   //5, 35

                //7, 32     //3, 33
                //5, 33     //2, 32

                //temp1 = (temp1 - 1.5) / 2;
                //temp2 = (temp2 - 33);

                Console.WriteLine(payload + " //// " + x + " //// " + y + " //// " + z);

                this.BeginInvoke((MethodInvoker)delegate { this.X_TextBox.Text = x; });
                this.BeginInvoke((MethodInvoker)delegate { this.Y_TextBox.Text = y; });


                // this.BeginInvoke((MethodInvoker)delegate { this.X_TextBox.Text = x1.ToString(); });
                // this.BeginInvoke((MethodInvoker)delegate { this.Y_TextBox.Text = y1.ToString(); });
                

                //x = "17"; y = "38"; 

                //X_TextBox.Text = x;
                //Y_TextBox.Text = y;

                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        private void LocationButton_Click(object sender, EventArgs e)
        {
            // getData();
            if (!configured)
            {
                gridmap.setOrigin();
                // location_button.Text = "Get Location";
                configured = true;

                this.BeginInvoke((MethodInvoker)delegate { this.X_TextBox.Text = gridmap.getNormalizedX().ToString("0.000"); });
                this.BeginInvoke((MethodInvoker)delegate { this.Y_TextBox.Text = gridmap.getNormalizedY().ToString("0.000"); });
                this.BeginInvoke((MethodInvoker)delegate { this.Loft_TextBox.Text = gridmap.calculateGrid(); });
            }
            else
            {
                this.BeginInvoke((MethodInvoker)delegate { this.X_TextBox.Text = gridmap.getNormalizedX().ToString("0.000"); });
                this.BeginInvoke((MethodInvoker)delegate { this.Y_TextBox.Text = gridmap.getNormalizedY().ToString("0.000"); });
                this.BeginInvoke((MethodInvoker)delegate { this.Loft_TextBox.Text = gridmap.calculateGrid(); });
                Console.WriteLine("END OF GET DATA FUNCTION");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //GridMap.PaintGridMap();
            gridmap.Show();
        }

        public (int x, int y) GetCoordinates()
        {
            getData();
            if (result1 == null || result1.Length < 2)
            {
                throw new InvalidOperationException("Coordinates not initialized or invalid.");
            }

            result

            // string[] positionValues = new string[3];

            Console.WriteLine("***** result1[0]: " + result1[0]);
            Console.WriteLine("***** result1[1]: " + result1[1]);

            // int num1 = int.Parse(result1[0]);
            // int num2 = int.Parse(result1[1]);

            return (int.Parse(result1[0]), int.Parse(result1[1]));
        }
    }
}
