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
        [JsonIgnore]
        public List<DateTime> EncountersLast10Minutes = new List<DateTime>();

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
            Console.WriteLine("Waiting for Temtem to start...");
            await temtem.WaitForTemtemStart();
            Console.WriteLine("Temtem process found!");

            Task.Run(() => HandleEncounterList());

            int[] textMargins = {70, 100, 130, 170};

            while (true)
            {
                if (temtem.IsTemtemActive())
                {
                    string temtemA = null, temtemB = null;
                    int loopCount = 0;
                    while (temtem.IsInEncounter())
                    {
                        bool updateUI = false;

                        if (temtemA == null)
                        {
                            var text = temtem.GetScreenText(await temtem.GetTemtem(true, textMargins[loopCount % 4], (loopCount / 4) % 2 == 1)).Replace("\n", "").Split(" ").First();
                            text = GetClosestMatch(text, out double accuracy);
                            if (accuracy >= 0.61)
                            {
                                //Console.WriteLine($"Found a {text} with {Math.Round(accuracy * 100, 2)}% certainty!");
                                temtemA = text;
                                AddEncounter(temtemA);
                                EncountersLast10Minutes.Add(DateTime.UtcNow);
                                Program.currentEncounter = new HashSet<string> { temtemA, temtemB };
                                updateUI = true;
                            }
                        }

                        if (temtemB == null)
                        {
                            var text = temtem.GetScreenText(await temtem.GetTemtem(false, textMargins[loopCount % 4], (loopCount / 4) % 2 == 1)).Replace("\n", "").Split(" ").First();
                            text = GetClosestMatch(text, out double accuracy);
                            if (accuracy >= 0.61)
                            {
                                //Console.WriteLine($"Found a {text} with {Math.Round(accuracy * 100, 2)}% certainty!");
                                temtemB = text;
                                AddEncounter(temtemB);
                                EncountersLast10Minutes.Add(DateTime.UtcNow);
                                Program.currentEncounter = new HashSet<string> { temtemA, temtemB };
                                updateUI = true;
                            }
                        }

                        if (updateUI)
                        {
                            await Program.DrawEncounterTable();
                            Save(this);
                        }
                        await Task.Delay(100);
                        loopCount++;
                    }

                    await Task.Delay(200);
                }
            }
        }

        public async Task HandleEncounterList(){
            while(true){
                for(int i = 0; i < EncountersLast10Minutes.Count; i++){
                    if((DateTime.UtcNow - EncountersLast10Minutes[i]).TotalMinutes > 10){
                        EncountersLast10Minutes.RemoveAt(i);
                        i--;
                    } else {
                        break;
                    }
                }
                WriteEncounterRate();
                await Task.Delay(5000);
            }
        }

        public void WriteEncounterRate(){
            double sumOfDistances = 0;
            double temtemPerHour = 0;
            if(EncountersLast10Minutes.Count > 1){
                for(int i = 1; i < EncountersLast10Minutes.Count; i++){
                    sumOfDistances += (EncountersLast10Minutes[i] - EncountersLast10Minutes[i-1]).TotalMinutes;
                }
                temtemPerHour = 60 / (sumOfDistances / (EncountersLast10Minutes.Count - 1));
            }
            Console.SetCursorPosition(47, Encounters.Count < 10 ? 4 + Encounters.Count : 14);
            Console.WriteLine($"{Math.Round(temtemPerHour, 2) + " Temtems per hour", -40}");
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

        public string GetClosestMatch(string temtemName, out double accuracy)
        {
            accuracy = 0;
            if (NameIsLegit(temtemName)){
                accuracy = 1;
                return temtemName;
            }

            string closestName = "";
            foreach (var name in Temtems.Keys)
            {
                if (temtemName.Length > 2)
                {
                    var relDistance = 1 - DamerauLevenshteinDistance(temtemName.Select(x => (int)x).ToArray(), name.Select(x => (int)x).ToArray(), 10) / (double)(name.Length > temtemName.Length ? name.Length : temtemName.Length);
                    if (relDistance > accuracy)
                    {
                        accuracy = relDistance;
                        closestName = name;
                    }
                    if(relDistance > 0.99) break;
                }
            }

            return closestName;
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

        public void WriteTesseractWordlist()
        {
            using (StreamWriter sw = new StreamWriter(new FileStream("tessdata//eng.user-words", FileMode.OpenOrCreate)))
            {
                sw.WriteLine(string.Join("\n", Temtems.Keys));
                sw.WriteLine("Temtem");
                sw.WriteLine("Early");
                sw.Write("Access");
            }
        }

        public List<KeyValuePair<string, EncounterInfo>> GetSortedEncounters(Columns column)
        {
            switch (column)
            {
                case Columns.Date:
                    return Encounters.OrderByDescending(x => x.Value.LastEncounter).ToList();
                case Columns.Name:
                    return Encounters.OrderByDescending(x => x.Key).ToList();
                default:
                    return Encounters.OrderByDescending(x => x.Value.HowOften).ToList();
            }
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// </summary>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        public static int DamerauLevenshteinDistance(int[] source, int[] target, int threshold)
        {

            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length 
            if (length1 > length2)
            {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++)
            {

                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++)
                {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }

        static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
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