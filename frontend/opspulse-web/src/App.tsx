import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { LoginPage } from './pages/auth/LoginPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { DashboardPage } from './pages/DashboardPage'
import { UsersPage } from './pages/users/UsersPage'
import { OrganizationSettingsPage } from './pages/settings/OrganizationSettingsPage'
import { SystemSettingsPage } from './pages/settings/SystemSettingsPage'
import { ApplicationsPage } from './pages/monitoring/ApplicationsPage'
import { ApplicationDetailsPage } from './pages/monitoring/ApplicationDetailsPage'
import { ServersPage } from './pages/monitoring/ServersPage'
import { ServerDetailsPage } from './pages/monitoring/ServerDetailsPage'
import { DatabasesPage } from './pages/monitoring/DatabasesPage'
import { KubernetesPage } from './pages/monitoring/KubernetesPage'
import { DockerPage } from './pages/monitoring/DockerPage'
import { AgentsPage } from './pages/monitoring/AgentsPage'
import { IncidentsPage } from './pages/operations/IncidentsPage'
import { AlertRulesPage } from './pages/operations/AlertRulesPage'
import { NotificationsPage } from './pages/operations/NotificationsPage'
import { ReportsPage } from './pages/operations/ReportsPage'
import { AuditLogsPage } from './pages/operations/AuditLogsPage'
import { LogsPage } from './pages/operations/LogsPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/applications" element={<ApplicationsPage />} />
          <Route path="/applications/:id" element={<ApplicationDetailsPage />} />
          <Route path="/servers" element={<ServersPage />} />
          <Route path="/servers/:id" element={<ServerDetailsPage />} />
          <Route path="/databases" element={<DatabasesPage />} />
          <Route path="/kubernetes" element={<KubernetesPage />} />
          <Route path="/docker" element={<DockerPage />} />
          <Route path="/agents" element={<AgentsPage />} />
          <Route path="/incidents" element={<IncidentsPage />} />
          <Route path="/alert-rules" element={<AlertRulesPage />} />
          <Route path="/notifications" element={<NotificationsPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/audit-logs" element={<AuditLogsPage />} />
          <Route path="/logs" element={<LogsPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/settings/organization" element={<OrganizationSettingsPage />} />
          <Route path="/settings/system" element={<SystemSettingsPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App
