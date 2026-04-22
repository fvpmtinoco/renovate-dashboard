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
