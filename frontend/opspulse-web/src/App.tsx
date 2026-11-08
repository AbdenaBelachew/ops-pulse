import { Navigate, Route, Routes } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { AlertTriangle, BellRing, FileText, Globe, Mail, ScrollText, Container, Cpu, Database, Server, Ship } from 'lucide-react'
import { AppShell } from './components/layout/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { ComingSoon } from './components/ComingSoon'
import { LoginPage } from './pages/auth/LoginPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { DashboardPage } from './pages/DashboardPage'
import { UsersPage } from './pages/users/UsersPage'
import { OrganizationSettingsPage } from './pages/settings/OrganizationSettingsPage'
import { SystemSettingsPage } from './pages/settings/SystemSettingsPage'

interface ComingSoonRoute {
  path: string
  title: string
  description: string
  milestone: number
  icon: LucideIcon
}

const comingSoonRoutes: ComingSoonRoute[] = [
  {
    path: '/applications',
    title: 'Application Monitoring',
    description: 'Register services, run HTTP checks and track uptime, response times and status transitions.',
    milestone: 3,
    icon: Globe,
  },
  {
    path: '/servers',
    title: 'Server Monitoring',
    description: 'CPU, RAM, disk and network metrics streamed by lightweight OpsPulse agents.',
    milestone: 6,
    icon: Server,
  },
  {
    path: '/databases',
    title: 'Database Monitoring',
    description: 'PostgreSQL and SQL Server availability, connections, size and query statistics.',
    milestone: 7,
    icon: Database,
  },
  {
    path: '/kubernetes',
    title: 'Kubernetes Monitoring',
    description: 'Cluster health, nodes, pods, deployments and restart counts via least-privilege API access.',
    milestone: 10,
    icon: Ship,
  },
  {
    path: '/docker',
    title: 'Docker Monitoring',
    description: 'Container status, uptime, CPU, memory and restart counts across hosts.',
    milestone: 11,
    icon: Container,
  },
  {
    path: '/agents',
    title: 'Monitoring Agents',
    description: 'Deploy and manage the secure agents that collect server and database metrics.',
    milestone: 6,
    icon: Cpu,
  },
  {
    path: '/incidents',
    title: 'Incident Management',
    description: 'Automatic incident creation, acknowledgement, assignment, comments and resolution.',
    milestone: 8,
    icon: AlertTriangle,
  },
  {
    path: '/alert-rules',
    title: 'Alert Rules',
    description: 'Thresholds, evaluation windows, cooldowns and notification channels for every metric.',
    milestone: 9,
    icon: BellRing,
  },
  {
    path: '/notifications',
    title: 'Notifications',
    description: 'In-app and email delivery with preferences, history and retry handling.',
    milestone: 9,
    icon: Mail,
  },
  {
    path: '/reports',
    title: 'Reports & Analytics',
    description: 'Uptime summaries, utilization trends and CSV exports across your infrastructure.',
    milestone: 9,
    icon: FileText,
  },
  {
    path: '/audit-logs',
    title: 'Audit Logs',
    description: 'Who changed what and when — logins, configuration changes and role updates.',
    milestone: 12,
    icon: ScrollText,
  },
]

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/settings/organization" element={<OrganizationSettingsPage />} />
          <Route path="/settings/system" element={<SystemSettingsPage />} />
          {comingSoonRoutes.map((route) => (
            <Route
              key={route.path}
              path={route.path}
              element={
                <ComingSoon
                  title={route.title}
                  description={route.description}
                  milestone={route.milestone}
                  icon={route.icon}
                />
              }
            />
          ))}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App
