using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Temtem_EncounterTracker
{
    public enum Columns
    {
        Name, Encounters, Date
    }

    class Program
    {
        public static Encounter encounter;
        public static int RowChosen;
        public static Columns SortBy;
        public static AspectRatio Ratio;

        static void Main(string[] args)
        {
            Console.WriteLine($"Currently running version {Updater.Version}");
            Updater.CheckForUpdate().Wait();
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

        public static HashSet<string> currentEncounter = new HashSet<string>();
        public static async Task DrawEncounterTable()
        {
#if !DEBUG
                Console.Clear();
#endif
            Console.WriteLine(String.Format("        {0, -20}   {1, -13}   {2, -23}", "[1]", "[2]", "[3]"));
            Console.WriteLine(String.Format("Index | {0, -20} | {1, -13} | {2, -23}", "Temtem", "Encounters", "Last Encounter"));
            Console.WriteLine("--------------------------------------------------------------------------");

            var sortedEncounter = encounter.GetSortedEncounters(SortBy);
            for(int i = Math.Max(0, RowChosen - 9); i < Math.Min(sortedEncounter.Count, Math.Max(10, RowChosen+1)); i++){
                if (i == RowChosen)
                    Console.BackgroundColor = ConsoleColor.DarkGray;

                Console.WriteLine($"#{i+1,-4} | {sortedEncounter[i].Key,-20} | {sortedEncounter[i].Value.HowOften,-6} {(sortedEncounter[i].Value.HowOftenToday > 0 ? $"(+{sortedEncounter[i].Value.HowOftenToday})" : ""),-6} | {sortedEncounter[i].Value.LastEncounter,-23} UTC");

                if (i == RowChosen)
                    Console.BackgroundColor = ConsoleColor.Black;
            }
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"      | {"Total",-20} | {encounter.Encounters.Sum(x => x.Value.HowOften),-6} {(encounter.Encounters.Sum(x => x.Value.HowOftenToday) > 0 ? $"(+{encounter.Encounters.Sum(x => x.Value.HowOftenToday)})" : ""),-6} |");
            encounter.WriteEncounterRate();

            foreach (var chosenTemtem in currentEncounter)
            {
                if (!string.IsNullOrEmpty(chosenTemtem) && encounter.Temtems.ContainsKey(chosenTemtem))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var info = encounter.Temtems[chosenTemtem];

                    var attackInfos = encounter.GetAttackValues(info.types);
                    Console.WriteLine($"\n{info.name} has the types: {string.Join(", ", info.types)}\n" +
                                      $"Good elements: {string.Join("| ", attackInfos.Where(x => x.Value > 1.1).Select(x => $"{x.Key} {x.Value}x "))}\n" +
                                      $"Bad elements: {string.Join("| ", attackInfos.Where(x => x.Value < 0.9).Select(x => $"{x.Key} {x.Value}x "))}\n");// +
                                      //$"\nCan be found in: {string.Join("\n", info.locations.Select(x => $"{x.island} - {x.location} ({x.frequency} levels {x.level})"))}");

                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }

            Console.WriteLine("\n[W] [S] to navigate between rows, [A] [D] to change the encounter value.\n[I] to get detailed information of the current encounter.");
        }

        public static async Task UserInput()
        {
            SortBy = Columns.Date;
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
                            currentEncounter = new HashSet<string>(){encounter.GetSortedEncounters(SortBy)[RowChosen].Key};
                            break;
                        case 's':
                            if (RowChosen < encounter.Encounters.Count - 1) RowChosen++;
                            else RowChosen = 0;
                            currentEncounter = new HashSet<string>(){encounter.GetSortedEncounters(SortBy)[RowChosen].Key};
                            break;
                        case 'a':
                            var temtemName = encounter.GetSortedEncounters(SortBy)[RowChosen].Key;
                            if (encounter.Encounters[temtemName].HowOften <= 1) encounter.Encounters.Remove(temtemName);
                            else
                            {
                                encounter.Encounters[temtemName].HowOften--;
                                if(encounter.Encounters[temtemName].HowOftenToday > 0)
                                    encounter.Encounters[temtemName].HowOftenToday--;
                                Encounter.Save(encounter);
                            }
                            break;
                        case 'd':
                            temtemName = encounter.GetSortedEncounters(SortBy)[RowChosen].Key;
                            encounter.Encounters[temtemName].HowOften++;
                            encounter.Encounters[temtemName].HowOftenToday++;
                            Encounter.Save(encounter);
                            break;
                        case 'i':
                            currentEncounter = new HashSet<string>();
                            string temtemA = encounter.GetClosestMatch(encounter.temtem.GetScreenText(await encounter.temtem.GetTemtem(true)).Replace("\n", "").Split(" ").First(), out double wasFoundA);
                            string temtemB = encounter.GetClosestMatch(encounter.temtem.GetScreenText(await encounter.temtem.GetTemtem(false)).Replace("\n", "").Split(" ").First(), out double wasFoundB);
                            if(wasFoundA < 1)currentEncounter.Add(temtemA);
                            if(wasFoundB < 1)currentEncounter.Add(temtemB);
                            break;
                        case '1':
                            SortBy = Columns.Name;
                            break;
                        case '2':
                            SortBy = Columns.Encounters;
                            break;
                        case '3':
                            SortBy = Columns.Date;
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
