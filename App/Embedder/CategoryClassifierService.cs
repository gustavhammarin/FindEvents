using App.Scraper.Categorization;
using Pgvector;

namespace App.Embedder;

public class CategoryClassifierService(IServiceScopeFactory scopeFactory, ILogger<CategoryClassifierService> logger)
{
    private (string Category, ReadOnlyMemory<float> Embedding)[]? _categoryEmbeddings;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public bool IsReady => _categoryEmbeddings is { Length: > 0 };

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (IsReady) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (IsReady) return;

            using var scope = scopeFactory.CreateScope();
            var embedder = scope.ServiceProvider.GetRequiredService<MistralEmbeddingService>();

            var descriptions = CategoryDescriptions.Select(d => d.Description).ToList();
            var vectors = await embedder.EmbedBatchAsync(descriptions, ct);

            var result = new List<(string, ReadOnlyMemory<float>)>();
            for (var i = 0; i < CategoryDescriptions.Length; i++)
            {
                if (vectors[i] is null)
                {
                    logger.LogWarning("Failed to embed category '{Category}'", CategoryDescriptions[i].Category);
                    continue;
                }
                result.Add((CategoryDescriptions[i].Category, vectors[i]!.Memory));
            }

            _categoryEmbeddings = result.ToArray();
            logger.LogInformation("CategoryClassifier initialized with {Count}/{Total} category embeddings",
                _categoryEmbeddings.Length, CategoryDescriptions.Length);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public string Classify(Vector eventEmbedding)
    {
        if (_categoryEmbeddings is null || _categoryEmbeddings.Length == 0)
            return EventCategories.Default;

        var evSpan = eventEmbedding.Memory.Span;
        var bestCategory = EventCategories.Default;
        var bestDistance = double.MaxValue;

        foreach (var (category, catMem) in _categoryEmbeddings)
        {
            var dist = CosineDistance(evSpan, catMem.Span);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestCategory = category;
            }
        }

