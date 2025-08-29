import { FilterList } from "@mui/icons-material";
import 'react-calendar/dist/Calendar.css';
import { useStore } from "../../lib/hooks/useStore";
import { cn } from "@/lib/utils"
import { observer } from "mobx-react-lite";
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"
import { CalendarIcon } from "lucide-react";
import { format } from "date-fns"


const FILTER_OPTIONS = [
    { key: "all", label: "All events" },
    { key: "Musik & Konsert", label: "Musik & Konsert" },
    { key: "Teater & Show", label: "Teater & Show" },
    { key: "Konst & Utställning", label: "Konst & Utställning" },
    { key: "Workshop & Kurs", label: "Workshop & Kurs" },
    { key: "Sport & Tävling", label: "Sport & Tävling" },
    { key: "Träning & Motion", label: "Träning & Motion" },
    { key: "Natur & Friluftsliv", label: "Natur & Friluftsliv" },
    { key: "Mat & Dryck", label: "Mat & Dryck" },
    { key: "Marknad & Loppis", label: "Marknad & Loppis" },
    { key: "Familj & Barn", label: "Familj & Barn" },
    { key: "Seniorer & Pensionärer", label: "Seniorer & Pensionärer" },
    { key: "Hälsa & Välmående", label: "Hälsa & Välmående" },
    { key: "Socialt & Träffpunkt", label: "Socialt & Träffpunkt" },
    { key: "Övrigt", label: "Övrigt" },
];

const EventFilters = observer(() => {
    const { eventStore: { setFilter, setStartDate, filter, startDate, setSearch, search } } = useStore()

    const filterLabel = FILTER_OPTIONS.find((f) => f.key === filter)?.label || "Filters";

    return (
        <div className="flex justify-between items-center gap-4 rounded-xl p-1 text-card-foreground max-w-md mx-auto">
            {/* Filter */}
            <Popover>
                <PopoverTrigger asChild>
                    <Button
                        variant="outline"
                        className="flex-1 justify-start text-left font-normal hover:bg-stone-400 bg-stone-800 text-white"
                        aria-label="Filter events"
                    >
                        <FilterList className="mr-2 h-4 w-4" />
                        {filterLabel}
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-48 p-0">
                    <Card className="p-0 rounded-md shadow-md">
                        <ul className="divide-y divide-border">
                            {FILTER_OPTIONS.map(({ key, label }) => (
                                <li
                                    key={key}
                                    onClick={() => setFilter(key)}
                                    className={cn(
                                        "cursor-pointer select-none px-4 py-2",
                                        filter === key
                                            ? "bg-primary text-primary-foreground font-semibold"
                                            : "hover:bg-primary/10"
                                    )}
                                    role="menuitem"
                                    tabIndex={0}
                                >
                                    {label}
                                </li>
                            ))}
                        </ul>
                    </Card>
                </PopoverContent>
            </Popover>

            {/* Datepicker med Shadcn Calendar */}
            <Popover>
                <PopoverTrigger asChild>
                    <Button
                        variant="outline"
                        data-empty={!startDate}
                        className="data-[empty=true]:text-muted-foreground flex-1 justify-start text-left font-normal hover:bg-stone-400 bg-stone-700 text-white"
                    >
                        <CalendarIcon />
                        {startDate ? format(startDate, "PPP") : <span>Pick a date</span>}
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0">
                    <Calendar mode="single" selected={startDate} onSelect={setStartDate} />
                </PopoverContent>
            </Popover>
            <input
                type="text"
                placeholder="Sök event..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="flex-1 px-4 py-2 rounded-md bg-stone-800 text-white placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-primary"
            />

        </div>
    )
})

export default EventFilters;
