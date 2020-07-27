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
        public Dictionary<string, int> Encounters;

        private Encounter()
        {
            Encounters = new Dictionary<string, int>();
        }

        public async Task CheckForEncounters()
        {
            var temtem = new TemtemWindow();

            while (true)
            {
                string text = "";
                while(!text.Contains("An untamed") || !text.Contains("found you"))
                {
                    //await temtem.WaitForEncounter();
                    //await Task.Delay(4000);
                    if(temtem.IsTemtemActive()){
                        var bytes = await temtem.GetEncounterScreenshot();
                        text = temtem.GetScreenText(bytes).Replace("Tound", "found");
                    }
                    await Task.Delay(200);
                }

                do{
                    var skipFirst = text.Split("n untamed ")[1];
                    var temtemType = skipFirst.Split(" ").First(x => x.Length > 1);
                    if(temtemType.Equals("team")) continue;
                    bool isTeam = skipFirst.Contains("team");

                    if (!Encounters.ContainsKey(temtemType))
                        Encounters[temtemType] = 0;

                    Encounters[temtemType] += isTeam ? 2 : 1;
                    await temtemEncountered(temtemType);
                    text = text.Split(temtemType)[1];
                }while(text.Contains("and an"));

                //await temtem.WaitForEncounter();
                //await Task.Delay(1500);
                await Task.Delay(2000);
                text = "";
            }
        }

        private async Task temtemEncountered(string Temtem)
        {
            if (OnTemtemEncountered != null)
                await OnTemtemEncountered(Temtem, Encounters[Temtem]);
            Save(this);
        }

        public static void Save(Encounter toSave)
        {
            if(!Directory.Exists("data"))
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
}