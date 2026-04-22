# MR Table Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display open Renovate MRs grouped by repo in a collapsible, searchable table — built with React + TypeScript + Tailwind CSS v4.

**Architecture:** `App` owns all state (fetch result, search query). Pure utility functions handle grouping and filtering. Four presentational components (SearchBar, RepoGroup, MrTable, MrRow) receive data as props. Utility functions are unit-tested with Vitest; components are verified by running the dev server.

**Tech Stack:** React 19, TypeScript, Vite, Tailwind CSS v4 (`@tailwindcss/vite`), Vitest

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `src/RenovateDashboard.Web/vite.config.ts` | Add Tailwind plugin + Vitest config |
| Modify | `src/RenovateDashboard.Web/package.json` | Add tailwindcss, @tailwindcss/vite, vitest |
| Modify | `src/RenovateDashboard.Web/src/index.css` | Replace content with `@import "tailwindcss"` |
| Create | `src/RenovateDashboard.Web/src/types.ts` | `RenovateMrDto` TypeScript interface |
| Create | `src/RenovateDashboard.Web/src/utils/mr.ts` | `groupByRepo`, `filterGroups`, `getRepoUrl` |
| Create | `src/RenovateDashboard.Web/src/utils/relativeTime.ts` | `relativeTime(iso)` → `"3d ago"` |
| Create | `src/RenovateDashboard.Web/src/utils/mr.test.ts` | Unit tests for mr.ts |
| Create | `src/RenovateDashboard.Web/src/utils/relativeTime.test.ts` | Unit tests for relativeTime.ts |
| Create | `src/RenovateDashboard.Web/src/components/SearchBar.tsx` | Controlled search input |
| Create | `src/RenovateDashboard.Web/src/components/MrRow.tsx` | Clickable table row + pipeline badge |
| Create | `src/RenovateDashboard.Web/src/components/MrTable.tsx` | Table with column headers |
| Create | `src/RenovateDashboard.Web/src/components/RepoGroup.tsx` | Collapsible repo section |
| Modify | `src/RenovateDashboard.Web/src/App.tsx` | Fetch, group, filter, render |

---

## Task 1: Install Tailwind CSS v4 and Vitest

**Files:**
- Modify: `src/RenovateDashboard.Web/package.json`
- Modify: `src/RenovateDashboard.Web/vite.config.ts`
- Modify: `src/RenovateDashboard.Web/src/index.css`

- [ ] **Step 1: Install packages**

```bash
cd src/RenovateDashboard.Web
npm install tailwindcss @tailwindcss/vite
npm install -D vitest
```

- [ ] **Step 2: Update `vite.config.ts`** to add Tailwind plugin and Vitest test config

Replace the entire file with:

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
  test: {
    environment: 'node',
  },
})
```

- [ ] **Step 3: Update `src/index.css`** — replace the entire file content with:

```css
@import "tailwindcss";
```

- [ ] **Step 4: Add test script to `package.json`**

In the `"scripts"` section, add:
```json
"test": "vitest run"
```

- [ ] **Step 5: Verify Tailwind is wired up**

```bash
cd src/RenovateDashboard.Web
npm run dev
```

Open http://localhost:5173. The page should still load (placeholder text). No console errors. Stop the server (`Ctrl+C`).

- [ ] **Step 6: Commit**

```bash
git add src/RenovateDashboard.Web/vite.config.ts src/RenovateDashboard.Web/package.json src/RenovateDashboard.Web/package-lock.json src/RenovateDashboard.Web/src/index.css
git commit -m "chore: add Tailwind CSS v4 and Vitest"
```

---

## Task 2: Types + utility functions + unit tests

**Files:**
- Create: `src/RenovateDashboard.Web/src/types.ts`
- Create: `src/RenovateDashboard.Web/src/utils/mr.ts`
- Create: `src/RenovateDashboard.Web/src/utils/relativeTime.ts`
- Create: `src/RenovateDashboard.Web/src/utils/mr.test.ts`
- Create: `src/RenovateDashboard.Web/src/utils/relativeTime.test.ts`

- [ ] **Step 1: Create `src/types.ts`**

```ts
export interface RenovateMrDto {
  repo: string
  iid: number
  title: string
  webUrl: string
  createdAt: string
  author: string
  sourceBranch: string
  targetBranch: string
  pipelineStatus: string | null
}
```

- [ ] **Step 2: Write failing tests for `mr.ts`**

Create `src/utils/mr.test.ts`:

```ts
import { describe, it, expect } from 'vitest'
import { groupByRepo, filterGroups, getRepoUrl } from './mr'
import type { RenovateMrDto } from '../types'

