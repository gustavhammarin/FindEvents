import { ArrowRight } from "lucide-react";
import { Link } from "react-router";

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white flex flex-col items-center justify-center px-4">
      <div className="text-center space-y-8 max-w-lg">
        <div className="space-y-3">
          <h1 className="text-6xl font-bold tracking-tight text-gray-900">
            Happening
          </h1>
          <p className="text-lg text-gray-400">
            Hitta evenemang nära dig.
          </p>
        </div>

        <Link
          to="/events"
          className="inline-flex items-center gap-2 px-6 py-3 bg-gray-900 text-white text-sm font-medium rounded-full hover:bg-gray-700 transition-colors duration-150"
        >
          Se evenemang
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>
    </div>
  );
}
