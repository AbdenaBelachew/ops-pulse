import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ScrollText, Search } from 'lucide-react'
import { listAuditLogs } from '../../api/operations'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'

export function AuditLogsPage() {
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearch(term)
      setPage(1)
    }, 300)
    return () => window.clearTimeout(timer)
  }, [term])

  const auditQuery = useQuery({
    queryKey: ['audit-logs', search, page],
    queryFn: () => listAuditLogs({ page, pageSize: 50, search }),
    placeholderData: (p) => p,
  })

  return (
    <div>
      <div>
        <h1 className="text-xl font-semibold text-white">Audit Logs</h1>
        <p className="mt-1 text-sm text-slate-500">Security-relevant activity: logins, configuration and user changes.</p>
      </div>

      <div className="relative mt-6 max-w-md">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-600" />
        <input
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          placeholder="Search by action, user or details…"
          aria-label="Search audit logs"
          className="w-full rounded-lg border border-navy-600 bg-navy-900 py-2 pl-9 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
        />
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-navy-700 text-xs text-slate-500">
                <th scope="col" className="px-5 py-3 font-medium">Time</th>
                <th scope="col" className="px-5 py-3 font-medium">User</th>
                <th scope="col" className="px-5 py-3 font-medium">Action</th>
                <th scope="col" className="px-5 py-3 font-medium">Entity</th>
                <th scope="col" className="px-5 py-3 font-medium">Details</th>
                <th scope="col" className="px-5 py-3 font-medium">IP</th>
              </tr>
            </thead>
            <tbody>
              {auditQuery.isLoading ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-slate-600">Loading audit logs…</td></tr>
              ) : (auditQuery.data?.items.length ?? 0) === 0 ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-sm text-slate-500"><ScrollText className="mx-auto mb-2 h-6 w-6 text-slate-700" />No audit entries found.</td></tr>
              ) : (
                auditQuery.data?.items.map((entry) => (
                  <tr key={entry.id} className="border-b border-navy-800 last:border-0 hover:bg-navy-800/40">
                    <td className="whitespace-nowrap px-5 py-2.5 text-xs text-slate-500">{new Date(entry.occurredAtUtc).toLocaleString()}</td>
                    <td className="px-5 py-2.5 text-xs text-slate-400">{entry.userEmail ?? 'system'}</td>
                    <td className="px-5 py-2.5">
                      <span className="rounded bg-navy-700 px-2 py-0.5 font-mono text-[11px] text-slate-300">{entry.action}</span>
                    </td>
                    <td className="px-5 py-2.5 text-xs text-slate-500">{entry.entityType ?? '—'}</td>
                    <td className="max-w-xs truncate px-5 py-2.5 text-xs text-slate-500">{entry.details ?? '—'}</td>
                    <td className="px-5 py-2.5 font-mono text-[11px] text-slate-600">{entry.ipAddress ?? '—'}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        {!auditQuery.isLoading && auditQuery.data && auditQuery.data.totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-navy-700 px-5 py-3 text-xs text-slate-500">
            <span>Page {auditQuery.data.page} of {auditQuery.data.totalPages} · {auditQuery.data.totalCount} entries</span>
            <div className="flex gap-2">
              <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
              <Button variant="secondary" disabled={page >= auditQuery.data!.totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
            </div>
          </div>
        )}
      </Card>
    </div>
  )
}
