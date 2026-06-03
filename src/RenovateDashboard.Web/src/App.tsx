import { useEffect, useMemo, useState } from 'react'
import { useAuth0, withAuthenticationRequired } from '@auth0/auth0-react'
import type { RenovateMrDto } from './types'
import { groupByRepo, filterGroups, getRepoUrl } from './utils/mr'
import { SearchBar } from './components/SearchBar'
import { RepoGroup } from './components/RepoGroup'

type FetchStatus = 'loading' | 'error' | 'done'

function App() {
  const { getAccessTokenSilently, logout, user } = useAuth0()
  const [mrs, setMrs] = useState<RenovateMrDto[]>([])
  const [status, setStatus] = useState<FetchStatus>('loading')
  const [query, setQuery] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    ;(async () => {
      try {
        const token = await getAccessTokenSilently()
        const r = await fetch('/api/mrs', {
          signal: controller.signal,
          headers: { Authorization: `Bearer ${token}` },
        })
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        const data = (await r.json()) as RenovateMrDto[]
        setMrs(data)
        setStatus('done')
      } catch (err) {
        if ((err as Error).name !== 'AbortError') {
          console.error('[renovate-dashboard] failed to fetch /api/mrs', err)
          setStatus('error')
        }
      }
    })()
    return () => controller.abort()
  }, [getAccessTokenSilently])

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
      <header className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Renovate Dashboard</h1>
          <p className="text-sm text-gray-500 mt-1">
            {mrs.length} open MR{mrs.length !== 1 ? 's' : ''} across {groups.size} repo{groups.size !== 1 ? 's' : ''}
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          {user?.email && <span className="text-sm text-gray-500">{user.email}</span>}
          <button
            onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
            className="text-sm text-gray-600 hover:text-gray-900 underline"
          >
            Log out
          </button>
        </div>
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

export default withAuthenticationRequired(App, {
  onRedirecting: () => (
    <div className="flex items-center justify-center min-h-screen text-gray-500">
      Redirecting to login…
    </div>
  ),
})
