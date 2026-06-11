using System.Text.RegularExpressions;

namespace EventScraper.Categorization;

/// <summary>
/// Single source of truth for event categories.
/// The LLM is asked to pick one of <see cref="Categories"/>; <see cref="Normalize"/>
/// validates that answer. Keyword scoring (<see cref="Categorize"/>) covers
/// structured sources that never pass through the LLM, and acts as fallback
/// when the LLM returns something outside the list.
/// </summary>
public static class EventCategorizer
{
    public const string Default = "Övrigt";

    public static readonly IReadOnlyList<string> Categories =
    [
        "Musik & Konsert",
        "Teater & Show",
        "Konst & Utställning",
        "Föreläsning & Utbildning",
        "Workshop & Kurs",
        "Sport & Tävling",
        "Träning & Motion",
        "Natur & Friluftsliv",
        "Mat & Dryck",
        "Marknad & Loppis",
        "Familj & Barn",
        "Seniorer & Pensionärer",
        "Hälsa & Välmående",
        "Socialt & Träffpunkt",
        Default
    ];

    /// <summary>Maps a free-text LLM answer onto the fixed list, or null if it doesn't match.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var cleaned = raw.Trim().TrimEnd('.');

        var exact = Categories.FirstOrDefault(c =>
            string.Equals(c, cleaned, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Tolerate "&"/"och" swaps and partial answers like "Musik"
        var firstWord = cleaned.Split(' ', '&', ',')[0];
        if (firstWord.Length < 3) return null;

        return Categories.FirstOrDefault(c =>
            c.StartsWith(firstWord, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Keyword-based classification of title + description.</summary>
    public static string Categorize(string? title, string? description)
    {
        var t = title?.Trim() ?? "";
        var d = description?.Trim() ?? "";
        if (t.Length + d.Length == 0) return Default;

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (category, keywords) in CategoryKeywords)
        {
            int score = 0;
            foreach (var kw in keywords)
            {
                if (HasKeyword(t, kw)) score += 3; // title hits weigh 3x
                if (HasKeyword(d, kw)) score += 1;
            }
            if (score > 0) scores[category] = score;
        }

        if (scores.Count == 0) return Default;

        var maxScore = scores.Values.Max();
        return scores
            .Where(kv => kv.Value == maxScore)
            .Select(kv => kv.Key)
            .OrderByDescending(c => CategoryPriority.GetValueOrDefault(c, 0))
            .First();
    }

    // Whole-word/phrase match only, no substrings ("mat" must not hit "matchen")
    private static bool HasKeyword(string text, string keyword)
    {
        var pattern = $@"(?<![\p{{L}}\p{{M}}\p{{N}}]){Regex.Escape(keyword)}(?![\p{{L}}\p{{M}}\p{{N}}])";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    // Higher wins on score ties (more specific audience beats generic genre)
    private static readonly Dictionary<string, int> CategoryPriority = new()
    {
        ["Familj & Barn"] = 10,
        ["Seniorer & Pensionärer"] = 9,
        ["Sport & Tävling"] = 8,
        ["Teater & Show"] = 7,
        ["Musik & Konsert"] = 6,
        ["Workshop & Kurs"] = 6,
        ["Föreläsning & Utbildning"] = 6,
        ["Konst & Utställning"] = 5,
        ["Mat & Dryck"] = 5,
        ["Hälsa & Välmående"] = 4,
        ["Natur & Friluftsliv"] = 4,
        ["Marknad & Loppis"] = 3,
        ["Träning & Motion"] = 3,
        ["Socialt & Träffpunkt"] = 2,
        [Default] = 1
    };

    private static readonly Dictionary<string, List<string>> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Musik & Konsert"] =
        [
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
            "salsa night","bachata party","kizomba","afrobeat","melodifestivalen","afro house","world music",
            "indie","punk","hardcore","hard rock","progrock","progg","electropop","synth",
            "balearic","progressive","acoustic night","chillout","musikunderhållning"
        ],

        ["Teater & Show"] =
        [
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
        ],

        ["Konst & Utställning"] =
        [
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
        ],

        ["Föreläsning & Utbildning"] =
        [
            "föreläsning","föredrag","inspirationsföreläsning","seminarium","kursintro","workshop intro",
            "presentation","talk","keynote","expert talk","universitetsföreläsning","akademiföredrag",
            "panel","paneldiskussion","diskussionsforum","samtal","q&a","frågestund","publiksamtal",
            "konferens","kongress","symposium","congress","webbinarium","webinar","webbseminarium",
            "masterclass","föreläsningsserie","utbildning","kursstart","fortbildning","vidareutbildning",
            "kompetensutveckling","branschträff","internutbildning","företagsutbildning","varsamtal",
            "frukostmöte","lunchföreläsning","after lunch talk","after work talk","TEDx","TED talk",
            "forskningsföreläsning","expertföreläsning","reseföreläsning","reseskildring",
            "bokrelease","bokpresentation"
        ],

        ["Workshop & Kurs"] =
        [
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
        ],

        ["Sport & Tävling"] =
        [
            "match","serieomgång","omgång","cup","turnering","championship","liga","mästerskap",
            "final","semifinal","kvartsfinal","kval","playoff","seriespel","friendly","vänskapsmatch",
            "träningsmatch","fotboll","herrmatch","dammatch","ishockey","innebandy","basket","handboll",
            "tennis","golf","pingis","padel","ridsport","dressyr","hoppning","trav","galopp",
            "skidåkning","slalom","längdskidor","snowboard","cykeltävling","mtb","mountainbike",
            "landsväg","löptävling","jogginglopp","mil","halvmaraton","ultramaraton","triathlon",
            "ironman","orientering","nattorientering","skidskytte","skidlopp","schack","bridgespel",
            "e-sport","gamingturnering","LAN","CS:GO","LoL","Fortnite","darts","poker","bowling",
            "bilrally","motorsport","mc-race","speedway","karting","drifting","stafett","boxning",
            "kickboxning","mma","karate","enduro","taekwondo","aikido","judo","brottning","capoeira",
            "simtävling","simsport","friidrott","cricket","amerikansk fotboll","rugby","baseboll",
            "frisbeegolf","ultimat frisbee","klättringstävling","surfing","segelregatta"
        ],

        ["Träning & Motion"] =
        [
            "träning","pass","workout","cirkelträning","gym","gympass","styrketräning",
            "gruppträning","spinning","cycling","corepass","funktionell träning","bootcamp",
            "yoga","yogapass","yin yoga","hatha yoga","ashtanga","power yoga","hot yoga",
            "pilates","zumba","salsaträning","afrodans","vattengympa","aerobics","stretch",
            "intervaller","HIIT","tabata","crossfit","trx","bodypump","bodybalance","bodycombat",
            "outdoor fitness","träna utomhus","löppass","löpning","jogging","trailrun","powerwalk",
            "rörlighetsträning","klättringsträning","danspass","latindanspass"
        ],

        ["Natur & Friluftsliv"] =
        [
            "vandring","hajk","hiking","trekking","fjällvandring","fjälltur","bergsvandring",
            "skogsvandring","naturnatt","nattvandring","skogstur","skogsbad","shinrin yoku",
            "friluftsliv","naturdag","äventyr","wildlife","kanot","kajak","rafting","forsränning",
            "fiske","camping","tältning","survival","bushcraft","läger","sommarkollo","äventyrsdag",
            "friluftsguide","naturreservat","nationalpark","svampplockning","bärplockning",
            "skogskurs","eldkväll","äventyrshelg","klättring","bergsklättring",
            "ice climbing","kajaktur","vildmarkshelg","skogshäng","naturvandring","naturfestival"
        ],

        ["Mat & Dryck"] =
        [
            "mat","drinkar","fika","brunch","middag","lunch","after lunch","supé","middagssittning",
            "smörgåsbord","julfika","julbord","påskbord","smörgåstårta","buffé","knytkalas",
            "gatumat","streetfood","foodtruck","food truck","streetfoodfestival","matfestival",
            "mathelg","food market","country fair","kulinarisk","matmässa","grillfest","barbecue",
            "bbq","picknick","höstmiddag","vinmiddag","tapas","bakfest","bakverk","bakdag",
            "pizzakväll","pasta night","ostprovning","ostfestival","chokladprovning","chokladfestival",
            "kaffeprovning","teprovning","ölprovning","vinprovning","ginprovning","whiskyprovning",
            "romprovning","champagneafton","cocktail","drink","bartendershow",
            "drinkprovning","vinfestival","ölfestival","sakeprovning","matmarknad","bryggeribesök",
            "destilleribesök","lokalproducerat","ekologisk mat","slow food","fine dining","gastronomi"
        ],

        ["Marknad & Loppis"] =
        [
            "marknad","vårmarknad","vårmässa","sommarutflykt","sommarfestival","höstmarknad",
            "julmarknad","adventsmarknad","påskmarknad","skördefest","bondemarknad","gårdsmarknad",
            "ekologisk marknad","torghandel","bynatt","byfest","loppis","loppmarknad",
            "antikmarknad","antikmässa","lördagsloppis","söndagsloppis","stor loppis","byteloppis",
            "bakluckeloppis","second hand","bytesdag","bytardag","klädbytardag","barnloppis",
            "hantverksmarknad","vintage marknad","designmarknad","craft fair","bazaar","mässa",
            "julmässa","tigermarknad","christmas fair","julrea","reaevent","pop up market","handelsdag"
        ],

        ["Familj & Barn"] =
        [
            "barn","familj","föräldrar","familjekul","barnkalas","familjedag","barnens dag",
            "pyssel","barnpyssel","sagostund","barnteater","barnföreställning","barnshow","barndisco",
            "lek","lekdag","lekplats","barnlekdag","barnbio","dockteater","familjebio","familjematiné",
            "öppen förskola","babymassage","babyrytmik","barnrytmik","sångstund","barnsång",
            "föräldracafé","barnbibliotek","barnworkshop","familjeworkshop","barnhelg","ungdomshelg",
            "pannkaksfrukost","familjequiz","spelturnering barn","familjeloppis","barnmarknad",
            "barnens festival","mulleverksamhet","barnidrott","skattjakt","barnskoj"
        ],

        ["Seniorer & Pensionärer"] =
        [
            "senior","pensionär","äldre","äldreträff","pensionärsfest","seniorträff","dagverksamhet",
            "pro-förening","pro träff","mötesplats senior","veteranklubb","veteranträff","seniorcafé",
            "dagcentral","seniorgympa","äldrejumppa","seniorträning","nostalgikväll","historiekväll",
            "historieberättande","seniordans","pensionärsdans","gammeldans","boccia","bridgekväll",
            "sällskapsspel","stickcafé","syjunta","målarcafé","målarglädje","vårdbingo","filmkafé",
            "seniorbio","släktträff äldre","promenadgrupp","trygga promenader"
        ],

        ["Hälsa & Välmående"] =
        [
            "hälsa","wellness","mindfulness","meditation","meditationskväll",
            "massage","spa","spabehandling","avslappning","mental träning","självhjälp",
            "holistisk hälsa","kostråd","kostföreläsning","hälsocoach",
            "livsstil","stresshantering","utbrändhet","återhämtning","friskvård","egen omtanke",
            "hälsomässa","hälsohelg","hälsoretreat","föreläsning hälsa","healing",
            "sound healing","gongbad","klangkärl","reiki","återhämtningsdag","balansdag",
            "self-care","mental health","mental styrka","psykisk hälsa","wellbeing","hälsokväll"
        ],

        ["Socialt & Träffpunkt"] =
        [
            "mingel","after work","afterwork","after ski","after beach","AW","träffpunkt","meetup",
            "öppet hus","språkcafé","sprakcafe","språkgrupp","international mingle","quizkväll",
            "quiz night","music quiz","pubkväll","klubbträff","studentpub","studentkväll",
            "kårpub","brädspelskväll","sällskapsspelskväll","nördträff","gamingkväll","LAN-kväll",
            "spelkväll","filmkväll","bioafton","communityträff","temakväll","föräldrakväll",
            "kaffehäng","fredagshäng","torsdaghäng","nätverk","networking",
            "affärsmingel","speed dating","datingkväll","festkväll",
            "temafest","temafestkväll","bokcirkel"
        ],

        [Default] =
        [
            "jubileum","invigning","invigningsfest","öppningsfest","upptaktsmöte","årsfest",
            "specialevent","temadag","firande","högtid","tradition","ceremoni"
        ]
    };
}
