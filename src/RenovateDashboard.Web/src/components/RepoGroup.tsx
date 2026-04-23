import { useState } from 'react'
import type { RenovateMrDto } from '../types'
import { MrTable } from './MrTable'

interface Props {
  repo: string
  mrs: RenovateMrDto[]
  repoUrl: string
}

export function RepoGroup({ repo, mrs, repoUrl }: Props) {
  const [isOpen, setIsOpen] = useState(true)

  return (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      <div className="flex items-center gap-2 px-4 py-2.5 bg-gray-50 hover:bg-gray-100">
        <button
          type="button"
          aria-expanded={isOpen}
          aria-label={`${isOpen ? 'Collapse' : 'Expand'} ${repo}`}
          onClick={() => setIsOpen(o => !o)}
          className="text-gray-400 text-xs w-3 shrink-0 cursor-pointer"
        >
          {isOpen ? '▼' : '▶'}
        </button>
        <a
          href={repoUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="font-semibold text-indigo-600 hover:underline text-sm"
        >
          {repo}
        </a>
        <span className="bg-indigo-100 text-indigo-800 text-xs font-medium px-2 py-0.5 rounded-full">
          {mrs.length} open
        </span>
      </div>
      {isOpen && <MrTable mrs={mrs} />}
    </div>
  )
}
