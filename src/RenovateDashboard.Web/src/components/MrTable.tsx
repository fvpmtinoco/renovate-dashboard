import type { RenovateMrDto } from '../types'
import { MrRow } from './MrRow'

export function MrTable({ mrs }: { mrs: RenovateMrDto[] }) {
  return (
    <table className="w-full table-fixed border-collapse text-sm">
      <thead>
        <tr className="bg-gray-50 text-left text-xs text-gray-500 font-medium uppercase tracking-wide">
          <th className="px-4 py-2 w-[45%]">Title</th>
          <th className="px-4 py-2 w-[30%]">Branch</th>
          <th className="px-4 py-2 w-[12%]">Age</th>
          <th className="px-4 py-2 w-[13%]">Pipeline</th>
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
