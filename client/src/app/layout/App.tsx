
import NavBar from "./NavBar";
import { Outlet, ScrollRestoration, useLocation } from "react-router";
import HomePage from "../../features/home/HomePage";

export function App() {
  const location = useLocation();

  return (
    <div className="bg-gradient-to-br from-gray-900 via-gray-800 to-black min-h-screen">
      {/*<div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-violet-500/10 rounded-full blur-3xl"></div>
        
      </div>*/}
      <ScrollRestoration />

      {location.pathname === "/" ? (
        <HomePage />
      ) : (
        <>
          <NavBar />
          {/* container max-width: xl + padding top */}
          <main className="container mx-auto max-w-screen-xl pt-20 px-4">
            <Outlet />
          </main>
        </>
      )}
    </div>
  );
}

export default App;
