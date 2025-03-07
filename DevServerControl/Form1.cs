﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevServerControl
{
    public partial class Form1 : Form
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static Dictionary<string, string> ServiceStatus = new Dictionary<string, string>();

        // ReSharper disable once MemberCanBePrivate.Global
        public static string WampPath = "C:\\wamp64\\";

        // ReSharper disable once FieldCanBeMadeReadOnly.Global

        //Form1
        public Form1()
        {
            InitializeComponent();
        }

        //Form1 onload
        private void Form1_Load(object sender, EventArgs e)
        {
            // Readonly: tbx_log
            tbx_log.ReadOnly = true;
            // Verify wamp installation
            if (!Directory.Exists(WampPath))
            {
                MessageBox.Show(
                    $@"Do not detected the WAMP installation, Config your install path at next window. (current: {WampPath})",
                    @"Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                var wampPathSelector = new FolderBrowserDialog();
                wampPathSelector.ShowDialog();
                WampPath = wampPathSelector.SelectedPath + "\\";
            }
            else
            {
                AppendText(tbx_log, $"Wamp install detected! {WampPath}", 10, Color.DarkGreen, true);
            }

            WindowState = FormWindowState.Normal;

            AppendText(tbx_log, $"Wamp install set to {WampPath}", 10, Color.DarkGreen, true);

            //Verify dynamodb install
            if (!Directory.Exists(Dynamodb.path))
            {
                MessageBox.Show(
                    $@"Do not detected the DynamoDB installation, Config your install path at next window. (current: {Dynamodb.path})",
                    @"Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                var dynamodbPathSelector = new FolderBrowserDialog();
                dynamodbPathSelector.ShowDialog();
                Dynamodb.path = dynamodbPathSelector.SelectedPath + "\\";
            }
            else
            {
                AppendText(tbx_log, $"DynamoDB install detected {Dynamodb.path}", 10, Color.DarkGreen, true);
            }

            AppendText(tbx_log, $"DynamoDB install set to {WampPath}", 10, Color.DarkGreen, true);

            //Fetch installed apache versions
            AppendText(tbx_log, "Detected Apache versions:", 10, Color.Black, false);
            foreach (
                var apacheVersionPath
                in Directory.GetDirectories(WampPath + "bin\\apache"))
            {
                var version = apacheVersionPath
                    .Split('e').Last();
                Apache.Versions.Add(version);
                AppendText(tbx_log, version, 10, Color.Black, false);
            }

            //Fetch installed php versions
            AppendText(tbx_log, "Detected PHP versions:", 10, Color.Black, false);
            foreach (var phpVersionPath in Directory.GetDirectories(WampPath + "bin\\php"))
            {
                var version = phpVersionPath
                    .Split('p').Last();
                Php.Versions.Add(version);
                AppendText(tbx_log, version, 10, Color.Black, false);
            }

            //Print fetched php versions to buttons
            var phpVersionButtons = new Dictionary<string, Button>();
            var phpVersionsDictionary = Php.Versions.Select(
                (s, i) => new { s, i }
            ).ToDictionary(
                x => x.i,
                x => x.s
            );
            foreach (var version in phpVersionsDictionary)
            {
                phpVersionButtons[version.Value] = new Button
                {
                    Location = new Point(69 + 87 * version.Key, 47),
                    Size = new Size(81, 27),
                    Text = version.Value,
                    Name = "switchPhpVersion_" + version.Value,
                    Font = new Font("Arial", 9),
                };
                phpVersionButtons[version.Value].Click += (o, args) => { Php.SwitchVersion(version.Value); };
                Controls.Add(phpVersionButtons[version.Value]);
            }

            //Get apache status
            Apache.RefreshStatus();
            btn_apache_toggle.Text = ServiceStatus["apache"] == "running" ? "stop" : "start";
            AppendText(tbx_log, $"Apache2 is {ServiceStatus["apache"]}", 10, Color.Brown, true);

            //Set onclick event for Apache toggle button
            btn_apache_toggle.Click += async (o, args) =>
            {
                Apache.RefreshStatus();
                if (ServiceStatus["apache"] == "running")
                {
                    Apache.Stop();
                }
                else
                {
                    await Apache.Start();
                }

                Apache.RefreshStatus();
            };

            //Set onclick event for Apache restart button
            btn_apache_restart.Click += (o, args) => Apache.Restart();

            //Set onclick event for check port
            chk_port_80.Click += async (o, args) =>
            {
                await Task.Run(() => check_port(80));
                await Task.Yield();
            };
            chk_port_443.Click += async (o, args) =>
            {
                await Task.Run(() => check_port(443));
                await Task.Yield();
            };

            //Hide tray icon
            notifyIcon1.Visible = false;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void AppendText(RichTextBox textBox, string str, int fontSize, Color fontColor, bool bold)
        {
            // Invoking to prevent form exception
            tbx_log.Invoke((Action)delegate
            {
                textBox.SelectionColor = Color.LightGray;
                textBox.SelectionFont =
                    new Font("Arial", fontSize, FontStyle.Regular);
                textBox.AppendText(DateTime.Now.ToString("[HH:mm:ss tt] \t"));
                textBox.SelectionColor = fontColor;
                textBox.SelectionFont =
                    new Font("Arial", fontSize, bold ? FontStyle.Bold : FontStyle.Regular);
                textBox.AppendText(str + "\r\n");

                // scroll it automatically
                textBox.SelectionStart = textBox.Text.Length;
                textBox.ScrollToCaret();
            });
        }

        private async Task check_port(int port)
        {
            AppendText(tbx_log, $"Now checking port {port}, please wait...", 10, Color.LightGray, false);
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var ipEndPoints = ipProperties.GetActiveTcpListeners();

            if (ipEndPoints.Any(endPoint => endPoint.Port == port))
            {
                get_port_used_by(port);
                //used
                AppendText(tbx_log, $"Port {port} in use!", 10, Color.DarkRed, true);
                AppendText(tbx_log, $"Port {port} is used by {_processPid}", 10, Color.DarkRed, true);
                var killTask = MessageBox.Show(
                    // ReSharper disable once LocalizableElement
                    $"Port {port} is used by {_processName} (PID: {_processPid})\r\nDo you want to kill this process?",
                    @"Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (killTask != DialogResult.Yes) return;
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            Arguments = "/F /PID \"" + _processPid + "\""
                        };
                        process.Start();
                        process.WaitForExit(60000);
                        AppendText(tbx_log, $"{_processPid} killed!", 10, Color.LightGreen, true);
                    }
                }
                catch (Exception e)
                {
                    AppendText(tbx_log, $"Failed to kill task {_processPid}", 10, Color.DarkRed, true);
                    AppendText(tbx_log, e.ToString(), 10, Color.DarkRed, false);
                }
            }
            else
            {
                //free
                try
                {
                    AppendText(tbx_log, $"Port {port} is free!", 10, Color.Black, false);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        //@return PID of process
        private static string _processPid;
        private static string _processName;

        private static void get_port_used_by(int port)
        {
            foreach (var p in ProcessPorts.ProcessPortMap.FindAll(x => x.PortNumber == port))
            {
                _processName = p.ProcessPortDescription
                    .Split(' ')[0];
                _processPid = p.ProcessPortDescription
                    .Split(' ')[5]
                    .Split(')')[0];
                return;
            }

            _processName = string.Empty;
            _processPid = string.Empty;
        }

        //----------------------- Event handlers --------------------------
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            Hide();
            notifyIcon1.Visible = true;
        }

        private void btn_apache_vhost_Click(object sender, EventArgs e)
        {
            var virtualHost = new VirtualHost();
            virtualHost.Show();
        }

        private void btn_hosts_Click(object sender, EventArgs e)
        {
            var hostFile = new HostFile();
            hostFile.Show();
        }

        private void btn_dynamodb_toggle_Click(object sender, EventArgs e)
        {
            if (Dynamodb.status == "stopped")
            {
                Dynamodb.Start();
                btn_dynamodb_toggle.Text = @"Stop";
            }
            else
            {
                Dynamodb.Stop();
                btn_dynamodb_toggle.Text = @"Start";
            }
        }
    }
}