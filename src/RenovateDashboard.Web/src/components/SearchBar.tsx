interface Props {
  value: string
  onChange: (value: string) => void
}

export function SearchBar({ value, onChange }: Props) {
  return (
    <div className="relative">
      <label htmlFor="mr-search" className="sr-only">Search MRs</label>
      <span aria-hidden="true" className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none">
        🔍
      </span>
      <input
        id="mr-search"
        type="text"
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder="Search by repo or MR title…"
        className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
      />
    </div>
  )
}
