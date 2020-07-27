using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Tesseract;
using System.Linq;
using System.IO;

namespace Temtem_EncounterTracker
{
    class Program
    {
        public static Encounter encounter;

        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        public static async Task Start()
        {
            encounter = Encounter.Load();
            Console.WriteLine("Loaded encounters!");
            DrawEncounterTable();
            encounter.OnTemtemEncountered += WriteEncounter;
            await encounter.CheckForEncounters();
        }

        public static async Task WriteEncounter(string temtem, int counter)
        {
            DrawEncounterTable();
            Console.WriteLine($"\nNew Encounter! ({temtem})");
        }

        public static async Task DrawEncounterTable(){
            //Console.Clear();
            string table = String.Format("{0, -20} | {1, -10} | {2, -23}\n", "Temtem", "Encounters", "Last Encounter");
            table += "------------------------------------------------------------------\n";
            foreach(var temtem in encounter.Encounters.OrderByDescending(x => x.Value.LastEncounter)){
                table += $"{temtem.Key, -20} | {temtem.Value.HowOften, -10} | {temtem.Value.LastEncounter, -23} UTC\n";
            }
            Console.WriteLine(table);
        }
    }
}
