import { useEvents } from "@/lib/hooks/useEvents";
import { observer } from "mobx-react-lite";
import EventCard from "./EventCard";
import { useInView } from "react-intersection-observer";
import { useEffect } from "react";

const EventsList = observer(function EventsList() {
  const { eventsGroup, isLoading, hasNextPage, fetchNextPage } = useEvents();
  const { ref, inView } = useInView({
    threshold: 0.5,
  });

  useEffect(() => {
    if (inView && hasNextPage) {
      fetchNextPage();
    }
  }, [inView, hasNextPage, fetchNextPage]);

  if (isLoading)
    return <p className="text-gray-500 text-center py-8">Loading...</p>;

  if (!eventsGroup)
    return (
      <p className="text-gray-500 text-center py-8">No activities found</p>
    );

  // Flatten all events from all pages
  const allEvents = eventsGroup.pages.flatMap((page) => page.items);
  const uniqueEvents = allEvents.filter(
    (event, index, arr) => arr.findIndex((e) => e.id === event.id) === index
  );

  // Log om det finns skillnad
  if (allEvents.length !== uniqueEvents.length) {
    console.warn(
      `Removed ${
        allEvents.length - uniqueEvents.length
      } duplicate events on frontend`
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
      {uniqueEvents.map((event, index) => (
        <div
          key={event.id}
          ref={index === uniqueEvents.length - 1 ? ref : null}
        >
          <EventCard event={event} />
        </div>
      ))}
    </div>
  );
});

export default EventsList;
