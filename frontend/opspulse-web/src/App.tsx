import { Navigate, Route, Routes } from 'react-router-dom'
import OverviewPage from './pages/OverviewPage'

function App() {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b border-navy-700 bg-navy-900">
        <div className="mx-auto flex h-14 w-full max-w-6xl items-center justify-between px-6">
          <div className="flex items-center gap-2.5">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-info/15 text-info">
              <svg
                className="h-4.5 w-4.5"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <path d="M22 12h-2.48a2 2 0 0 0-1.93 1.46l-2.35 8.36a.25.25 0 0 1-.48 0L9.24 2.18a.25.25 0 0 0-.48 0l-2.35 8.36A2 2 0 0 1 4.49 12H2" />
              </svg>
            </span>
            <div className="leading-tight">
              <p className="text-sm font-semibold text-white">OpsPulse</p>
              <p className="text-[11px] text-slate-500">Infrastructure Monitoring</p>
            </div>
          </div>
          <span className="rounded-full border border-navy-600 bg-navy-800 px-2.5 py-1 text-[11px] font-medium text-slate-400">
            Milestone 1 — Foundation
          </span>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
        <Routes>
          <Route path="/" element={<OverviewPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>

      <footer className="border-t border-navy-700 bg-navy-900">
        <div className="mx-auto w-full max-w-6xl px-6 py-3 text-[11px] text-slate-600">
          OpsPulse — ASP.NET Core 10 · React 19 · PostgreSQL · Redis
        </div>
      </footer>
    </div>
  )
}

export default App
