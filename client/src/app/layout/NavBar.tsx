
import {

  CircularProgress,
} from "@mui/material";
import MenuItemLink from "../shared/components/MenuItemLink";
import { useStore } from "../../lib/hooks/useStore";
import { Observer } from "mobx-react-lite";
import { useState } from "react";
import { Menu, X } from "lucide-react";

export default function NavBar() {
  const { uiStore } = useStore();

  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <>
      <nav className="fixed top-0 left-0 w-full z-50 bg-stone-950 flex items-center p-2 justify-between">
        <div className="container mx-auto px-2 flex items-center justify-between">
          
            <div>
              <MenuItemLink to="/" className="flex gap-2 left-0 items-center">
                <span className="relative font-bold text-2xl text-white">
                  Happening
                  <Observer>
                    {() => uiStore.isLoading ? (
                      <CircularProgress
                        size={20}
                        thickness={7}
                        sx={{
                          color: 'white',
                          position: 'absolute',
                          top: '30%',
                          left: '105%'
                        }}
                      />
                    ) : null}
                  </Observer>
                </span>
              </MenuItemLink>
            </div>

            <div className="hidden md:flex space-x-3">
              <MenuItemLink to="/errors">Errors</MenuItemLink>
              <MenuItemLink to="/events">Events</MenuItemLink>
            </div>

            <button
              className="md:hidden text-white p-2 rounded hover:bg-white/20 transition-colors"
              onClick={() => setSidebarOpen(true)}
              aria-label="Open menu"
            >
              <Menu className="w-6 h-6" />
            </button>



          
        </div>
      </nav>

    {/* Sidebar-overlay + offcanvas */}
      {sidebarOpen && (
        <>
          {/* Bakgrund som dimmas */}
          <div
            className="fixed inset-0 bg-opacity-50 z-40"
            onClick={() => setSidebarOpen(false)}
            aria-hidden="true"
          />

          {/* Sidebar */}
          <aside className="fixed top-0 left-0 bottom-0 w-64 bg-stone-800 z-50 p-6 flex flex-col space-y-6 text-white shadow-lg">
            <div className="flex items-center justify-between">
              <MenuItemLink to="/" className="font-bold text-2xl" onClick={() => setSidebarOpen(false)}>
                Happening
              </MenuItemLink>
              <button
                className="p-2 rounded hover:bg-teal-400/20"
                onClick={() => setSidebarOpen(false)}
                aria-label="Close menu"
              >
                <X className="w-6 h-6" />
              </button>
            </div>

            <nav className="flex flex-col space-y-4">
              {/* Samma länkar */}
              <MenuItemLink to="/errors" onClick={() => setSidebarOpen(false)}>Errors</MenuItemLink>
            </nav>

          </aside>
        </>
      )}       

    </>


  );
}
