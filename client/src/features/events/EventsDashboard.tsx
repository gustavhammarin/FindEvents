
import EventFilters from "./EventFilters";
import EventsList from "./EventsList";

export default function EventsDashboard() {
  return (
    <div className="container mx-auto p4">
      <div className="sticky top-16 z-10 mb-6 bg-transparent ">
        <EventFilters />
      </div>
      <EventsList />
    </div>
  )
}