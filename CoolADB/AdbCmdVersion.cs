﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CoolADB
{
    public partial class AdbCmdVersion
    {
        // ----------------------------------------- Adb.exe path, leave blank if in same directory as app or included in PATH
        private string _adbPath;

        public string adbPath
        {
            get { return _adbPath; }
            set
            {
                _adbPath = File.Exists(value) ? value : "adb";
            }
        }

        // ----------------------------------------- Create our emulated shell here and assign events

        // Create a background thread an assign work event to our emulated shell method
        BackgroundWorker CMD = new BackgroundWorker();

        public AdbCmdVersion()
        {
            CMD.DoWork += new DoWorkEventHandler(cmdSend);
        }

        // Needed data types for our emulated shell
        string Command = "";

        // Create an emulated shell for executing commands
        private void cmdSend(object sender, DoWorkEventArgs e)
        {
            
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "cmd.exe",
                Arguments = "/C \"" + Command + "\""
            };

            using(Process process = Process.Start(startInfo))
            {
                if (Command.StartsWith("\"" + adbPath + "\" logcat"))
                    return;

                if (!process.WaitForExit(5000))
                    process.Kill();

                Output = process.StandardOutput.ReadToEnd();
            }
        }

        // Send a command to emulated shell
        private void SendCommand(string command)
        {
            CMD.WorkerSupportsCancellation = true;
            Command = command;

            CMD.RunWorkerAsync();
            while (CMD.IsBusy)
            {
                Thread.Sleep(10);
            }
        }

        // Bootstate for rebooting
        public enum bootState
        {
            System, Bootloader, Recovery
        }

        public enum commands
        {
            Connect,
            Disconnect,
            StartServer,
            KillServer,
            ListDevices,
            Reboot,
            RebootRecovery,
            RebootBootloader,
            Execute,
            ExecuteAsRoot,
            Remount,
            //TODO
            Push,
            Pull,
            Install,
            ForceInstall,
            Uninstall,
            Backup,
            Restore,
            Logcat,
            LogcatOverwrite
        }

        public string SendCommandExposed(commands commandToSend, List<string> parameters)
        {
            var commandText = string.Empty;

            switch (commandToSend)
            {
                case commands.Connect:
                    commandText = $"connect {parameters.FirstOrDefault() }";
                    break;
                case commands.Disconnect:
                    commandText = $"disconnect {parameters.FirstOrDefault() }";
                    break;
                case commands.StartServer:
                    commandText = $"start-server";
                    break;
                case commands.KillServer:
                    commandText = $"kill-server";
                    break;
                case commands.ListDevices:
                    commandText = $"devices";
                    break;
                case commands.Reboot:
                    commandText = $"reboot";
                    break;
                case commands.RebootBootloader:
                    commandText = $"reboot bootloader";
                    break;
                case commands.RebootRecovery:
                    commandText = $"reboot recovery";
                    break;
                case commands.Execute:
                    commandText = $"shell {parameters.FirstOrDefault() }";
                    break;
                case commands.ExecuteAsRoot:
                    commandText = $"shell su -c  \"{parameters.FirstOrDefault() }\"";
                    break;
                case commands.Remount:
                    commandText = $"shell su -c \"mount -o rw,remount /system\"";
                    break;
                case commands.Push:
                    commandText = $"push { string.Join(" ", parameters.Select(x => string.Concat("\"", x, "\""))) }";
                    break;
                case commands.Pull:
                    commandText = $"pull {parameters}";
                    break;
                case commands.Install:
                case commands.ForceInstall:
                    commandText = $"install \"{parameters}\"";
                    break;
                case commands.Uninstall:
                    commandText = $"uninstall \"{parameters}\"";
                    break;
                case commands.Backup:
                    commandText = $"backup \"{parameters}\"";
                    break;
                case commands.Restore:
                    commandText = $"backup \"{parameters}\"";
                    break;
                case commands.Logcat:
                    commandText = $"logcat >> \"{parameters}\"";
                    break;
                case commands.LogcatOverwrite:
                    commandText = $"logcat > \"{parameters}\"";
                    break;

            }

            SendCommand($"\"{adbPath}\" {commandText}");

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
            if (asroot)
                SendCommand("\"" + adbPath + "\" shell su -c \"" + command + "\"");
            else
                SendCommand("\"" + adbPath + "\" shell " + command);
        }

        public void Remount()
        {
            SendCommand("\"" + adbPath + "\" shell su -c \"mount -o rw,remount /system\"");
        }

        public void Reboot(bootState boot)
        {
            switch (boot)
            {
                case bootState.System:
                    SendCommand($"\"{ adbPath }\" reboot");
                    break;
                case (bootState.Bootloader):
                    SendCommand($"\"{ adbPath }\" reboot bootloader\"");
                    break;
                case (bootState.Recovery):
                    SendCommand($"\"{ adbPath }\" reboot recovery");
                    break;
            }
        }

        public void Push(string input, string output)
        {
            try
            {
                SendCommand("\"" + adbPath + "\" push \"" + input + "\" \"" + output + "\"");
            }
            catch
            {
                try
                {
                    SendCommand("\"" + adbPath + "\" push \"" + input.Replace("/", "\\") + "\" \"" + output + "\"");
                }
                catch { }
            }
        }

        public void Pull(string input, string output)
        {
            if (output != null && !string.IsNullOrWhiteSpace(output))
                try
                {
                    SendCommand("\"" + adbPath + "\" pull \"" + input + "\" \"" + output + "\"");
                }
                catch
                {
                    try
                    {
                        SendCommand("\"" + adbPath + "\" pull \"" + input + "\" \"" + output.Replace("/", "\\") + "\"");
                    }
                    catch { }
                }
            else
                try
                {
                    SendCommand("\"" + adbPath + "\" pull \"" + input + "\"");
                }
                catch { }
        }

        public void Install(string application)
        {
            try
            {
                SendCommand("\"" + adbPath + "\" install \"" + application + "\"");
            }
            catch
            {
                try
                {
                    SendCommand("\"" + adbPath + "\" install \"" + application.Replace("/", "\\") + "\"");
                }
                catch { }
            }
        }

        public void Uninstall(string packageName)
        {
            SendCommand("\"" + adbPath + "\" uninstall \"" + packageName + "\"");
        }

        public void Backup(string backupPath, string backupArgs)
        {
            if (backupArgs != null && !string.IsNullOrWhiteSpace(backupArgs))
                SendCommand("\"" + adbPath + "\" backup \"" + backupPath + "\" " + "\"" + backupArgs + "\"");
            else
                SendCommand("\"" + adbPath + "\" backup \"" + backupPath + "\"");
        }

        public void Restore(string backupPath)
        {
            try
            {
                SendCommand("\"" + adbPath + "\" restore \"" + backupPath + "\"");
            }
            catch
            {
                try
                {
                    SendCommand("\"" + adbPath + "\" restore \"" + backupPath.Replace("/", "\\") + "\"");
                }
                catch { }
            }
        }

        public void Logcat(string logPath, bool overWrite)
        {
            if (overWrite == true)
                try
                {
                    SendCommand("\"" + adbPath + "\" logcat > \"" + logPath + "\"");
                }
                catch
                {
                    try
                    {
                        SendCommand("\"" + adbPath + "\" logcat > \"" + logPath.Replace("/", "\\") + "\"");
                    }
                    catch { }
                }
            else
                try
                {
                    SendCommand("\"" + adbPath + "\" logcat >> \"" + logPath + "\"");
                }
                catch
                {
                    try
                    {
                        SendCommand("\"" + adbPath + "\" logcat >> \"" + logPath.Replace("/", "\\") + "\"");
                    }
                    catch { }
                }
        }
    }
}
