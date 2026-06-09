import { useInfiniteQuery } from "@tanstack/react-query";
import agent from "../api/agent";
import { useStore } from "./useStore";
import { format } from "date-fns";

export const useEvents = () => {
    const { eventStore: { search, startDate } } = useStore();

    const { data: eventsGroup, isLoading, isFetchingNextPage, fetchNextPage, hasNextPage } =
        useInfiniteQuery<PagedList<FetchedEvent, EventCursor | null>>({
            queryKey: ["events", search, startDate?.toISOString()],
            queryFn: async ({ pageParam }) => {
                const cursor = pageParam as EventCursor | null;
                const response = await agent.get<PagedList<FetchedEvent, EventCursor | null>>("/events", {
                    params: {
                        cursorStartDate: cursor?.startDate ?? null,
                        cursorId: cursor?.id ?? null,
                        pageSize: 16,
                        search: search || null,
                        startDate: startDate ? format(startDate, "yyyy-MM-dd") : null,
                    }
                });
                return response.data;
            },
            initialPageParam: null,
            getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
        });

    return { eventsGroup, isLoading, hasNextPage, fetchNextPage, isFetchingNextPage };
};
