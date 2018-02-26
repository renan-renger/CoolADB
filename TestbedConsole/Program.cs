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
            //ADBClient client = new ADBClient()
            //{
            //    AdbPath = Path.Combine(Environment.CurrentDirectory, "lib", "adb.exe")
            //};

            //client.StartServer();

            //client.Devices().ForEach(x => Console.WriteLine(x));

            //Directory.GetFiles("D:\\Google Drive\\Pessoal\\Imagens\\Wallpapers\\Casual Overwatch\\").ToList().ForEach(fileName =>
            //{
            //    client.Push(fileName, "/storage/emulated/0/Pictures/Overwatch/");   
            //});

            //client.KillServer();

            //AdbCmdVersion client = new AdbCmdVersion()
            //{
            //    adbPath = Path.Combine(Environment.CurrentDirectory, "lib", "adb.exe")
            //};

            //client.StartServer();
            //client.Devices().ForEach(device => Console.WriteLine(device));


            //Directory.GetFiles("D:\\Google Drive\\Pessoal\\Imagens\\Wallpapers\\Casual Overwatch\\").ToList().ForEach(fileName =>
            //{
            //    client.SendCommandExposed(
            //    AdbCmdVersion.commands.Push,
            //    new List<string>()
            //    {
            //        fileName,
            //        "/storage/emulated/0/Pictures/Overwatch/"
            //    });
            //});

            //client.Reboot(AdbCmdVersion.bootState.System);

            AdbPowershellVersion client = new AdbPowershellVersion()
            {
                adbPath = Path.Combine(Environment.CurrentDirectory, "lib", "adb.exe")
            };

            client.SendCommand(AdbPowershellVersion.commands.StartServer, null);
            Console.WriteLine("Server up and running!");

            Console.WriteLine(client.SendCommand(AdbPowershellVersion.commands.ListDevices, null));

            Directory.GetFiles("D:\\Google Drive\\Pessoal\\Imagens\\Wallpapers\\Casual Overwatch\\").ToList().ForEach(fileName =>
            {
                client.SendCommand(
                AdbPowershellVersion.commands.Push,
                new List<string>()
                {
                    fileName,
                    "/storage/emulated/0/Pictures/Overwatch/"
                });
                Console.WriteLine($"Sent {fileName} to device!");
            });

            client.SendCommand(AdbPowershellVersion.commands.KillServer, null);
            Console.WriteLine("Server' dead and gone!");

            Console.ReadLine();
        }
    }
}
