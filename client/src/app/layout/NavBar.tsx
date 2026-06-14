import { Link } from "react-router";

export default function NavBar() {
  return (
    <nav className="fixed top-0 left-0 w-full z-50 bg-white border-b border-gray-100">
      <div className="container mx-auto max-w-screen-xl px-4 h-14 flex items-center justify-between">
        <Link to="/" className="font-semibold text-gray-900 tracking-tight">
          Happening
        </Link>
        <Link
          to="/events"
          className="text-sm text-gray-500 hover:text-gray-900 transition-colors"
        >
          Evenemang
        </Link>
      </div>
    </nav>
  );
}
