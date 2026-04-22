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
  const idx = webUrl.indexOf('/-/')
  return idx !== -1 ? webUrl.slice(0, idx) : webUrl
}
