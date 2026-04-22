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

  it('returns "just now" for timestamps in the future (clock skew)', () => {
    expect(relativeTime('2026-04-22T12:01:00Z')).toBe('just now')
  })

  it('returns "just now" for timestamps less than 1 minute ago', () => {
    expect(relativeTime('2026-04-22T11:59:30Z')).toBe('just now')
  })

  it('shows 59m ago at the minute boundary', () => {
    expect(relativeTime('2026-04-22T11:01:00Z')).toBe('59m ago')
  })

  it('shows 1h ago at the hour boundary', () => {
    expect(relativeTime('2026-04-22T11:00:00Z')).toBe('1h ago')
  })

  it('shows 23h ago near the day boundary', () => {
    expect(relativeTime('2026-04-21T13:00:00Z')).toBe('23h ago')
  })

  it('shows 1d ago at the day boundary', () => {
    expect(relativeTime('2026-04-21T12:00:00Z')).toBe('1d ago')
  })
})
