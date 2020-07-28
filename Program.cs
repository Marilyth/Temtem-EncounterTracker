using System;
using System.Threading.Tasks;
using System.Linq;

namespace Temtem_EncounterTracker
{
    class Program
    {
        public static Encounter encounter;

        static void Main(string[] args)
        {
            UserInput();
            while (true)
            {
                try
                {
                    Start().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static async Task Start()
        {
            encounter = Encounter.Load();
            Console.WriteLine("Loaded encounters!");
            await DrawEncounterTable();
            encounter.OnTemtemEncountered += WriteEncounter;
            await encounter.CheckForEncounters();
        }

        public static async Task WriteEncounter(string temtem, int counter)
        {
            await DrawEncounterTable();
            Console.WriteLine($"\nNew Encounter! ({temtem})");
        }

        public static async Task DrawEncounterTable()
        {
#if !DEBUG
                Console.Clear();
#endif

            Console.WriteLine(String.Format("{0, -20} | {1, -13} | {2, -23}", "Temtem", "Encounters", "Last Encounter"));
            Console.WriteLine("------------------------------------------------------------------");
            int current = 0;
            foreach (var temtem in encounter.Encounters.OrderByDescending(x => x.Value.LastEncounter))
            {
                if (current == RowChosen) Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{temtem.Key,-20} | {temtem.Value.HowOften,-6} {(temtem.Value.HowOftenToday > 0 ? $"(+{temtem.Value.HowOftenToday})" : ""),-6} | {temtem.Value.LastEncounter,-23} UTC");
                if (current++ == RowChosen) Console.BackgroundColor = ConsoleColor.Black;
            }
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"{"Total",-20} | {encounter.Encounters.Sum(x => x.Value.HowOften),-6} {(encounter.Encounters.Sum(x => x.Value.HowOftenToday) > 0 ? $"(+{encounter.Encounters.Sum(x => x.Value.HowOftenToday)})" : ""),-6} | ");
            Console.WriteLine("\n[W] [S] to navigate between rows, [A] [D] to change the encounter value.");
        }

        public static int RowChosen;
        public static async Task UserInput()
        {
#if !DEBUG
            RowChosen = -1;
            while (true)
            {
                try
                {
                    while (!Console.KeyAvailable)
                    {
                        await Task.Delay(200);
                    }
                    var key = Console.ReadKey(true);

                    switch (key.KeyChar)
                    {
                        case 'w':
                            if (RowChosen > 0) RowChosen--;
                            else RowChosen = encounter.Encounters.Count - 1;
                            break;
                        case 's':
                            if (RowChosen < encounter.Encounters.Count - 1) RowChosen++;
                            else RowChosen = 0;
                            break;
                        case 'a':
                            var temtemName = encounter.Encounters.OrderByDescending(x => x.Value.LastEncounter).ToArray()[RowChosen].Key;
                            if (encounter.Encounters[temtemName].HowOften <= 1) encounter.Encounters.Remove(temtemName);
                            else
                            {
                                encounter.Encounters[temtemName].HowOften--;
                                encounter.Encounters[temtemName].HowOftenToday--;
                            }
                            break;
                        case 'd':
                            temtemName = encounter.Encounters.OrderByDescending(x => x.Value.LastEncounter).ToArray()[RowChosen].Key;
                            encounter.Encounters[temtemName].HowOften++;
                            encounter.Encounters[temtemName].HowOftenToday++;
                            break;
                        default:
                            RowChosen = -1;
                            break;
                    }
                    await DrawEncounterTable();
                }
                catch
                {

                }
            }
#endif
        }
    }
}
