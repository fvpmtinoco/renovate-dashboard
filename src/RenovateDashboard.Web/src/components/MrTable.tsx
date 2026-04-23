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
