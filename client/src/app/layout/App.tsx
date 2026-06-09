import NavBar from "./NavBar";
import { Outlet, ScrollRestoration, useLocation } from "react-router";
import HomePage from "../../features/home/HomePage";

export function App() {
  const location = useLocation();

  return (
    <div className="min-h-screen bg-gray-50">
      <ScrollRestoration />
      {location.pathname === "/" ? (
        <HomePage />
      ) : (
        <>
          <NavBar />
          <main className="container mx-auto max-w-screen-xl pt-20 px-4 pb-12">
            <Outlet />
          </main>
        </>
      )}
    </div>
  );
}

export default App;
