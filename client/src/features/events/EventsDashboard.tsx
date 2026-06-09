import EventFilters from "./EventFilters";
import EventsList from "./EventsList";

export default function EventsDashboard() {
  return (
    <div className="space-y-6">
      <div className="sticky top-14 z-10 py-3 bg-gray-50">
        <EventFilters />
      </div>
      <EventsList />
    </div>
  );
}
