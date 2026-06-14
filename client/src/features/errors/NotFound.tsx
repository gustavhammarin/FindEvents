import { Link } from "react-router";
import { SearchX } from "lucide-react";

export default function NotFound() {
  return (
    <div className="max-w-sm mx-auto mt-24 flex flex-col items-center gap-4 text-center">
      <SearchX className="w-16 h-16 text-gray-300" />
      <h1 className="text-xl font-semibold text-gray-900">Sidan hittades inte</h1>
      <Link
        to="/events"
        className="text-sm text-gray-500 hover:text-gray-900 underline transition-colors"
      >
        Tillbaka till evenemang
      </Link>
    </div>
  );
}
