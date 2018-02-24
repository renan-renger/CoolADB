using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace CoolADB
{
    public partial class ADBClient : Component
    {
        // ----------------------------------------- Adb.exe path, leave blank if in same directory as app or included in PATH
        private string _adbPath;

        public string adbPath
        {
            get { return _adbPath; }
            set
            {
                _adbPath = File.Exists(value) ? value : "adb";
                //if (File.Exists(value)) adbPath = value;
                //else adbPath = "\"" + adbPath + "\"";
            }
        }

        // ----------------------------------------- Create our emulated shell here and assign events

        // Create a background thread an assign work event to our emulated shell method
        BackgroundWorker CMD = new BackgroundWorker();
        private Process Shell;

        public ADBClient()
        {
            CMD.DoWork += new DoWorkEventHandler(cmdSend);
        }

        // Needed data types for our emulated shell
        string Command = "";

        //private string RunScript(string scriptText)
        //{
        //    // create Powershell runspace

        //    Runspace runspace = RunspaceFactory.CreateRunspace();

        //    // open it

        //    runspace.Open();

        //    // create a pipeline and feed it the script text

        //    Pipeline pipeline = runspace.CreatePipeline();
        //    pipeline.Commands.AddScript(scriptText);

        //    // add an extra command to transform the script
        //    // output objects into nicely formatted strings

        //    // remove this line to get the actual objects
        //    // that the script returns. For example, the script

        //    // "Get-Process" returns a collection
        //    // of System.Diagnostics.Process instances.

        //    pipeline.Commands.Add("Out-String");

        //    // execute the script

        //    Collection < psobject /> results = pipeline.Invoke();

        //    // close the runspace

        //    runspace.Close();

        //    // convert the script result into a single string

        //    StringBuilder stringBuilder = new StringBuilder();
        //    foreach (PSObject obj in results)
        //    {
        //        stringBuilder.AppendLine(obj.ToString());
        //    }

        //    return stringBuilder.ToString();
        //}

        // Create an emulated shell for executing commands
        private void cmdSend(object sender, DoWorkEventArgs e)
        {
            Process process = new Process();
            Shell = process;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "cmd.exe",
                Arguments = "/C \"" + Command + "\""
            };
            process.StartInfo = startInfo;
            process.Start();
            if (Command.StartsWith("\"" + adbPath + "\" logcat")) return;
            process.WaitForExit();
            Output = process.StandardOutput.ReadToEnd();
            //Complete = true;
        }

        // Send a command to emulated shell
        private void SendCommand(string command)
        {
            CMD.WorkerSupportsCancellation = true;
            Command = command;
            
            CMD.RunWorkerAsync();
            while (CMD.IsBusy)
            {
                //var busy = CMD.IsBusy;
                Thread.Sleep(10);
            }
            //Complete = false;
        }

        // Sleep until output
        public void Sleep(int milliseconds)
        {
            DateTime delayTime = DateTime.Now.AddMilliseconds(milliseconds);
            while (DateTime.Now < delayTime)
            {
                Application.DoEvents();
            }
        }

        // Bootstate for rebooting
        public enum BootState
        {
            System, Bootloader, Recovery
        }

        public enum Commands
        {
            Connect,
            Disconnect,
            StartServer,
            KillServer,
            ListDevices
        }

        public string SendCommandExposed(Commands commandToSend)
        {
            var commandText = string.Empty;

            switch (commandToSend)
            {
                case Commands.StartServer:
                    commandText = "start-server";
                    break;
                case Commands.KillServer:
                    commandText = "kill-server";
                    break;
                case Commands.ListDevices:
                    commandText = "devices";
                    break;

            }

            SendCommand($" &\"{adbPath}\" {commandText}");

            return Output;
        }

        // ----------------------------------------- Allow public modifiers to get output

        public string Output { get; private set; }

        // ----------------------------------------- Functions

        public void Connect(string ip)
        {
            SendCommand("\"" + adbPath + "\" connect " + ip);
        }

        public void Disconnect(decimal ip)
        {
            SendCommand("\"" + adbPath + "\" disconnect " + ip);
        }

        public void StartServer()
        {
            SendCommand($"& \"{ adbPath }\" start-server");
        }

        public void KillServer()
        {
            SendCommand("\"" + adbPath + "\" kill-server");
        }

        public List<string> Devices()
        {
            SendCommand("\"" + adbPath + "\" devices");

            string[] outLines = Output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            return outLines.Where(stdout => !stdout[0].Equals('*')).Skip(1).ToList();
        }

        public void Execute(string command, bool asroot)
        {
            if (asroot) SendCommand("\"" + adbPath + "\" shell su -c \"" + command + "\"");
            else SendCommand("\"" + adbPath + "\" shell " + command);
        }

        public void Remount()
        {
            SendCommand("\"" + adbPath + "\" shell su -c \"mount -o rw,remount /system\"");
        }

        public void Reboot(BootState boot)
        {
            switch (boot)
            {
                case BootState.System:
                    SendCommand($"& \"{ adbPath }\" reboot");
                    break;
                case (BootState.Bootloader):
                    SendCommand($"& \"{ adbPath }\" reboot bootloader\"");
                    break;
                case (BootState.Recovery):
                    SendCommand($"& \"{ adbPath }\" reboot recovery");
                    break;
            }
        }

        public void Push(string input, string output)
        {
            try { SendCommand("\"" + adbPath + "\" push \"" + input + "\" \"" + output + "\""); } catch { try { SendCommand("\"" + adbPath + "\" push \"" + input.Replace("/", "\\") + "\" \"" + output + "\""); } catch { } }
        }

        public void Pull(string input, string output)
        {
            if (output != null && !string.IsNullOrWhiteSpace(output)) try { SendCommand("\"" + adbPath + "\" pull \"" + input + "\" \"" + output + "\""); } catch { try { SendCommand("\"" + adbPath + "\" pull \"" + input + "\" \"" + output.Replace("/", "\\") + "\""); } catch { } }
            else try { SendCommand("\"" + adbPath + "\" pull \"" + input + "\""); } catch { }
        }

        public void Install(string application)
        {
            try { SendCommand("\"" + adbPath + "\" install \"" + application + "\""); } catch { try { SendCommand("\"" + adbPath + "\" install \"" + application.Replace("/", "\\") + "\""); } catch { } }
        }

        public void Uninstall(string packageName)
        {
            SendCommand("\"" + adbPath + "\" uninstall \"" + packageName + "\"");
        }

        public void Backup(string backupPath, string backupArgs)
        {
            if (backupArgs != null && !string.IsNullOrWhiteSpace(backupArgs)) SendCommand("\"" + adbPath + "\" backup \"" + backupPath + "\" " + "\"" + backupArgs + "\"");
            else SendCommand("\"" + adbPath + "\" backup \"" + backupPath + "\"");
        }

        public void Restore(string backupPath)
        {
            try { SendCommand("\"" + adbPath + "\" restore \"" + backupPath + "\""); } catch { try { SendCommand("\"" + adbPath + "\" restore \"" + backupPath.Replace("/", "\\") + "\""); } catch { } }
        }

        public void Logcat(string logPath, bool overWrite)
        {
            if (overWrite == true) try { SendCommand("\"" + adbPath + "\" logcat > \"" + logPath + "\""); } catch { try { SendCommand("\"" + adbPath + "\" logcat > \"" + logPath.Replace("/", "\\") + "\""); } catch { } }
            else try { SendCommand("\"" + adbPath + "\" logcat >> \"" + logPath + "\""); } catch { try { SendCommand("\"" + adbPath + "\" logcat >> \"" + logPath.Replace("/", "\\") + "\""); } catch { } }
        }
    }
}
