using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Ets2SdkClient.Demo
{
    public partial class Ets2SdkClientDemo : Form
    {
        public Ets2SdkTelemetry Telemetry;

        private ConnectionMultiplexer redis;

        public Ets2SdkClientDemo()
        {
            InitializeComponent();

            // Connect to the Redis server
            redis = ConnectionMultiplexer.Connect("localhost");
            // Get a reference to the Redis database
            IDatabase db = redis.GetDatabase();

            Telemetry = new Ets2SdkTelemetry();
            Telemetry.Data += (Ets2Telemetry data, bool updated) =>
            {
                PushToRedis(db, data);
                Telemetry_Data(data, updated);
            };

            Telemetry.JobFinished += TelemetryOnJobFinished;
            Telemetry.JobStarted += TelemetryOnJobStarted;

            if (Telemetry.Error != null)
            {
                lbGeneral.Text =
                    "General info:\r\nFailed to open memory map " + Telemetry.Map +
                        " - on some systems you need to run the client (this app) with elevated permissions, because e.g. you're running Steam/ETS2 with elevated permissions as well. .NET reported the following Exception:\r\n" +
                        Telemetry.Error.Message + "\r\n\r\nStacktrace:\r\n" + Telemetry.Error.StackTrace;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // Disconnect from Redis
            redis.Close();
        }

        private void TelemetryOnJobFinished(object sender, EventArgs args)
        {
            MessageBox.Show("Job finished, or at least unloaded nearby cargo destination.");
        }

        private void TelemetryOnJobStarted(object sender, EventArgs e)
        {
            MessageBox.Show("Just started job OR loaded game with active.");
        }

        private void PushToRedis(IDatabase db, Ets2Telemetry data)
        {
            // Set a key-value pair
            db.StringSet("engineRpm", data.Drivetrain.EngineRpm.ToString());
            db.StringSet("truckModel", data.TruckId.ToString());
            db.StringSet("speed", data.Drivetrain.SpeedKmh.ToString());
            db.StringSet("brakeTemperature", data.Drivetrain.BrakeTemperature.ToString());
            db.StringSet("userThrottle", data.Controls.UserThrottle.ToString());
            db.StringSet("userBrake", data.Controls.UserBrake.ToString());
            db.StringSet("userSteer", data.Controls.UserSteer.ToString());
            db.StringSet("trailerMass", data.Job.Mass.ToString());
            db.StringSet("truckOdometer", data.Drivetrain.TruckOdometer.ToString());




        }

        private void Telemetry_Data(Ets2Telemetry data, bool updated)
        {
            try
            {
                ////Catching data structure produced by Ets2Telemetry class
                //// Set a variable to the Documents path.
                //string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                //// Append text to an existing file named "WriteLines_from_ETS2_SDK.txt".
                //using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "Write_Ets2Telemetry_from_ETS2_SDK.txt"), false))
                //{
                //    string my_json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                //    //outputFile.WriteLine("Line " + DateTime.Now.ToUniversalTime().ToString());
                //    outputFile.WriteLine(my_json);
                //}
                
                
                

                if (this.InvokeRequired)
                {
                    this.Invoke(new TelemetryData(Telemetry_Data), new object[2] { data, updated });
                    return;
                }

                lbGeneral.Text = "General info:\r\n SDK Version: " + data.Version.SdkPlugin + "\r\n Reported game Version: " +
                                 data.Version.Ets2Major + "." + data.Version.Ets2Minor + "\r\n\r\nTruck: " + data.Truck + " (" + data.TruckId + ")\r\nManufacturer: " + data.Manufacturer + "(" + data.ManufacturerId + ")" +
                                 "\r\nGame Timestamp: " + data.Time + "\r\nPaused? " + data.Paused;

                // Do some magic trickery to display ALL info:
                var grps = new object[]
                       {
                           data.Drivetrain, data.Physics, data.Controls, data.Axilliary, data.Damage, data.Lights, data.Job
                       };

                foreach (var grp in grps)
                {
                    // Find the right tab page:
                    var grpName = grp.GetType().Name;
                    if (grpName.StartsWith("_"))
                        grpName = grpName.Substring(1);

                    var tabPage = default(TabPage);
                    var tabFound = false;

                    for (int k = 0; k < telemetryInfo.TabCount; k++)
                    {
                        if (telemetryInfo.TabPages[k].Text == grpName)
                        {
                            tabPage = telemetryInfo.TabPages[k];
                            tabFound = true;
                        }
                    }
                    if (!tabFound)
                    {
                        tabPage = new CustomTabPage(grpName);
                        telemetryInfo.TabPages.Add(tabPage);
                    }

                    // All properties;
                    var props = grp.GetType().GetProperties().OrderBy(x => x.Name);
                    var labels = new StringBuilder();
                    var vals = new StringBuilder();
                    foreach (var prop in props)
                    {
                        labels.AppendLine(prop.Name + ":");
                        object val = prop.GetValue(grp, null);
                        if (val is float[])
                        {
                            vals.AppendLine(string.Join(", ", (val as float[]).Select(x=> x.ToString("0.000"))));
                        }
                        else
                        {
                            vals.AppendLine(val.ToString());
                        }
                    }

                    tabPage.Controls.Clear();
                    var lbl1 = new Label { Location = new Point(3, 3), Size = new Size(200, tabPage.Height - 6) };
                    var lbl2 = new Label { Location = new Point(203, 3), Size = new Size(1000, tabPage.Height - 6) };
                    lbl1.Text = labels.ToString();
                    lbl2.Text = vals.ToString();
                    lbl2.AutoSize = false;
                    tabPage.Controls.Add(lbl1);
                    tabPage.Controls.Add(lbl2);
                }
            }
            catch
            {
            }
        }
    }
}
