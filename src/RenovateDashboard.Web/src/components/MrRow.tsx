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
  const open = () => window.open(mr.webUrl, '_blank', 'noopener,noreferrer')
  return (
    <tr
      className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
      onClick={open}
      onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && open()}
      tabIndex={0}
      role="link"
      aria-label={mr.title}
    >
      <td className="px-4 py-2.5 font-medium text-gray-900">{mr.title}</td>
      <td className="px-4 py-2.5 font-mono text-xs text-gray-500 truncate max-w-[200px]">
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
