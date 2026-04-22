# Frontend MR Table — Design Spec

**Date:** 2026-04-22
**Status:** Approved

## Overview

Update `RenovateDashboard.Web` to fetch and display open Renovate MRs from `/api/mrs`, grouped by repo, with collapsible sections and a search box. Built with React + TypeScript + Tailwind CSS (Tailwind v4 via Vite plugin).

## Architecture

Single-page React app. `App` owns all state (fetch, search query, collapsed repos). No router needed — one view. Components are pure/presentational except `App`.

```
App
├── SearchBar          (controlled input)
└── RepoGroup[]        (one per repo, collapsible)
    └── MrTable
        └── MrRow[]    (clickable rows)
```

## Data Flow

1. `App` fetches `GET /api/mrs` on mount via `useEffect`.
2. Response is `RenovateMrDto[]`:
   ```ts
   interface RenovateMrDto {
     repo: string;
     iid: number;
     title: string;
     webUrl: string;
     createdAt: string; // ISO 8601
     author: string;
     sourceBranch: string;
     targetBranch: string;
     pipelineStatus: string | null;
   }
   ```
3. `App` groups the array by `repo` (preserving API order within each group).
4. Search query filters groups client-side:
   - If `repo` name matches query → show all MRs in that group.
   - Otherwise → show only MRs whose `title` matches query.
   - Matching is case-insensitive substring.
5. Grouped + filtered data passed as props to `RepoGroup` components.

## Components

### `SearchBar`
- Controlled `<input>` with placeholder `"Search by repo or MR title…"`.
- `onChange` updates search query in `App` state. No debounce needed for this data volume.

### `RepoGroup`
- Props: `repo: string`, `mrs: RenovateMrDto[]`, `repoUrl: string`.
- Local `isOpen` state, defaults to `true`.
- Header: chevron icon + repo name (external link to `repoUrl`) + open count badge.
- Clicking the header row (excluding the repo link itself) toggles collapse.
- Repo URL derived in `App` from the first MR's `webUrl`: `webUrl.split('/-/')[0]`.
- When collapsed, only the header is visible.

### `MrTable`
- Renders a `<table>` with columns: **Title · Branch · Age · Pipeline**.
- Header row with column labels.

### `MrRow`
- Props: one `RenovateMrDto`.
- The entire `<tr>` is clickable: `onClick={() => window.open(webUrl, '_blank')}` with `cursor-pointer` styling.
- **Title**: bold text (not a separate `<a>` tag — the row itself is the link).
- **Branch**: `sourceBranch` in monospace, truncated if long.
- **Age**: relative time from `createdAt` (e.g. "3d ago"), computed with a small utility function — no date library.
- **Pipeline**: colored badge — `passed` (green), `failed` (red), `running`/`pending` (amber), `null` → "—" (gray).

## Styling

Tailwind CSS v4 via `@tailwindcss/vite` plugin. No custom CSS beyond what Tailwind provides. Utility classes applied directly in JSX.

Color conventions:
- Repo link: `text-indigo-600`
- MR count badge: `bg-indigo-100 text-indigo-800`
- Pipeline passed: `bg-green-100 text-green-800`
- Pipeline failed: `bg-red-100 text-red-800`
- Pipeline running/pending: `bg-amber-100 text-amber-800`
- Row hover: `hover:bg-gray-50`

## Vite Dev Proxy

`vite.config.ts` gets a proxy entry so the frontend dev server forwards `/api/*` to the .NET backend:

```ts
server: {
  proxy: {
    '/api': 'http://localhost:5000'
  }
}
```

## Tailwind Setup

1. `npm install tailwindcss @tailwindcss/vite`
2. Add plugin to `vite.config.ts`.
3. Replace `index.css` content with `@import "tailwindcss";`.

## Error & Loading States

- **Loading**: show a simple centered spinner or "Loading…" text.
- **Error**: show a brief error message with the status code.
- **Empty** (no MRs after fetch): show "No open Renovate MRs found."
- **No search results**: show "No MRs match your search."

## Out of Scope

- Sorting table columns
- Pagination
- Auto-refresh / polling
- Dark mode
