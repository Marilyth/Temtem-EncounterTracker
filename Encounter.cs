using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Tesseract;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Temtem_EncounterTracker
{
    public class Encounter
    {
        public event MainEventHandler OnTemtemEncountered;
        public delegate Task MainEventHandler(string Temtem, int counter);
        public Dictionary<string, EncounterInfo> Encounters;

        private Encounter()
        {
            Encounters = new Dictionary<string, EncounterInfo>();
        }

        public async Task CheckForEncounters()
        {
            var temtem = new TemtemWindow();

            while (true)
            {
                if (temtem.IsTemtemActive())
                {
                    bool temFound = false;
                    bool[] bools = { true, false };

                    //Check multiple times in case screen flashed
                    bool[] wasFound = {false, false};
                    for (int i = 0; i < 20; i++)
                    {
                        for(int j = 0; j < bools.Length; j++)
                        {
                            if(wasFound[j]) continue;

                            var temtemType = temtem.GetScreenText(await temtem.GetTemtem(bools[j])).Replace("\n", "");
                            if (string.IsNullOrEmpty(temtemType) || temtemType.Length <= 2) continue;
                            temFound = true;
                            wasFound[j] = true;

                            if (!Encounters.ContainsKey(temtemType))
                                Encounters[temtemType] = new EncounterInfo();

                            Encounters[temtemType].HowOften += 1;
                            Encounters[temtemType].HowOftenToday += 1;
                            Encounters[temtemType].LastEncounter = DateTime.UtcNow;
                            //await temtemEncountered(temtemType);
                            await Task.Delay(100);
                        }
                        if(wasFound.All(x => x)) break;
                    }

                    if (temFound)
                    {
                        await Program.DrawEncounterTable();
                        Save(this);

                        int counter = 0;
                        //Wait for encounter to end
                        while (true)
                        {
                            if (temtem.IsTemtemActive())
                            {
                                bool isEmpty = true;
                                foreach (bool b in bools)
                                {
                                    var temtemType = temtem.GetScreenText(await temtem.GetTemtem(b));
                                    if (!string.IsNullOrEmpty(temtemType)) isEmpty = false;
                                }
                                if (isEmpty) counter++;
                                else counter = 0;
                                if(counter == 10) break;
                                await Task.Delay(100);
                            }
                        }

                        await Task.Delay(3000);
                    }

                    await Task.Delay(200);
                }
            }
        }

        private async Task temtemEncountered(string Temtem)
        {
            if (OnTemtemEncountered != null)
                await OnTemtemEncountered(Temtem, Encounters[Temtem].HowOften);
            Save(this);
        }

        public static void Save(Encounter toSave)
        {
            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");

            var json = JsonConvert.SerializeObject(toSave);
            using (StreamWriter sw = new StreamWriter(new FileStream("data//Encounters.json", FileMode.OpenOrCreate)))
                sw.WriteLine(json);
        }

        public static Encounter Load()
        {
            try
            {
                using (StreamReader sr = new StreamReader(new FileStream("data//Encounters.json", FileMode.Open)))
                    return JsonConvert.DeserializeObject<Encounter>(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                return new Encounter();
            }
        }
    }

    public class EncounterInfo
    {
        public DateTime LastEncounter;
        public int HowOften;

        [JsonIgnore]
        public int HowOftenToday;
    }
}