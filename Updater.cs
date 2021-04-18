using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;

namespace Temtem_EncounterTracker
{
    public class Updater
    {
        public static readonly string Version = "0.2.6";

        public static async Task CheckForUpdate()
        {
            Console.WriteLine("Checking for updates!");
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, "request");
                var result = JsonConvert.DeserializeObject<dynamic>(client.DownloadString("https://api.github.com/repos/Marilyth/Temtem-EncounterTracker/releases"));
                string latestVersion = result[0]["tag_name"];
                if (!latestVersion.Equals(Version))
                {
                    Console.Write($"New release found (version {latestVersion})\nWould you like to update? (y/n) ");
                    var input = Console.ReadKey(true).KeyChar;
                    if(input.Equals('y') || input.Equals('Y')){
                        Console.WriteLine("Updating...");
                        await DownloadRelease(result[0]["assets"][0]["browser_download_url"].ToString());
                        ReplaceAndRestart();
                    }
                }
                else
                {
                    Console.WriteLine("No new release found!");
                }
            }
        }

        public static async Task DownloadRelease(string url)
        {
            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += DownloadProgressCallback;

                await client.DownloadFileTaskAsync(new Uri(url), "NewRelease.temp");
                Console.WriteLine("Download completed, extracting file...");
                ZipFile.ExtractToDirectory("NewRelease.temp", "update");
                File.Delete("NewRelease.temp");
                Console.WriteLine("Restarting...");
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            Console.WriteLine("Downloaded {0} of {1} bytes. {2} % complete...",
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage);
        }

        public static void ReplaceAndRestart()
        {
            Task.Run(()=>{
                Task.Delay(1000).Wait();
                Environment.Exit(1);
            });

            ExecuteCommand("ping -n 5 localhost> nul && RMDIR /S /Q  tessdata & robocopy update/publish ./ /is /it /E /S /MOVE & RMDIR /S /Q update & start Temtem-EncounterTracker.exe");
        }

        public static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = true;

            var process = Process.Start(processInfo);

            process.WaitForExit();
            process.Close();
        }
    }
}