// Must match EventCategorizer.Categories in EventScraper (backend validates against the same list)
export const EVENT_CATEGORIES = [
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
    "Övrigt",
] as const;

export type EventCategory = typeof EVENT_CATEGORIES[number];
