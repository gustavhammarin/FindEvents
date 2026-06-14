import { createContext, useContext, useState, type ReactNode } from 'react';

interface Filters {
  search: string;
  setSearch: (v: string) => void;
  startDate: Date | undefined;
  setStartDate: (v: Date | undefined) => void;
  category: string;
  setCategory: (v: string) => void;
  municipality: string;
  setMunicipality: (v: string) => void;
  source: string;
  setSource: (v: string) => void;
}

const FilterContext = createContext<Filters | null>(null);

export function FilterProvider({ children }: { children: ReactNode }) {
  const [search, setSearch] = useState('');
  const [startDate, setStartDate] = useState<Date | undefined>();
  const [category, setCategory] = useState('');
  const [municipality, setMunicipality] = useState('');
  const [source, setSource] = useState('');

  return (
    <FilterContext.Provider value={{
      search, setSearch,
      startDate, setStartDate,
      category, setCategory,
      municipality, setMunicipality,
      source, setSource,
    }}>
      {children}
    </FilterContext.Provider>
  );
}

export function useFilters() {
  const ctx = useContext(FilterContext);
  if (!ctx) throw new Error('useFilters must be used within FilterProvider');
  return ctx;
}
