import { useEvents } from "@/lib/hooks/useEvents";
import { observer } from "mobx-react-lite";
import EventCard from "./EventCard";
import { useInView } from "react-intersection-observer";
import { useEffect } from "react";

const EventsList = observer(function EventsList() {
  const { eventsGroup, isLoading, hasNextPage, fetchNextPage, isFetchingNextPage } = useEvents();
  const { ref, inView } = useInView({ threshold: 0.1 });

  useEffect(() => {
    if (inView && hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [inView, hasNextPage, isFetchingNextPage, fetchNextPage]);

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl overflow-hidden border border-gray-100 animate-pulse">
            <div className="aspect-[16/9] bg-gray-200" />
            <div className="p-3 space-y-2">
              <div className="h-3 bg-gray-200 rounded w-full" />
              <div className="h-3 bg-gray-200 rounded w-2/3" />
              <div className="h-3 bg-gray-100 rounded w-1/3 mt-2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (!eventsGroup) {
    return <p className="text-sm text-gray-400 text-center py-16">Inga evenemang hittades</p>;
  }

  const events = eventsGroup.pages.flatMap((page) => page.items);

  if (events.length === 0) {
    return <p className="text-sm text-gray-400 text-center py-16">Inga evenemang hittades</p>;
  }

  return (
    <div>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 items-stretch">
        {events.map((event, index) => (
          <div
            key={event.id}
            ref={index === events.length - 1 ? ref : null}
            className="h-full"
          >
            <EventCard event={event} />
          </div>
        ))}
      </div>

      {isFetchingNextPage && (
        <div className="flex justify-center py-8">
          <span className="w-5 h-5 rounded-full border-2 border-gray-200 border-t-gray-600 animate-spin" />
        </div>
      )}
    </div>
  );
});

export default EventsList;
