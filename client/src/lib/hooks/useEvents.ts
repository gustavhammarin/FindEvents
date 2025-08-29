import { useInfiniteQuery } from "@tanstack/react-query"
import agent from "../api/agent";
import { useStore } from "./useStore";


export const useEvents = () => {
    const {eventStore: {filter, startDate, search}} = useStore();

    const {data: eventsGroup, isLoading, isFetchingNextPage, fetchNextPage, hasNextPage} = useInfiniteQuery<PagedList<FetchedEvent, string>>({
        queryKey:["events", filter, startDate, search],
        queryFn: async ({pageParam = null}) => {
            const response = await agent.get<PagedList<FetchedEvent, string>>("/events", {
                params: {
                    cursor: pageParam,
                    pageSize: 16,
                    filter,
                    startDate,
                    search
                }
            });
            return response.data;
        },
        initialPageParam: null,
        getNextPageParam: (lastPage) => lastPage.nextCursor,
        select: data => ({
            ...data,
            pages: data.pages.map((page) => ({
                ...page,
                items: page.items.map(event => {
                    return{
                        ...event
                    }
                })
            }))
        })
    })
    return {
        eventsGroup,
        isLoading,
        hasNextPage,
        fetchNextPage,
        isFetchingNextPage
    }
}