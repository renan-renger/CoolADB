using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoolADB;

namespace TestbedConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AdbPowershellVersion client = new AdbPowershellVersion()
            {
                adbPath = Path.Combine(Environment.CurrentDirectory, "lib", "adb.exe")
            };

            client.SendCommand(AdbPowershellVersion.commands.StartServer);

            Console.WriteLine(client.SendCommand(AdbPowershellVersion.commands.ListDevices));

            Directory.GetFiles("D:\\Google Drive\\Pessoal\\Imagens\\Wallpapers\\Casual Overwatch\\").ToList().ForEach(fileName =>
            {
                client.SendCommand(
                AdbPowershellVersion.commands.Push,
                $" \"{fileName}\" /storage/emulated/0/Pictures/Overwatch/");
            });            

            client.SendCommand(
                AdbPowershellVersion.commands.Pull,
                " /storage/emulated/0/ D:\\AdbPull");

            //client.SendCommand(AdbPowershellVersion.commands.Reboot);

            client.SendCommand(AdbPowershellVersion.commands.KillServer);

            //client.Devices().ForEach(device => Console.WriteLine(device));

            //client.Reboot(AdbCmdVersion.BootState.System);

            //Console.WriteLine(client.Devices());

            Console.ReadLine();
        }
    }
}
