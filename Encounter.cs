using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;

namespace Temtem_EncounterTracker
{
    public class Encounter
    {
        [JsonIgnore]
        public Dictionary<string, Temtem> Temtems;
        [JsonIgnore]
        public Dictionary<string, Dictionary<string, double>> AttackValues;
        public event MainEventHandler OnTemtemEncountered;
        public delegate Task MainEventHandler(string Temtem, int counter);
        public Dictionary<string, EncounterInfo> Encounters;
        [JsonIgnore]
        public TemtemWindow temtem;

        private Encounter()
        {
            Encounters = new Dictionary<string, EncounterInfo>();

            HttpClient client = new HttpClient();
            string result = client.GetStringAsync("https://temtem-api.mael.tech/api/temtems").Result;
            var tempTems = JsonConvert.DeserializeObject<List<Temtem>>(result);
            result = client.GetStringAsync("https://temtem-api.mael.tech/api/weaknesses").Result;
            AttackValues = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(result);
            Temtems = tempTems.ToDictionary(x => x.name);
            WriteTesseractWordlist();
        }

        public async Task CheckForEncounters()
        {
            temtem = new TemtemWindow();

            while (true)
            {
                if (temtem.IsTemtemActive())
                {
                    string temtemA = null, temtemB = null;

                    while (temtem.IsInEncounter())
                    {
                        bool updateUI = false;

                        if (temtemA == null)
                        {
                            var text = temtem.GetScreenText(await temtem.GetTemtem(true)).Replace("\n", "").Split(" ").First();
                            if (NameIsLegit(text)){
                                temtemA = text;
                                AddEncounter(temtemA);
                                Program.currentEncounter = new HashSet<string>{temtemA, temtemB};
                                updateUI = true;
                            }
                        }

                        if (temtemB == null)
                        {
                            var text = temtem.GetScreenText(await temtem.GetTemtem(false)).Replace("\n", "").Split(" ").First();
                            if (NameIsLegit(text)){
                                temtemB = text;
                                AddEncounter(temtemB);
                                Program.currentEncounter = new HashSet<string>{temtemA, temtemB};
                                updateUI = true;
                            }
                        }
                        
                        if(updateUI){
                            await Program.DrawEncounterTable();
                            Save(this);
                        }
                        await Task.Delay(100);
                    }

                    await Task.Delay(200);
                }
            }
        }

        public void AddEncounter(string temtemName)
        {
            if (!Encounters.ContainsKey(temtemName))
                Encounters[temtemName] = new EncounterInfo();

            Encounters[temtemName].HowOften += 1;
            Encounters[temtemName].HowOftenToday += 1;
            Encounters[temtemName].LastEncounter = DateTime.UtcNow;
        }

        public bool NameIsLegit(string temtemName)
        {
            return Temtems.ContainsKey(temtemName ?? "");
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

        public Dictionary<string, double> GetAttackValues(List<string> defendingElements)
        {
            Dictionary<string, double> attackValues = new Dictionary<string, double>();
            foreach (var attackingElement in AttackValues.Keys)
            {
                double attackValue = 1;

                foreach (var element in defendingElements)
                {
                    attackValue *= AttackValues[attackingElement][element];
                }
                attackValues[attackingElement] = attackValue;
            }

            return attackValues;
        }

        public void WriteTesseractWordlist(){
            using (StreamWriter sw = new StreamWriter(new FileStream("tessdata//eng.user-words", FileMode.OpenOrCreate))){
                sw.WriteLine(string.Join("\n", Temtems.Keys));
                sw.WriteLine("Temtem");
                sw.WriteLine("Early");
                sw.Write("Access");
            }
        }

        public List<KeyValuePair<string, EncounterInfo>> GetSortedEncounters(Columns column){
            switch(column){
                case Columns.Date: 
                    return Encounters.OrderByDescending(x => x.Value.LastEncounter).ToList();
                case Columns.Name:
                    return Encounters.OrderByDescending(x => x.Key).ToList();
                default:
                    return Encounters.OrderByDescending(x => x.Value.HowOften).ToList();
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