const make = (repo: string, title: string, iid = 1): RenovateMrDto => ({
  repo, iid, title,
  webUrl: `https://gitlab.com/${repo}/-/merge_requests/${iid}`,
  createdAt: new Date().toISOString(),
  author: 'renovate-bot',
  sourceBranch: 'renovate/dep',
  targetBranch: 'main',
  pipelineStatus: null,
})

describe('groupByRepo', () => {
  it('groups MRs by repo preserving order', () => {
    const mrs = [make('org/api', 'Update A'), make('org/web', 'Update B'), make('org/api', 'Update C', 2)]
    const groups = groupByRepo(mrs)
    expect(groups.get('org/api')).toHaveLength(2)
    expect(groups.get('org/web')).toHaveLength(1)
  })

  it('returns empty map for empty input', () => {
    expect(groupByRepo([])).toEqual(new Map())
  })
})

describe('filterGroups', () => {
  const groups = new Map([
    ['org/api', [make('org/api', 'Update lodash')]],
    ['org/frontend', [make('org/frontend', 'Update react')]],
  ])

  it('returns same reference when query is empty', () => {
    expect(filterGroups(groups, '')).toBe(groups)
  })

  it('matches by repo name and shows all MRs in that group', () => {
    const result = filterGroups(groups, 'api')
    expect(result.size).toBe(1)
    expect(result.get('org/api')).toHaveLength(1)
  })

  it('matches by MR title', () => {
    const result = filterGroups(groups, 'react')
    expect(result.size).toBe(1)
    expect(result.get('org/frontend')).toHaveLength(1)
  })

  it('is case-insensitive', () => {
    expect(filterGroups(groups, 'LODASH').size).toBe(1)
  })

  it('returns empty map when nothing matches', () => {
    expect(filterGroups(groups, 'zzz').size).toBe(0)
  })
})

describe('getRepoUrl', () => {
  it('strips the MR path from a GitLab web_url', () => {
    expect(getRepoUrl('https://gitlab.com/org/api/-/merge_requests/42')).toBe('https://gitlab.com/org/api')
  })
})
```

- [ ] **Step 3: Run tests — expect them to fail**

```bash
cd src/RenovateDashboard.Web
npm test
```

Expected: `Cannot find module './mr'`

- [ ] **Step 4: Create `src/utils/mr.ts`**

```ts
import type { RenovateMrDto } from '../types'

export function groupByRepo(mrs: RenovateMrDto[]): Map<string, RenovateMrDto[]> {
  const map = new Map<string, RenovateMrDto[]>()
  for (const mr of mrs) {
    const group = map.get(mr.repo) ?? []
    group.push(mr)
    map.set(mr.repo, group)
  }
  return map
}

export function filterGroups(
  groups: Map<string, RenovateMrDto[]>,
  query: string,
): Map<string, RenovateMrDto[]> {
  if (!query.trim()) return groups
  const q = query.toLowerCase()
  const result = new Map<string, RenovateMrDto[]>()
  for (const [repo, mrs] of groups) {
    if (repo.toLowerCase().includes(q)) {
      result.set(repo, mrs)
    } else {
      const matched = mrs.filter(mr => mr.title.toLowerCase().includes(q))
      if (matched.length > 0) result.set(repo, matched)
    }
  }
  return result
}

export function getRepoUrl(webUrl: string): string {
  return webUrl.split('/-/')[0]
}
```

- [ ] **Step 5: Write failing tests for `relativeTime.ts`**

Create `src/utils/relativeTime.test.ts`:

```ts
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { relativeTime } from './relativeTime'

