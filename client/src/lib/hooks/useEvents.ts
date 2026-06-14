import { useInfiniteQuery } from "@tanstack/react-query";
import agent from "../api/agent";
import { useFilters } from "../context/FilterContext";
import { format } from "date-fns";

export const useEvents = () => {
    const { search, startDate, category, municipality, source } = useFilters();

    const { data: eventsGroup, isLoading, isFetchingNextPage, fetchNextPage, hasNextPage } =
        useInfiniteQuery<PagedList<FetchedEvent, EventCursor | null>>({
            queryKey: ["events", search, startDate?.toISOString(), category, municipality, source],
            queryFn: async ({ pageParam }) => {
                const cursor = pageParam as EventCursor | null;
                const response = await agent.get<PagedList<FetchedEvent, EventCursor | null>>("/events", {
                    params: {
                        cursorStartDate: cursor?.startDate ?? null,
                        cursorId: cursor?.id ?? null,
                        pageSize: 16,
                        search: search || null,
                        startDate: startDate ? format(startDate, "yyyy-MM-dd") : null,
                        category: category || null,
                        municipality: municipality || null,
                        source: source || null,
                    }
                });
                return response.data;
            },
            initialPageParam: null,
            getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
        });

    return { eventsGroup, isLoading, hasNextPage, fetchNextPage, isFetchingNextPage };
};
