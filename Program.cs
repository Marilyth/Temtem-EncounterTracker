using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Tesseract;
using System.IO;

namespace Temtem_EncounterTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        public static async Task Start()
        {
            Console.SetError(TextWriter.Null);
            
            var encounter = Encounter.Load();
            Console.WriteLine("Loaded encounters!");
            foreach(var temtem in encounter.Encounters){
                Console.WriteLine($"Encountered {temtem.Key} {temtem.Value} times.");
            }
            encounter.OnTemtemEncountered += WriteEncounter;
            await encounter.CheckForEncounters();
        }

        public static async Task WriteEncounter(string temtem, int counter)
        {
            Console.WriteLine($"New Encounter! ({temtem} {counter} times)");
        }
    }
}