describe('relativeTime', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-04-22T12:00:00Z'))
  })
  afterEach(() => vi.useRealTimers())

  it('shows minutes for recent timestamps', () => {
    expect(relativeTime('2026-04-22T11:45:00Z')).toBe('15m ago')
  })

  it('shows hours for same-day timestamps', () => {
    expect(relativeTime('2026-04-22T08:00:00Z')).toBe('4h ago')
  })

  it('shows days for older timestamps', () => {
    expect(relativeTime('2026-04-19T12:00:00Z')).toBe('3d ago')
  })
})
```

- [ ] **Step 6: Run tests — expect relativeTime tests to fail**

```bash
npm test
```

Expected: mr.ts tests PASS, relativeTime tests fail with `Cannot find module './relativeTime'`

- [ ] **Step 7: Create `src/utils/relativeTime.ts`**

```ts
export function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diff / 60_000)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}
```

- [ ] **Step 8: Run all tests — expect all to pass**

```bash
npm test
```

Expected output:
```
✓ src/utils/mr.test.ts (6)
✓ src/utils/relativeTime.test.ts (3)
Test Files  2 passed (2)
Tests       9 passed (9)
```

- [ ] **Step 9: Commit**

```bash
git add src/RenovateDashboard.Web/src/types.ts src/RenovateDashboard.Web/src/utils/
git commit -m "feat: add MR types and utility functions with tests"
```

---

## Task 3: MrRow component

**Files:**
- Create: `src/RenovateDashboard.Web/src/components/MrRow.tsx`

- [ ] **Step 1: Create `src/components/MrRow.tsx`**

```tsx
import type { RenovateMrDto } from '../types'
import { relativeTime } from '../utils/relativeTime'

function PipelineBadge({ status }: { status: string | null }) {
  if (!status) return <span className="text-gray-400 text-xs">—</span>
  const styles: Record<string, string> = {
    success: 'bg-green-100 text-green-800',
    failed: 'bg-red-100 text-red-800',
    running: 'bg-amber-100 text-amber-800',
    pending: 'bg-amber-100 text-amber-800',
    canceled: 'bg-gray-100 text-gray-600',
    skipped: 'bg-gray-100 text-gray-600',
  }
  const cls = styles[status] ?? 'bg-gray-100 text-gray-600'
  return (
    <span className={`${cls} text-xs font-medium px-2 py-0.5 rounded-full`}>
      {status}
    </span>
  )
}

