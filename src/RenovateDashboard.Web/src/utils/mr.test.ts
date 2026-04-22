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
