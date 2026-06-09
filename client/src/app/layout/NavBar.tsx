import { Link } from "react-router";
import { useStore } from "../../lib/hooks/useStore";
import { Observer } from "mobx-react-lite";

export default function NavBar() {
  const { uiStore } = useStore();

  return (
    <nav className="fixed top-0 left-0 w-full z-50 bg-white border-b border-gray-100">
      <div className="container mx-auto max-w-screen-xl px-4 h-14 flex items-center justify-between">
        <Link to="/" className="flex items-center gap-2">
          <span className="font-semibold text-gray-900 tracking-tight">Happening</span>
          <Observer>
            {() => uiStore.isLoading ? (
              <span className="w-3.5 h-3.5 rounded-full border-2 border-gray-300 border-t-gray-700 animate-spin" />
            ) : null}
          </Observer>
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
