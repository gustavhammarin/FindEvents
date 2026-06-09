import { useStore } from "../../lib/hooks/useStore";
import { observer } from "mobx-react-lite";
import { Search, X, CalendarIcon } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { format } from "date-fns";
import { sv } from "date-fns/locale";

const EventFilters = observer(() => {
    const { eventStore: { setSearch, search, setStartDate, startDate } } = useStore();

    const hasFilters = !!search || !!startDate;

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
                    onClick={() => { setSearch(""); setStartDate(undefined); }}
                    className="p-0.5 rounded-full hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors flex-shrink-0"
                >
                    <X className="w-3.5 h-3.5" />
                </button>
            )}
        </div>
    );
});

export default EventFilters;
