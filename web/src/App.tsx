import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter as Router, Link, Navigate, Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import { AppShell, activeRole, type UserRole } from './components/AppShell';
import { DashboardPage } from './pages/DashboardPage';
import { AccountsPage } from './pages/AccountsPage';
import { ActivityPage } from './pages/ActivityPage';
import { ApprovalsPage } from './pages/ApprovalsPage';
import {
  AuditPage,
  BankPage,
  CounterpartiesPage,
  IntegrationsPage,
  ObligationsPage,
  PoliciesPage,
  AdminPage,
} from './pages/ReferencePages';
import './index.css';

const queryClient = new QueryClient();

const roleRoute = (roles: UserRole[], element: ReactNode) =>
  roles.includes(activeRole) ? element : <Navigate to="/dashboard" replace />;

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Routes>
          <Route path="/" element={<AppShell />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <DashboardPage />)} />
            <Route path="activity" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ActivityPage />)} />
            <Route path="activity/:eventId" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ActivityPage />)} />
            <Route path="approvals" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ApprovalsPage />)} />
            <Route path="approvals/:id" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ApprovalsPage />)} />
            <Route path="policies" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <PoliciesPage />)} />
            <Route path="policies/:policyId" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <PoliciesPage />)} />
            <Route path="counterparties" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <CounterpartiesPage />)} />
            <Route path="counterparties/:counterpartyId" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <CounterpartiesPage />)} />
            <Route path="obligations" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ObligationsPage />)} />
            <Route path="obligations/:obligationId" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <ObligationsPage />)} />
            <Route path="accounts" element={roleRoute(['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <AccountsPage />)} />
            <Route path="integrations" element={roleRoute(['BusinessAdmin', 'PlatformAdmin'], <IntegrationsPage />)} />
            <Route path="audit" element={roleRoute(['BusinessAdmin', 'BankAdmin', 'PlatformAdmin'], <AuditPage />)} />
            <Route path="bank/*" element={roleRoute(['BankAdmin', 'PlatformAdmin'], <BankPage />)} />
            <Route path="admin/*" element={roleRoute(['PlatformAdmin'], <AdminPage />)} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </Router>
    </QueryClientProvider>
  );
}

const NotFoundPage = () => (
  <section className="panel mx-auto max-w-xl p-8 text-center">
    <p className="eyebrow">404</p>
    <h1 className="mt-2 text-2xl font-semibold">Page not found</h1>
    <Link className="mt-5 inline-block text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-300" to="/dashboard">Return to dashboard</Link>
  </section>
);

export default App;
