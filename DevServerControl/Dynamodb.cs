﻿using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace DevServerControl
{
    public class Dynamodb
    {
        public static string path = "C:\\dynamodb_local_latest\\";

        public static string status = "stopped";

        private static Process _cmd = new Process();

        public static void Start()
        {
            //Test if data dir exist. If not create it
            if (!Directory.Exists(path + "data\\"))
            {
                Form1.AppendText(Form1.tbx_log, $"Data directory not found! Creating {path}data\\", 10,
                    Color.LightCoral,
                    true);
                Directory.CreateDirectory(path + "data\\");
            }
            else
            {
                Form1.AppendText(Form1.tbx_log, $"Data directory set to {path}data!", 10, Color.LightCoral, true);
            }

            Form1.AppendText(Form1.tbx_log, $"Starting DynamoDB local server...", 10, Color.Gray, false);

            _cmd = new Process
            {
                StartInfo =
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "java.exe",
                    Arguments =
                        $"-D\"java.library.path={path}DynamoDBLocal_lib\" " +
                        $"-jar {path}DynamoDBLocal.jar " +
                        $"-dbPath {path}data\\ " +
                        "-sharedDb"
                }
            };
            _cmd.Start();
            status = "started";
            Form1.AppendText(Form1.tbx_log, $"DynamoDB local server started", 10, Color.Black, false);
        }

        public static void Stop()
        {
            Form1.AppendText(Form1.tbx_log, $"Stopping DynamoDB local server...", 10, Color.Gray, false);
            _cmd.Kill();
            status = "stopped";
            Form1.AppendText(Form1.tbx_log, $"DynamoDB local server stopped", 10, Color.Black, false);
        }
    }
}