        return bestCategory;
    }

    private static double CosineDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0;
        for (var i = 0; i < a.Length; i++)
            dot += a[i] * b[i];
        return 1.0 - dot;
    }

    private static readonly (string Category, string Description)[] CategoryDescriptions =
    [
        ("Musik & Konsert",
            "konsert livemusik spelning band artist sångare sångerska orkester symfoniorkester " +
            "kammarorkester kör blandad kör manskör damkör jazz jazzkonsert rock pop metal " +
            "folkmusik klassisk musik musikfestival spelkväll dansgala dansband tribute covers " +
            "gitarr piano violin saxofon trummor dj set entertainer uppträder musikunderhållning " +
            "körframträdande köruppvisning musikevenemang musikafton sånguppvisning"),
        ("Teater & Show",
            "teater pjäs musikal föreställning drama komedi tragedi ståupp standup cirkus " +
            "kabaret improvisationsteater improv barnteater scen skådespel show revy varieté " +
            "pantomim opera operett sångspel trolleri magi gycklare sagoberättande sagospektakel " +
            "clown dockteater marionetteater figurteater gatuteatern uppvisning"),
        ("Konst & Utställning",
            "konst utställning vernissage galleri museum konstverk fotografi fotoutställning " +
            "skulptur måleri keramik design hantverk kulturhus konsthall teckning illustration " +
            "textil textilkonst grafik akvarell oljemålning konstutställning öppen ateljé " +
            "ateljevisning konstmässa bildkonst installationskonst samtidskonst folkkonst " +
            "hantverksutställning slöjd"),
        ("Föreläsning & Utbildning",
            "föreläsning seminarium föredrag diskussion debatt paneldiskussion konferens " +
            "symposium talk presentation kunskap akademisk lärande informationsmöte " +
            "studiebesök föreläsningskväll samhällsfrågor bokrelease bokmässa bokprat " +
            "berättarkväll inspirationsföreläsning temakväll reportage"),
        ("Workshop & Kurs",
            "workshop kurs praktisk övning kreativt skapande undervisning lär dig prova på " +
            "prova-på göra tillverka klass studiecirkel sykurs syslöjd slöjd keramikkurs " +
            "målerikurs kreativ stickning virkning pyssel pyssla skapa skaparkväll " +
            "matlagningskurs kokkurs bakning bakkurs hantverk hantverkskurs teckning " +
            "smyckestillverkning smide"),
        ("Sport & Tävling",
            "sport tävling match fotboll hockey ishockey tennis simning friidrott löpartävling " +
            "SM DM cup turnering liga speldag bandy innebandy basket handboll golf orientering " +
            "skidtävling skidskytte cykeltävling bordtennis squash badminton ridsport häst " +
            "ridning trav galopp motorsport bilsport mc-race motorcykeltävling rally autocross " +
            "cross motocross speedway dragrace karting brottning kampsport UFC boxningstävling " +
            "cykelrace triatlon maraton halvmaraton stafett hinderlopp"),
        ("Träning & Motion",
            "träning motion yoga gym fitness löpning jogging promenad motionsrunda dans aerobics " +
            "styrketräning pilates zumba kondition rörelse stretching spinning cykling " +
            "vattengymnastik bassängträning qigong tai chi boxning kampsport karate judo " +
            "bootcamp HIIT intervallträning crossfit träningsklass motionspass gympass " +
            "morgonjympa morgonyoga löpargrupp träningsgrupp"),
        ("Natur & Friluftsliv",
            "natur friluftsliv vandring utomhus fågelskådning naturupplevelse skog botanik " +
            "stig naturvandring naturguide guidning sjö kanot kajakpaddling paddling fiske " +
            "svampplockning bärplockning naturfotografering skogspromenad geologisk bergsbestigning " +
            "klättring MTB mountainbike terränglöpning naturreservat ekologi biologisk mångfald " +
            "miljö camping friluft"),
        ("Mat & Dryck",
            "mat dryck middag restaurang matfestival vin öl gastronomi provsmakning matlagning " +
            "brunch kock måltid lunch tapas whisky whiskey rom cocktail gin punsch cider " +
            "matkurs kokkurs bakning tårta dessert foodtruck streetfood julbord smörgåsbord " +
            "buffé gästkrog pop-up restaurang matmarknad bondens marknad skördefest " +
            "matupplevelse gourmet degustationsafton"),
        ("Marknad & Loppis",
            "marknad loppis basar secondhand second hand julmarknad påskmarknad vårmarknad " +
            "höstmarknad hantverk börsdag bytdag antikviteter loppisar mässa utställning " +
            "handel säljer köper byt byta garageförsäljning loppsmarknad hemslöjd " +
            "hantverksmarknad bondens marknaden torghandel"),
        ("Familj & Barn",
            "familj barn barnaktivitet barnkonsert barnteater barnshow barnkultur leksak " +
            "ungdomar barnvänligt förskola skola familjevänligt aktiviteter för barn barnkalas " +
            "sagostund bokläsning bibliotek unge tonåring grundskola gymnasiet barnbio " +
            "barnfilm barnfestival lekplats barndag öppet hus familjedag barnuppvisning " +
            "barnomhändertagande junior ungdomsaktivitet"),
        ("Seniorer & Pensionärer",
            "seniorer pensionärer äldre PRO SPF äldres träff senioraktivitet pensionärsförbund " +
            "65+ SeniorNet pensionärsdag seniorgympa seniorcafé veteranklubb äldrecenter " +
            "dagverksamhet dagcentral aktivitet för äldre pensionärernas hus träff för äldre " +
            "mötesplats för seniorer"),
        ("Hälsa & Välmående",
            "hälsa välmående meditation mindfulness mental hälsa hälsosamtal psykisk hälsa " +
            "stresshantering wellness terapi reiki andlighet avslappning kognitiv " +
            "beteendeterapi KBT terapeutisk chakra energiläkning självkänsla livsstil " +
            "kost näring hälsokost hälsomässa hälsodag sömnhälsa naturmedicin homeopati " +
            "healingkväll"),
        ("Socialt & Träffpunkt",
            "socialt träff förening möte nätverkande mingel community sällskap sammankomst " +
            "café umgås frivillig volontär spelkväll sällskapsspel quiz triviaafton nostalgi " +
            "tema fest firande jubileum kalas afterwork open mic öppen scen " +
            "bil mc motorcykel fordonsträff bilträff mc-träff veteranbil fordonsshow " +
            "veteranfordon mopedträff motorcykelklubb bilklubb gudstjänst kyrka " +
            "församling bön religiöst kyrkokonsert kyrkoevenemang"),
        ("Övrigt",
            "övrigt blandat diverse evenemang allmänt varierande mässa kongress invigning " +
            "premiär gala award ceremoni diplom prisutdelning politik kommunfullmäktige " +
            "information öppet hus visning")
    ];
}
