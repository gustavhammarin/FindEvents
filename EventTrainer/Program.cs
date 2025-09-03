using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EventScraper;
using EventScraper.models;

public class EventInput
{
    public string Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; } // Label
}

public class EventPrediction
{
    [ColumnName("PredictedLabel")]
    public string? PredictedCategory { get; set; }
    public float[]? Score { get; set; }
}

public static class CategoryMatcher
{
    // Viktighetsvikt för olika kategorier (högre = prioriteras vid poänglikhet)
    private static readonly Dictionary<string, int> CategoryPriority = new()
    {
        ["Familj & Barn"] = 10,
        ["Seniorer & Pensionärer"] = 9,
        ["Sport & Tävling"] = 8,
        ["Musik & Konsert"] = 6,
        ["Teater & Show"] = 7,
        ["Workshop & Kurs"] = 6,
        ["Föreläsning & Utbildning"] = 6,
        ["Konst & Utställning"] = 5,
        ["Mat & Dryck"] = 5,
        ["Hälsa & Välmående"] = 4,
        ["Natur & Friluftsliv"] = 4,
        ["Marknad & Loppis"] = 3,
        ["Träning & Motion"] = 3,
        ["Socialt & Träffpunkt"] = 2,
        ["Övrigt"] = 1
    };

    // Hjälpmetod: matcha bara hela ord/fraser, ej substring
    private static bool HasKeyword(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
            return false;

        var pattern = $@"(?<![\p{{L}}\p{{M}}\p{{N}}]){Regex.Escape(keyword)}(?![\p{{L}}\p{{M}}\p{{N}}])";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static string DetermineCategory(string? title, string? description,
        Dictionary<string, List<string>> categoryKeywords)
    {
        var t = title?.Trim() ?? "";
        var d = description?.Trim() ?? "";
        if (t.Length + d.Length == 0)
            return "Övrigt";

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in categoryKeywords)
        {
            var cat = kvp.Key;
            var keywords = kvp.Value;
            int score = 0;

            foreach (var kw in keywords)
            {
                if (HasKeyword(t, kw)) score += 3;     // title träffar viktas 3x
                if (HasKeyword(d, kw)) score += 1;     // description 1x
            }

            if (score > 0)
                scores[cat] = score;
        }

        if (!scores.Any())
            return "Övrigt";

        // Välj kategori med högst poäng, vid lika använd CategoryPriority
        var maxScore = scores.Values.Max();
        var tied = scores.Where(kv => kv.Value == maxScore).Select(kv => kv.Key);

        return tied
            .OrderByDescending(cat => CategoryPriority.GetValueOrDefault(cat, 0))
            .First();
    }
}

