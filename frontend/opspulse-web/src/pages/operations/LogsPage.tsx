import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Terminal } from 'lucide-react'
import { listLogs, type PlatformLogDto } from '../../api/logs'
import { Card } from '../../components/ui/Card'

const levelStyles: Record<string, string> = {
  Fatal: 'text-critical bg-critical/15 border-critical/30',
  Error: 'text-critical bg-critical/10 border-critical/25',
  Warning: 'text-warning bg-warning/10 border-warning/25',
  Information: 'text-info bg-info/10 border-info/25',
  Debug: 'text-slate-400 bg-slate-500/10 border-slate-500/25',
  Trace: 'text-slate-500 bg-slate-500/10 border-slate-500/25',
}


export function LogsPage() {
  const [term, setTerm] = useState('')
  const [search, setSearch] = useState('')
  const [level, setLevel] = useState('')
  const [source, setSource] = useState('')
  const [liveTail, setLiveTail] = useState(false)

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(term), 300)
    return () => window.clearTimeout(timer)
  }, [term])

  const logsQuery = useQuery({
    queryKey: ['logs', search, level, source],
    queryFn: () => listLogs({ page: 1, pageSize: 200, search, level, source }),
    refetchInterval: liveTail ? 3000 : false,
    placeholderData: (p) => p,
  })

  const logs: PlatformLogDto[] = logsQuery.data?.items ?? []

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white">System Logs</h1>
          <p className="mt-1 text-sm text-slate-500">
            Platform logs from the API and Worker, persisted by the Serilog → PostgreSQL pipeline.
          </p>
        </div>
        <label className="flex cursor-pointer items-center gap-2 rounded-lg border border-navy-600 bg-navy-800 px-3 py-2 text-xs font-medium text-slate-300">
          <input
            type="checkbox"
            checked={liveTail}
            onChange={(e) => setLiveTail(e.target.checked)}
            className="h-4 w-4 accent-blue-500"
          />
          <Terminal className="h-3.5 w-3.5" aria-hidden="true" />
          Live tail {liveTail && <span className="flex h-2 w-2 rounded-full bg-healthy" aria-hidden="true" />}
        </label>
      </div>

      <div className="mt-6 flex flex-wrap items-center gap-3">
        <input
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          placeholder="Search messages, sources, exceptions…"
          aria-label="Search logs"
          className="min-w-56 flex-1 rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none"
        />
        <select
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          aria-label="Filter by level"
          className="rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:outline-none"
        >
          <option value="">All levels</option>
          {['Fatal', 'Error', 'Warning', 'Information', 'Debug'].map((option) => (
            <option key={option} value={option}>{option}</option>
          ))}
        </select>
        <select
          value={source}
          onChange={(e) => setSource(e.target.value)}
          aria-label="Filter by source"
          className="max-w-52 rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-300 focus:outline-none"
        >
          <option value="">All sources</option>
          {Array.from(new Set(logs.map((log) => log.source).filter((s): s is string => !!s))).slice(0, 30).map((option) => (
            <option key={option} value={option}>{option}</option>
          ))}
        </select>
      </div>

      <Card className="mt-4 overflow-hidden p-0">
        <div className="flex items-center justify-between border-b border-navy-700 px-4 py-2.5 text-xs text-slate-500">
          <span>{logsQuery.data ? `${logsQuery.data.totalCount} matching entries` : '…'}</span>
          <span className="font-mono">showing newest {logs.length}</span>
        </div>
        <div className="max-h-[65vh] overflow-auto font-mono text-[11px] leading-relaxed">
          {logsQuery.isLoading ? (
            <p className="px-4 py-8 text-center text-slate-600">Loading logs…</p>
          ) : logs.length === 0 ? (
            <p className="px-4 py-8 text-center text-slate-600">No log entries match the filters.</p>
          ) : (
            <table className="w-full text-left">
              <tbody>
                {logs.map((log) => (
                  <tr key={log.id} className="border-b border-navy-800/60 align-top hover:bg-navy-800/40">
                    <td className="whitespace-nowrap px-3 py-1.5 text-slate-600">
                      {new Date(log.timestampUtc).toLocaleTimeString(undefined, { hour12: false })}
                    </td>
                    <td className="px-2 py-1.5">
                      <span className={`inline-block whitespace-nowrap rounded border px-1.5 text-[10px] font-semibold ${levelStyles[log.level] ?? 'border-slate-500/25 text-slate-400'}`}>
                        {log.level}
                      </span>
                    </td>
                    <td className="max-w-24 truncate px-2 py-1.5 text-slate-600" title={log.source ?? ''}>
                      {log.source?.split('.').pop()}
                    </td>
                    <td className="px-2 py-1.5 text-slate-300">
                      {log.message}
                      {log.exception && (
                        <details className="mt-1">
                          <summary className="cursor-pointer text-critical/80">exception</summary>
                          <pre className="mt-1 max-h-48 overflow-auto whitespace-pre-wrap rounded bg-navy-950 p-2 text-[10px] text-critical/70">{log.exception}</pre>
                        </details>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-2 py-1.5 text-right text-[10px] text-slate-700">{log.process}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </div>
  )
}
