import { NavLink } from 'react-router-dom'
import {
  AlertTriangle,
  BellRing,
  Building2,
  Container,
  Cpu,
  Database,
  FileText,
  Globe,
  LayoutDashboard,
  Mail,
  ScrollText,
  Server,
  Settings,
  Ship,
  Users,
  type LucideIcon,
} from 'lucide-react'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
  milestone: number
}

interface NavSection {
  heading: string | null
  items: NavItem[]
}

const sections: NavSection[] = [
  {
    heading: null,
    items: [{ to: '/', label: 'Overview', icon: LayoutDashboard, milestone: 1 }],
  },
  {
    heading: 'Monitoring',
    items: [{ to: '/applications', label: 'Applications', icon: Globe, milestone: 3 }],
  },
  {
    heading: 'Infrastructure',
    items: [
      { to: '/servers', label: 'Servers', icon: Server, milestone: 6 },
      { to: '/databases', label: 'Databases', icon: Database, milestone: 7 },
      { to: '/kubernetes', label: 'Kubernetes', icon: Ship, milestone: 10 },
      { to: '/docker', label: 'Docker', icon: Container, milestone: 11 },
      { to: '/agents', label: 'Monitoring Agents', icon: Cpu, milestone: 6 },
    ],
  },
  {
    heading: 'Operations',
    items: [
      { to: '/incidents', label: 'Incidents', icon: AlertTriangle, milestone: 8 },
      { to: '/alert-rules', label: 'Alert Rules', icon: BellRing, milestone: 9 },
      { to: '/notifications', label: 'Notifications', icon: Mail, milestone: 9 },
      { to: '/reports', label: 'Reports', icon: FileText, milestone: 9 },
    ],
  },
  {
    heading: 'Administration',
    items: [
      { to: '/audit-logs', label: 'Audit Logs', icon: ScrollText, milestone: 12 },
      { to: '/users', label: 'Users', icon: Users, milestone: 2 },
      { to: '/settings/organization', label: 'Organization', icon: Building2, milestone: 2 },
      { to: '/settings/system', label: 'System Settings', icon: Settings, milestone: 2 },
    ],
  },
]

export function Sidebar() {
  return (
    <aside className="flex h-full w-60 shrink-0 flex-col border-r border-navy-700 bg-navy-900">
      <div className="flex h-14 shrink-0 items-center gap-2.5 border-b border-navy-700 px-5">
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

      <nav className="flex-1 overflow-y-auto px-3 py-4" aria-label="Main navigation">
        {sections.map((section, sectionIndex) => (
          <div key={sectionIndex} className="mb-4 last:mb-0">
            {section.heading && (
              <p className="mb-1.5 px-3 text-[10px] font-semibold uppercase tracking-wider text-slate-600">
                {section.heading}
              </p>
            )}
            <ul className="space-y-0.5">
              {section.items.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.to === '/'}
                    className={({ isActive }) =>
                      `flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors ${
                        isActive
                          ? 'bg-navy-800 font-medium text-white'
                          : 'text-slate-400 hover:bg-navy-800/60 hover:text-slate-200'
                      }`
                    }
                  >
                    <item.icon className="h-4 w-4 shrink-0" aria-hidden="true" />
                    <span className="flex-1 truncate">{item.label}</span>
                    {item.milestone > 2 && (
                      <span
                        className="rounded border border-navy-600 px-1 text-[9px] leading-4 text-slate-600"
                        title={`Arrives in Milestone ${item.milestone}`}
                      >
                        M{item.milestone}
                      </span>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>
    </aside>
  )
}