class Program
{
    static void Main()
    {
        var mlContext = new MLContext(seed: 123);
        Console.WriteLine("📂 Ansluter till databas...");

        // 1) Definiera nyckelordslistor med massor av ord per kategori
        var categoryKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Musik & Konsert"] = new()
            {
                "konsert","konsertafton","spelning","spelkväll","gig","musik","liveband","livemusik",
                "utomhuskonsert","parkspelning","sommarkonsert","julkonsert","nyårskonsert",
                "musikfestival","festival","cityfestival","folkfest","jam session","jam night",
                "open mic","karaoke","allsångskväll","allsång","orkester","symfoni","stråkkvartett",
                "stråkensemble","kammarmusik","kör","gospelkonsert","jazzkvartett","jazzgig",
                "rockband","metalband","popspelning","tributeband","coverband","hyllningskonsert",
                "electronic","edm","house","deep house","techno","drum n bass","trance","hiphop",
                "hip-hop","rap","trap","rnb","rhythm and blues","soul","blues","country","americana",
                "visa","folkmusik","trubadur","singer-songwriter","soloartist","bandspelning",
                "dj","disc jockey","klubbdj","afterparty","efterfest","musikquiz","releasefest",
                "skivsläpp","singelsläpp","album release","showcase","minifestival","musiknatt",
                "rave","klubbkväll","klubbnatt","club night","reggae","dancehall","latin",
                "salsa night","bachata party","kizomba","afrobeat", "rockband", "melodifestivalen","afro house","world music",
                "indie","punk","hardcore","hard rock","progrock","progg","electropop","synth",
                "balearic","progressive","acoustic night","chillout","musikunderhållning", "orkester"
            },

            ["Teater & Show"] = new()
            {
                "teater","pjäs","föreställning","scenkonst","drama","komedi","tragikomedi","fars",
                "satir","monolog","dialog","manus","regi","scenografi","kostym","mask","standup",
                "stand-up","stand up comedy","humorshow","revue","revy","varieté","kabaret",
                "cirkus","nycirkus","akrobatik","jonglering","clownshow","trolleri","magishow",
                "illusionist","balett","balettscen","opera","operaföreställning","operakonsert",
                "musikal","musikföreställning","show","dansshow","dansföreställning","dance show",
                "improvisationsteater","impro","improshow","teatersport","performance","gatuteater",
                "mim","street performance","gästspel","internationellt gästspel","turné",
                "lokalteater","amatörteater","halvprofessionell teater","familjeföreställning",
                "barnteater","skuggspel","dockteater","interaktiv teater","sommarteater",
                "teaterfestival","dramakväll"
            },

            ["Konst & Utställning"] = new()
            {
                "konst","utställning","konstutställning","vernissage","finissage","vernissagekväll",
                "vernissageöppning","retrospektiv","soloutställning","grupputställning","diplomutställning",
                "examensutställning","studentutställning","årskursutställning","konstevent","konstvisning",
                "galleri","gallerivisning","ateljébesök","öppen ateljé","open studio","museum","museivisning",
                "konsthall","installation","site specific konst","performance art","målning","måleri",
                "oljemålning","akvarell","akryl","blandteknik","collage","grafik","träsnitt","linoleumtryck",
                "skulptur","modellering","foto","fotografi","fotokonst","fotovernissage","fotofest",
                "street art","graffiti","keramik","drejning","textilkonst","stickat","vävt","design","mode",
                "möbeldesign","arkitektur","formgivning","konsthantverk","hantverk","handarbete","slöjd",
                "träslöjd","sameslöjd","silverarbete","teckning","skiss","digital konst","ljuskonst","ljusinstallation",
                "ljusfestival","videokonst","mediekonst","sound art","konstmässa","art fair","artexpo",
                "modern konst","samtidskonst","äldre konst","antik konst"
            },

            ["Föreläsning & Utbildning"] = new()
            {
                "föreläsning","föredrag","inspirationsföreläsning","seminarium","kursintro","workshop intro",
                "presentation","talk","keynote","expert talk","universitetsföreläsning","akademiföredrag",
                "panel","paneldiskussion","diskussionsforum","samtal","q&a","frågestund","publiksamtal",
                "konferens","kongress","symposium","congress","webbinarium","webinar","webbseminarium",
                "masterclass","föreläsningsserie","utbildning","kursstart","fortbildning","vidareutbildning",
                "kompetensutveckling","branschträff","internutbildning","företagsutbildning","varsamtal",
                "frukostmöte","lunchföreläsning","after lunch talk","after work talk","TEDx","TED talk",
                "forskningsföreläsning","expertföreläsning","reseföreläsning","reseskildring",
                "bokrelease","bokpresentation"
            },

            ["Workshop & Kurs"] = new()
            {
                "workshop","kurs","studiecirkel","seminarium praktiskt","prova på","prova-på-kurs","kursdag",
                "helgkurs","introkurs","fortsättningskurs","målarworkshop","målarkurs","skisskurs",
                "akvarellkurs","fotokurs","foto workshop","skrivarkurs","författarkurs","poesikurs",
                "keramikkurs","drejakurs","keramikworkshop","stickkurs","virkkurs","vävkurs","sömnadskurs",
                "textilkurs","quilting","broderikurs","hantverkskurs","silversmide","smyckeskurs",
                "programmeringskurs","datakurs","it-kurs","webbkurs","onlinekurs","språkkurs",
                "yogakurs","pilateskurs","dansworkshop","danskurs","improvisationskurs","teaterkurs",
                "musikworkshop","bandworkshop","trumkurs","pianokurs","gitarrkurs","songwriting workshop",
                "filmworkshop","animationskurs","makerspace","3dprintkurs","robotworkshop","tech workshop",
                "matlagningskurs","bakningskurs","cocktailkurs","baristakurs","barnkurs","ungdomskurs",
                "coachning","mentorskap","ledarskapsworkshop","kommunikationsworkshop"
            },

            ["Sport & Tävling"] = new()
            {
                "match","serieomgång","omgång","cup","turnering","championship","liga","mästerskap",
                "final","semifinal","kvartsfinal","kval","playoff","seriespel","friendly","vänskapsmatch",
                "träningsmatch","fotboll","herrmatch","dammatch","ishockey","innebandy","basket","handboll",
                "tennis","golf","pingis","padel","ridsport","dressyr","hoppning","trav","galopp",
                "skidåkning","slalom","längdskidor","snowboard","cykeltävling","mtb","mountainbike",
                "landsväg","löptävling","jogginglopp","mil","halvmaraton","ultramaraton","triathlon",
                "ironman","orientering","nattorientering","skidskytte","skidlopp","schack","bridgespel",
                "e-sport","gamingturnering","LAN","CS:GO","LoL","Fortnite","darts","poker","bowling",
                "bilrally","motorsport","mc-race","speedway","karting","drifting","stafett","boxning",
                "kickboxning","mma","karate","motorsport", "enduro","taekwondo","aikido","judo","brottning","capoeira",
                "simtävling","simsport","friidrott","cricket","amerikansk fotboll","rugby","baseboll",
                "frisbeegolf","ultimat frisbee","klättringstävling","surfing","segelregatta"
            },

            ["Träning & Motion"] = new()
            {
                "träning","pass","workout","cirkelträning","gym","gympass","styrketräning",
                "gruppträning","spinning","cycling","corepass","funktionell träning","bootcamp",
                "yoga","yogapass","yin yoga","hatha yoga","ashtanga","power yoga","hot yoga",
                "pilates","zumba","salsaträning","afrodans","vattengympa","aerobics","stretch",
                "intervaller","HIIT","tabata","crossfit","trx","bodypump","bodybalance","bodycombat",
                "outdoor fitness","träna utomhus","löppass","löpning","jogging","trailrun","powerwalk",
                "rörlighetsträning","klättringsträning","danspass","latindanspass"
            },

            ["Natur & Friluftsliv"] = new()
            {
                "vandring","hajk","hiking","trekking","fjällvandring","fjälltur","bergsvandring",
                "skogsvandring","naturnatt","nattvandring","skogstur","skogsbad","shinrin yoku",
                "friluftsliv","naturdag","äventyr","wildlife","kanot","kajak","rafting","forsränning",
                "fiske","camping","tältning","survival","bushcraft","läger","sommarkollo","äventyrsdag",
                "friluftsguide","naturreservat","nationalpark","svampplockning","bärplockning",
                "fjälltur","skogskurs","eldkväll","äventyrshelg","klättring","bergsklättring",
                "ice climbing","kajaktur","vildmarkshelg","skogshäng","naturvandring","naturfestival"
            },

            ["Mat & Dryck"] = new()
            {
                "mat","drinkar","fika","brunch","middag","lunch","after lunch","supé","middagssittning",
                "smörgåsbord","julfika","julbord","påskbord","smörgåstårta","buffé","knytkalas",
                "gatumat","streetfood","foodtruck","food truck","streetfoodfestival","matfestival",
                "mathelg","food market","country fair","kulinarisk","matmässa","grillfest","barbecue",
                "bbq","picknick","höstmiddag","vinmiddag","tapas","bakfest","bakverk","bakdag",
                "pizzakväll","pasta night","ostprovning","ostfestival","chokladprovning","chokladfestival",
                "kaffeprovning","teprovning","ölprovning","vinprovning","ginprovning","whiskyprovning",
                "romprovning","champagneafton","cocktail","drink","cocktailkurs","bartendershow",
                "drinkprovning","vinfestival","ölfestival","sakeprovning","matmarknad","bryggeribesök",
                "destilleribesök","lokalproducerat","ekologisk mat","slow food","fine dining","gastronomi"
            },

            ["Marknad & Loppis"] = new()
            {
                "marknad","vårmarknad","vårmässa","sommarutflykt","sommarfestival","höstmarknad",
                "julmarknad","adventsmarknad","påskmarknad","skördefest","bondemarknad","gårdsmarknad",
                "ekologisk marknad","matmarknad","torghandel","bynatt","byfest","loppis","loppmarknad",
                "antikmarknad","antikmässa","lördagsloppis","söndagsloppis","stor loppis","byteloppis",
                "bakluckeloppis","second hand","bytesdag","bytardag","klädbytardag","barnloppis",
                "hantverksmarknad","vintage marknad","designmarknad","craft fair","bazaar","mässa",
                "julmässa","tigermarknad","christmas fair","julrea","reaevent","pop up market","handelsdag"
            },

            ["Familj & Barn"] = new()
            {
                "barn","familj","föräldrar","familjekul","barnkalas","familjedag","barnens dag",
                "pyssel","barnpyssel","sagostund","barnteater","barnföreställning","barnshow","barndisco",
                "lek","lekdag","lekplats","barnlekdag","barnbio","dockteater","familjebio","familjematiné",
                "öppen förskola","babymassage","babyrytmik","barnrytmik","sångstund","barnsång",
                "föräldracafé","barnbibliotek","barnworkshop","familjeworkshop","barnhelg","ungdomshelg",
                "pannkaksfrukost","familjequiz","spelturnering barn","familjeloppis","barnmarknad",
                "barnens festival","mulleverksamhet","barnidrott","skattjakt","barnskoj"
            },

            ["Seniorer & Pensionärer"] = new()
            {
                "senior","pensionär","äldre","äldreträff","pensionärsfest","seniorträff","dagverksamhet",
                "pro-förening","pro träff","mötesplats senior","veteranklubb","veteranträff","seniorcafé",
                "dagcentral","seniorgympa","äldrejumppa","seniorträning","nostalgikväll","historiekväll",
                "historieberättande","seniordans","pensionärsdans","gammeldans","boccia","bridgekväll",
                "sällskapsspel","stickcafé","syjunta","målarcafé","målarglädje","vårdbingo","filmkafé",
                "seniorbio","släktträff äldre","promenadgrupp","trygga promenader"
            },

            ["Hälsa & Välmående"] = new()
            {
                "hälsa","wellness","mindfulness","Yoga & Hälsa","meditation","meditationskväll",
                "massage","spa","spabehandling","avslappning","mental träning","självhjälp",
                "holistisk hälsa","kostråd","kostföreläsning","träning & balans","hälsocoach",
                "livsstil","stresshantering","utbrändhet","återhämtning","friskvård","egen omtanke",
                "hälsomässa","hälsohelg","hälsoretreat","föreläsning hälsa","tonic","healing",
                "sound healing","gongbad","klangkärl","reiki","återhämtningsdag","balansdag",
                "self-care","mental health","mental styrka","psykisk hälsa","wellbeing","hälsokväll"
            },

            ["Socialt & Träffpunkt"] = new()
            {
                "mingel","after work","afterwork","after ski","after beach","AW","träffpunkt","meetup",
                "öppet hus","språkcafé","sprakcafe","språkgrupp","international mingle","quizkväll",
                "quiz night","music quiz","pubkväll","baren kväll","klubbträff","studentpub","studentkväll",
                "kårpub","brädspelskväll","sällskapsspelskväll","nördträff","gamingkväll","LAN-kväll",
                "spelkväll","filmkväll","bioafton","communityträff","temakväll","föräldrakväll",
                "föräldracafé","kaffehäng","fredagshäng","torsdaghäng","nätverk","networking",
                "affärsmingel","speed dating","datingkväll","afterparty","flirtkväll","festkväll",
                "temafest","temafestkväll", "bokcirkel"
            },

            ["Övrigt"] = new() 
            { 
                "övrigt","annat","misc","diverse","öppet hus","kalas","jubileum","invigning",
                "invigningsfest","öppningsfest","upptaktsmöte","årsfest","specialevent","event",
                "okategoriserat","temadag","firande","högtid","tradition","ceremoni"
            }
        };