export function MrRow({ mr }: { mr: RenovateMrDto }) {
  return (
    <tr
      className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
      onClick={() => window.open(mr.webUrl, '_blank', 'noopener,noreferrer')}
    >
      <td className="px-4 py-2.5 font-medium text-gray-900">{mr.title}</td>
      <td className="px-4 py-2.5 font-mono text-xs text-gray-500 max-w-[200px] truncate">
        {mr.sourceBranch}
      </td>
      <td className="px-4 py-2.5 text-gray-400 whitespace-nowrap text-sm">
        {relativeTime(mr.createdAt)}
      </td>
      <td className="px-4 py-2.5">
        <PipelineBadge status={mr.pipelineStatus} />
      </td>
    </tr>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add src/RenovateDashboard.Web/src/components/MrRow.tsx
git commit -m "feat: add MrRow component"
```

---

## Task 4: MrTable component

**Files:**
- Create: `src/RenovateDashboard.Web/src/components/MrTable.tsx`

- [ ] **Step 1: Create `src/components/MrTable.tsx`**

```tsx
import type { RenovateMrDto } from '../types'
import { MrRow } from './MrRow'

export function MrTable({ mrs }: { mrs: RenovateMrDto[] }) {
  return (
    <table className="w-full border-collapse text-sm">
      <thead>
        <tr className="bg-gray-50 text-left text-xs text-gray-500 font-medium uppercase tracking-wide">
          <th className="px-4 py-2">Title</th>
          <th className="px-4 py-2">Branch</th>
          <th className="px-4 py-2">Age</th>
          <th className="px-4 py-2">Pipeline</th>
        </tr>
      </thead>
      <tbody>
        {mrs.map(mr => (
          <MrRow key={`${mr.repo}-${mr.iid}`} mr={mr} />
        ))}
      </tbody>
    </table>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add src/RenovateDashboard.Web/src/components/MrTable.tsx
git commit -m "feat: add MrTable component"
```

---

## Task 5: RepoGroup component

**Files:**
- Create: `src/RenovateDashboard.Web/src/components/RepoGroup.tsx`

- [ ] **Step 1: Create `src/components/RepoGroup.tsx`**

```tsx
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
      <button
        type="button"
        className="w-full flex items-center gap-2 px-4 py-2.5 bg-gray-50 hover:bg-gray-100 text-left"
        onClick={() => setIsOpen(o => !o)}
      >
        <span className="text-gray-400 text-xs w-3">{isOpen ? '▼' : '▶'}</span>
        <a
          href={repoUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="font-semibold text-indigo-600 hover:underline text-sm"
          onClick={e => e.stopPropagation()}
        >
          {repo}
        </a>
        <span className="bg-indigo-100 text-indigo-800 text-xs font-medium px-2 py-0.5 rounded-full">
          {mrs.length} open
        </span>
      </button>
      {isOpen && <MrTable mrs={mrs} />}
    </div>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add src/RenovateDashboard.Web/src/components/RepoGroup.tsx
git commit -m "feat: add collapsible RepoGroup component"
```

---

## Task 6: SearchBar component

**Files:**
- Create: `src/RenovateDashboard.Web/src/components/SearchBar.tsx`

- [ ] **Step 1: Create `src/components/SearchBar.tsx`**

```tsx
interface Props {
  value: string
  onChange: (value: string) => void
}

export function SearchBar({ value, onChange }: Props) {
  return (
    <div className="relative">
      <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none">
        🔍
      </span>
      <input
        type="text"
        value={value}
        onChange={e => onChange(e.target.value)}
        placeholder="Search by repo or MR title…"
        className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
      />
    </div>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add src/RenovateDashboard.Web/src/components/SearchBar.tsx
git commit -m "feat: add SearchBar component"
```

---

## Task 7: Wire up App.tsx

**Files:**
- Modify: `src/RenovateDashboard.Web/src/App.tsx`

- [ ] **Step 1: Replace `src/App.tsx`** with the complete implementation

```tsx
import { useEffect, useMemo, useState } from 'react'
import type { RenovateMrDto } from './types'
import { groupByRepo, filterGroups, getRepoUrl } from './utils/mr'
import { SearchBar } from './components/SearchBar'
import { RepoGroup } from './components/RepoGroup'

type FetchStatus = 'loading' | 'error' | 'done'

function App() {
  const [mrs, setMrs] = useState<RenovateMrDto[]>([])
  const [status, setStatus] = useState<FetchStatus>('loading')
  const [query, setQuery] = useState('')

  useEffect(() => {
    fetch('/api/mrs')
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        return r.json() as Promise<RenovateMrDto[]>
      })
      .then(data => {
        setMrs(data)
        setStatus('done')
      })
      .catch(() => setStatus('error'))
  }, [])

  const groups = useMemo(() => groupByRepo(mrs), [mrs])
  const filtered = useMemo(() => filterGroups(groups, query), [groups, query])

  if (status === 'loading') {
    return (
      <div className="flex items-center justify-center min-h-screen text-gray-500">
        Loading…
      </div>
    )
  }

  if (status === 'error') {
    return (
      <div className="flex items-center justify-center min-h-screen text-red-600">
        Failed to load MRs. Is the backend running?
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Renovate Dashboard</h1>
        <p className="text-sm text-gray-500 mt-1">
          {mrs.length} open MR{mrs.length !== 1 ? 's' : ''} across {groups.size} repo{groups.size !== 1 ? 's' : ''}
        </p>
      </header>

      <div className="mb-4">
        <SearchBar value={query} onChange={setQuery} />
      </div>

      {filtered.size === 0 ? (
        <p className="text-gray-500 text-sm py-8 text-center">
          {mrs.length === 0
            ? 'No open Renovate MRs found.'
            : 'No MRs match your search.'}
        </p>
      ) : (
        <div className="flex flex-col gap-3">
          {[...filtered.entries()].map(([repo, repoMrs]) => (
            <RepoGroup
              key={repo}
              repo={repo}
              mrs={repoMrs}
              repoUrl={getRepoUrl(repoMrs[0].webUrl)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export default App
```

- [ ] **Step 2: Run tests to make sure utilities still pass**

```bash
cd src/RenovateDashboard.Web
npm test
```

Expected: 9 tests pass.

- [ ] **Step 3: Start the dev server and verify the UI**

```bash
npm run dev
```

Open http://localhost:5173. With the backend not running you should see the error state. With the backend running (`dotnet run` in `src/RenovateDashboard.App`) you should see the dashboard with grouped MRs, the search box filtering in real time, and clicking any row opening the MR in a new tab.

- [ ] **Step 4: Commit**

```bash
git add src/RenovateDashboard.Web/src/App.tsx
git commit -m "feat: wire up MR dashboard — grouped, collapsible, searchable"
```
