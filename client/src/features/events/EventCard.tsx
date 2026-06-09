import { Calendar, MapPin } from "lucide-react";

type Props = {
  event: FetchedEvent;
};

const BASE_URL = "https://jkpg.com";

export default function EventCard({ event }: Props) {
  const fullLink = event.link?.startsWith("http")
    ? event.link
    : `${BASE_URL}${event.link}`;

  const hasValidImage =
    !!event.imageUrl &&
    !event.imageUrl.includes("placeholder") &&
    event.imageUrl.startsWith("http");

  const formattedDate = event.startDate
    ? new Date(event.startDate).toLocaleDateString("sv-SE", {
        day: "numeric",
        month: "short",
      })
    : null;

  return (
    <a
      href={fullLink}
      target="_blank"
      rel="noopener noreferrer"
      className="group flex flex-col bg-white rounded-xl overflow-hidden border border-gray-100 hover:border-gray-200 hover:shadow-md transition-all duration-200 h-full"
    >
      {/* Fixed-ratio image */}
      <div className="relative w-full aspect-[16/9] bg-gray-100 overflow-hidden flex-shrink-0">
        {hasValidImage ? (
          <img
            src={event.imageUrl}
            alt={event.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            onError={(e) => {
              e.currentTarget.style.display = "none";
            }}
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <Calendar className="w-8 h-8 text-gray-300" />
          </div>
        )}
        {event.category && (
          <span className="absolute bottom-2 left-2 text-xs font-medium px-2 py-0.5 rounded-full bg-black/50 text-white backdrop-blur-sm leading-tight">
            {event.category}
          </span>
        )}
      </div>

      {/* Content — fixed min-height so all cards same size */}
      <div className="p-3 flex flex-col gap-1 flex-1">
        <p className="text-sm font-semibold text-gray-900 line-clamp-2 leading-snug flex-1">
          {event.title}
        </p>
        <div className="flex flex-col gap-0.5 pt-2">
          {formattedDate && (
            <div className="flex items-center gap-1.5 text-xs text-gray-400">
              <Calendar className="w-3 h-3 flex-shrink-0" />
              <span>{formattedDate}</span>
            </div>
          )}
          {event.location && (
            <div className="flex items-center gap-1.5 text-xs text-gray-400">
              <MapPin className="w-3 h-3 flex-shrink-0" />
              <span className="truncate">{event.location}</span>
            </div>
          )}
        </div>
      </div>
    </a>
  );
}