        // 2) Läs in, matcha kategori och markera alla events som Modified
        using var db = new ScraperDbContext();
        var events = db.Events.ToList();
        int updatedCount = 0;

        foreach (var ev in events)
        {
            var detected = CategoryMatcher.DetermineCategory(ev.Title, ev.Description, categoryKeywords);
            if (!string.Equals(ev.Category, detected, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"🔄 [{ev.Id}] » \"{ev.Title}\" → {detected} (föregående: {ev.Category})");
                ev.Category = detected;
                db.Entry(ev).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                updatedCount++;
            }
        }

        var saveCount = db.SaveChanges();
        Console.WriteLine($"💾 Uppdaterade {updatedCount} events. SaveChanges påverkade {saveCount} rader.");

        // 3) (Frivilligt) Träna ML-modell på de uppdaterade kategorierna
        if (events.Count(e => !string.IsNullOrWhiteSpace(e.Category)) >= 20)
        {
            var trainingData = events
                .Where(e => !string.IsNullOrWhiteSpace(e.Category))
                .Select(e => new EventInput
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Category = e.Category
                })
                .ToList();

            var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = mlContext.Transforms.Text.FeaturizeText("TitleFeats", nameof(EventInput.Title))
                .Append(mlContext.Transforms.Text.FeaturizeText("DescFeats", nameof(EventInput.Description)))
                .Append(mlContext.Transforms.Concatenate("Features", "TitleFeats", "DescFeats"))
                .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(EventInput.Category)))
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            Console.WriteLine("🧠 Tränar ML-modell...");
            var model = pipeline.Fit(dataView);
            mlContext.Model.Save(model, dataView.Schema, "eventModel.zip");
            Console.WriteLine("✅ Modell tränad och sparad som eventModel.zip.");
        }
        else
        {
            Console.WriteLine("⚠️ För få uppdaterade events för modellträning. Minst 20 behövs.");
        }
    }
}