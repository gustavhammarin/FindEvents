import { useFilters } from "../../lib/context/FilterContext";
import { Search, X, CalendarIcon, Tag, Check, MapPin, BookOpen } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { format } from "date-fns";
import { sv } from "date-fns/locale";
import { EVENT_CATEGORIES } from "../../lib/util/categories";

const ORGANIZERS: { label: string; source: string }[] = [
    { label: "Studieförbundet Vuxenskolan", source: "sv.se" },
];

const MUNICIPALITIES = [
    "Aneby", "Eksjö", "Gislaved", "Gnosjö", "Habo",
    "Jönköping", "Mullsjö", "Nässjö", "Sävsjö", "Tranås",
    "Vaggeryd", "Vetlanda", "Värnamo",
];

export default function EventFilters() {
    const { search, setSearch, startDate, setStartDate, category, setCategory, municipality, setMunicipality, source, setSource } = useFilters();

    const hasFilters = !!search || !!startDate || !!category || !!municipality || !!source;

    return (
        <div className="flex items-center gap-2 bg-white border border-gray-200 rounded-xl px-4 py-2.5 shadow-sm max-w-xl mx-auto">
            <Search className="w-4 h-4 text-gray-400 flex-shrink-0" />
            <input
                type="text"
                placeholder="Sök evenemang..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="flex-1 text-sm text-gray-900 placeholder:text-gray-400 outline-none bg-transparent min-w-0"
            />

            <div className="w-px h-4 bg-gray-200 flex-shrink-0" />

            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <button className="flex items-center gap-1.5 text-sm whitespace-nowrap transition-colors flex-shrink-0 max-w-36"
                        style={{ color: category ? '#111827' : '#9ca3af' }}>
                        <Tag className="w-3.5 h-3.5 flex-shrink-0" />
                        <span className="truncate">{category || "Kategori"}</span>
                    </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="max-h-72 overflow-y-auto">
                    {category && (
                        <DropdownMenuItem onClick={() => setCategory("")} className="text-gray-500">
                            Alla kategorier
                        </DropdownMenuItem>
                    )}
                    {EVENT_CATEGORIES.map((c) => (
                        <DropdownMenuItem key={c} onClick={() => setCategory(c)}>
                            <span className="flex-1">{c}</span>
                            {category === c && <Check className="w-3.5 h-3.5 text-gray-500" />}
                        </DropdownMenuItem>
                    ))}
                </DropdownMenuContent>
            </DropdownMenu>

            <div className="w-px h-4 bg-gray-200 flex-shrink-0" />

            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <button className="flex items-center gap-1.5 text-sm whitespace-nowrap transition-colors flex-shrink-0 max-w-36"
                        style={{ color: municipality ? '#111827' : '#9ca3af' }}>
                        <MapPin className="w-3.5 h-3.5 flex-shrink-0" />
                        <span className="truncate">{municipality || "Kommun"}</span>
                    </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="max-h-72 overflow-y-auto">
                    {municipality && (
                        <DropdownMenuItem onClick={() => setMunicipality("")} className="text-gray-500">
                            Alla kommuner
                        </DropdownMenuItem>
                    )}
                    {MUNICIPALITIES.map((m) => (
                        <DropdownMenuItem key={m} onClick={() => setMunicipality(m)}>
                            <span className="flex-1">{m}</span>
                            {municipality === m && <Check className="w-3.5 h-3.5 text-gray-500" />}
                        </DropdownMenuItem>
                    ))}
                </DropdownMenuContent>
            </DropdownMenu>

            <div className="w-px h-4 bg-gray-200 flex-shrink-0" />

            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <button className="flex items-center gap-1.5 text-sm whitespace-nowrap transition-colors flex-shrink-0 max-w-36"
                        style={{ color: source ? '#111827' : '#9ca3af' }}>
                        <BookOpen className="w-3.5 h-3.5 flex-shrink-0" />
                        <span className="truncate">{ORGANIZERS.find(o => o.source === source)?.label || "Arrangör"}</span>
                    </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="max-h-72 overflow-y-auto">
                    {source && (
                        <DropdownMenuItem onClick={() => setSource("")} className="text-gray-500">
                            Alla arrangörer
                        </DropdownMenuItem>
                    )}
                    {ORGANIZERS.map((o) => (
                        <DropdownMenuItem key={o.source} onClick={() => setSource(o.source)}>
                            <span className="flex-1">{o.label}</span>
                            {source === o.source && <Check className="w-3.5 h-3.5 text-gray-500" />}
                        </DropdownMenuItem>
                    ))}
                </DropdownMenuContent>
            </DropdownMenu>

            <div className="w-px h-4 bg-gray-200 flex-shrink-0" />

            <Popover>
                <PopoverTrigger asChild>
                    <button className="flex items-center gap-1.5 text-sm whitespace-nowrap transition-colors flex-shrink-0"
                        style={{ color: startDate ? '#111827' : '#9ca3af' }}>
                        <CalendarIcon className="w-3.5 h-3.5" />
                        {startDate ? format(startDate, "d MMM", { locale: sv }) : "Datum"}
                    </button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="end">
                    <Calendar
                        mode="single"
                        selected={startDate}
                        onSelect={setStartDate}
                    />
                </PopoverContent>
            </Popover>

            {hasFilters && (
                <button
                    onClick={() => { setSearch(""); setStartDate(undefined); setCategory(""); setMunicipality(""); setSource(""); }}
                    className="p-0.5 rounded-full hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors flex-shrink-0"
                >
                    <X className="w-3.5 h-3.5" />
                </button>
            )}
        </div>
    );
}
