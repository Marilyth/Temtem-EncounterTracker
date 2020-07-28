using System.Collections.Generic;

namespace Temtem_EncounterTracker
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Stats
    {
        public int hp { get; set; }
        public int sta { get; set; }
        public int spd { get; set; }
        public int atk { get; set; }
        public int def { get; set; }
        public int spatk { get; set; }
        public int spdef { get; set; }
        public int total { get; set; }

    }

    public class Height
    {
        public int cm { get; set; }
        public int inches { get; set; }

    }

    public class Weight
    {
        public int kg { get; set; }
        public int lbs { get; set; }

    }

    public class Details
    {
        public Height height { get; set; }
        public Weight weight { get; set; }

    }

    public class Technique
    {
        public string name { get; set; }
        public string source { get; set; }
        public int levels { get; set; }

    }


    public class EvolutionTree
    {
        public int number { get; set; }
        public string name { get; set; }
        public int stage { get; set; }
        public int? levels { get; set; }
        public bool? trading { get; set; }

    }

    public class Evolution
    {
        public int stage { get; set; }
        public List<EvolutionTree> evolutionTree { get; set; }
        public bool evolves { get; set; }
        public string type { get; set; }

    }

    public class Freetem
    {
        public int minLevel { get; set; }
        public int maxLevel { get; set; }
        public int? minPansuns { get; set; }
        public int? maxPansuns { get; set; }

    }

    public class Location
    {
        public string location { get; set; }
        public string place { get; set; }
        public string note { get; set; }
        public string island { get; set; }
        public string frequency { get; set; }
        public string level { get; set; }
        public Freetem freetem { get; set; }

    }

    public class GenderRatio
    {
        public int male { get; set; }
        public int female { get; set; }

    }

    public class TvYields
    {
        public int hp { get; set; }
        public int sta { get; set; }
        public int spd { get; set; }
        public int atk { get; set; }
        public int def { get; set; }
        public int spatk { get; set; }
        public int spdef { get; set; }

    }

    public class AttackValues    {
        public double Neutral { get; set; } 
        public double Fire { get; set; } 
        public double Water { get; set; } 
        public double Nature { get; set; } 
        public double Electric { get; set; } 
        public double Earth { get; set; } 
        public double Mental { get; set; } 
        public double Wind { get; set; } 
        public double Digital { get; set; } 
        public double Melee { get; set; } 
        public double Crystal { get; set; } 
        public double Toxic { get; set; } 

    }

    public class Elements{
        Dictionary<string, KeyValuePair<string, double>> AttackValues;
    }

    public class Temtem
    {
        public int number { get; set; }
        public string name { get; set; }
        public List<string> types { get; set; }
        public string portraitWikiUrl { get; set; }
        public string lumaPortraitWikiUrl { get; set; }
        public string wikiUrl { get; set; }
        public Stats stats { get; set; }
        public List<string> traits { get; set; }
        public Details details { get; set; }
        public List<Technique> techniques { get; set; }
        public List<string> trivia { get; set; }
        public Evolution evolution { get; set; }
        public string wikiPortraitUrlLarge { get; set; }
        public string lumaWikiPortraitUrlLarge { get; set; }
        public List<Location> locations { get; set; }
        public string icon { get; set; }
        public string lumaIcon { get; set; }
        public GenderRatio genderRatio { get; set; }
        public int catchRate { get; set; }
        public double hatchMins { get; set; }
        public TvYields tvYields { get; set; }
        public string gameDescription { get; set; }
        public string wikiRenderStaticUrl { get; set; }
        public string wikiRenderAnimatedUrl { get; set; }
        public string wikiRenderStaticLumaUrl { get; set; }
        public string wikiRenderAnimatedLumaUrl { get; set; }
        public string renderStaticImage { get; set; }
        public string renderStaticLumaImage { get; set; }
        public string renderAnimatedImage { get; set; }
        public string renderAnimatedLumaImage { get; set; }

    }

}