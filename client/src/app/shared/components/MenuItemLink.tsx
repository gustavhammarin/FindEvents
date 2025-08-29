
import type { ReactNode } from "react";
import { NavLink } from "react-router";



type MenuItemLinkProps = {
  to: string;
  children: ReactNode;
  className?: string;
  onClick?: () => void;
};

export default function MenuItemLink({ children, to, className = "", onClick }: MenuItemLinkProps) {
  return (
    <NavLink
      to={to}
      onClick={onClick}
      className={({ isActive }) =>
        [
          "px-3 py-2 uppercase font-bold text-lg transition-colors rounded-md",
          isActive ? "text-teal-400" : "text-white hover:text-teal-200",
          className,
        ].join(" ")
      }
    >
      {children}
    </NavLink>
  );
}
