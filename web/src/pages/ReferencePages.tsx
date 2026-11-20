import { useMemo } from 'react';
import { Link, useLocation, useParams, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowUpRight, LockKeyhole } from 'lucide-react';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { useAccounts } from '../hooks/useAccounts';
import { useDashboardActivity } from '../hooks/useDashboardActivity';
import { apiClient } from '../lib/api';

interface PolicyRecord {
  id: string;
  accountId: string;
  name: string;
  status: string | number;
  version: number;
  effectiveFrom: string;
  updatedAt: string;
}

interface CounterpartyRecord {
  id: string;
  businessCustomerId: string;
  name: string;
  status: string | number;
  source: string | number;
  achCompanyIds: string[];
  createdAt: string;
}

const formatDate = (date: string) => new Date(date).toLocaleDateString();
const money = (amount: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

export const PoliciesPage = () => {
  const [searchParams] = useSearchParams();
  const selectedAccountId = searchParams.get('account');
  const { data: accounts, isLoading: accountsLoading, error: accountsError } = useAccounts();
  const targetAccounts = useMemo(
    () => (accounts ?? []).filter((account) => !selectedAccountId || account.id === selectedAccountId),
    [accounts, selectedAccountId]
  );
  const policies = useQuery({
    queryKey: ['presentation', 'policies', targetAccounts.map((account) => account.id).join(',')],
    queryFn: async () => {
      const responses = await Promise.all(targetAccounts.map((account) =>
        apiClient.get<PolicyRecord[]>(`/policies/account/${account.id}`)
      ));
      return responses.flatMap((response) => response.data);
    },
    enabled: !accountsLoading && !accountsError,
  });
  const { policyId } = useParams();

  if (accountsLoading || policies.isLoading) return <LoadingSpinner />;
  if (accountsError || policies.error) return <DataError label="policies" />;

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div><p className="eyebrow">Controls</p><h1>Rules</h1><p>Review and manage account rule versions and activation status.</p></div>
        <span className="subtle-count"><LockKeyhole size={16} /> Account-scoped</span>
      </header>
      <section className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Policy</th><th>Account</th><th>Version</th><th>Status</th><th>Updated</th></tr></thead>
            <tbody>
              {(policies.data ?? []).map((policy) => (
                <tr key={policy.id} className={policy.id === policyId ? 'row-selected' : ''}>
                  <td><Link className="entity-link" to={`/policies/${policy.id}`}>{policy.name}</Link></td>
                  <td>{accounts?.find((account) => account.id === policy.accountId)?.displayName ?? 'Unknown account'}</td>
                  <td>v{policy.version}</td>
                  <td><StatusPill value={enumLabel(policy.status, ['Draft', 'Active', 'Archived'])} /></td>
                  <td>{formatDate(policy.updatedAt)}</td>
                </tr>
              ))}
              {(policies.data ?? []).length === 0 && <tr><td colSpan={5} className="empty-row">No policies are configured for this account.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>
      {policyId && <DetailPanel title="Policy details" id={policyId} closeTo="/policies"><p>Policy activation and approval history are not currently exposed by the API.</p></DetailPanel>}
    </div>
  );
};

export const CounterpartiesPage = () => {
  const [searchParams] = useSearchParams();
  const selectedAccountId = searchParams.get('account');
  const { data: accounts, isLoading: accountsLoading, error: accountsError } = useAccounts();
  const businessIds = useMemo(() => [...new Set((accounts ?? [])
    .filter((account) => !selectedAccountId || account.id === selectedAccountId)
    .map((account) => account.businessCustomerId))], [accounts, selectedAccountId]);
  const counterparties = useQuery({
    queryKey: ['presentation', 'counterparties', businessIds.join(',')],
    queryFn: async () => {
      const responses = await Promise.all(businessIds.map((id) =>
        apiClient.get<CounterpartyRecord[]>(`/counterparties/business/${id}`)
      ));
      return responses.flatMap((response) => response.data);
    },
    enabled: !accountsLoading && !accountsError,
  });
  const { counterpartyId } = useParams();

  if (accountsLoading || counterparties.isLoading) return <LoadingSpinner />;
  if (accountsError || counterparties.error) return <DataError label="counterparties" />;

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div><p className="eyebrow">Directory</p><h1>Counterparties</h1><p>Payees and originators associated with protected accounts.</p></div>
        <span className="subtle-count">{counterparties.data?.length ?? 0} counterparties</span>
      </header>
      <section className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Name</th><th>Status</th><th>Source</th><th>ACH company IDs</th><th>Added</th></tr></thead>
            <tbody>
              {(counterparties.data ?? []).map((counterparty) => (
                <tr key={counterparty.id} className={counterparty.id === counterpartyId ? 'row-selected' : ''}>
                  <td><Link className="entity-link" to={`/counterparties/${counterparty.id}`}>{counterparty.name}</Link></td>
                  <td><StatusPill value={enumLabel(counterparty.status, ['Pending', 'Approved', 'Blocked'])} /></td>
                  <td>{enumLabel(counterparty.source, ['Manual', 'Accounting', 'Observed'])}</td>
                  <td>{counterparty.achCompanyIds.length ? counterparty.achCompanyIds.join(', ') : '—'}</td>
                  <td>{formatDate(counterparty.createdAt)}</td>
                </tr>
              ))}
              {(counterparties.data ?? []).length === 0 && <tr><td colSpan={5} className="empty-row">No counterparties found for this account.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>
      {counterpartyId && <DetailPanel title="Counterparty details" id={counterpartyId} closeTo="/counterparties"><p>Change history and linked event detail are not currently exposed on this page.</p></DetailPanel>}
    </div>
  );
};

export const ObligationsPage = () => {
  const [searchParams] = useSearchParams();
  const { data, isLoading, error } = useDashboardActivity(searchParams.get('account') ?? undefined);
  const { obligationId } = useParams();

  if (isLoading) return <LoadingSpinner />;
  if (error || !data) return <DataError label="obligations" />;

  const selectedObligation = obligationId
    ? data.expectedPayments.find((item) => item.id === obligationId)
    : undefined;
  const matchedPayment = selectedObligation?.matchedPaymentEventId
    ? data.payments.find((payment) => payment.id === selectedObligation.matchedPaymentEventId)
    : undefined;

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div><p className="eyebrow">Expected payments</p><h1>Obligations</h1><p>Expected invoices and their matching status.</p></div>
        <span className="subtle-count">{data.expectedPayments.length} records</span>
      </header>
      <section className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Counterparty</th><th>Invoice reference</th><th>Source</th><th>Due date</th><th>Status</th><th className="text-right">Amount</th></tr></thead>
            <tbody>
              {data.expectedPayments.map((item) => (
                <tr key={item.id} className={item.id === obligationId ? 'row-selected' : ''}>
                  <td><Link className="entity-link" to={`/obligations/${item.id}`}>{item.counterpartyName}</Link></td>
                  <td>{item.sourceRef}</td><td>{item.source}</td><td>{formatDate(item.dueDate)}</td>
                  <td><StatusPill value={item.status} /></td><td className="text-right font-semibold tabular-nums">{money(item.amount)}</td>
                </tr>
              ))}
              {data.expectedPayments.length === 0 && <tr><td colSpan={6} className="empty-row">No expected obligations are recorded.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>
      {obligationId && (
        <DetailPanel title="Bill details" id={obligationId} closeTo="/obligations">
          {selectedObligation ? (
            <dl className="grid gap-4 sm:grid-cols-2">
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Bill / counterparty</dt>
                <dd className="mt-1 font-semibold">{selectedObligation.counterpartyName}</dd>
              </div>
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Invoice reference</dt>
                <dd className="mt-1 font-mono text-sm">{selectedObligation.sourceRef}</dd>
              </div>
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Amount</dt>
                <dd className="mt-1 font-semibold">{money(selectedObligation.amount)}</dd>
              </div>
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Due date</dt>
                <dd className="mt-1">{formatDate(selectedObligation.dueDate)}</dd>
              </div>
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Source</dt>
                <dd className="mt-1">{selectedObligation.source}</dd>
              </div>
              <div>
                <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Match status</dt>
                <dd className="mt-1"><StatusPill value={selectedObligation.status} /></dd>
              </div>
              {matchedPayment && (
                <>
                  <div className="sm:col-span-2">
                    <dt className="text-xs font-bold uppercase tracking-wide text-slate-500">Matched payment</dt>
                    <dd className="mt-2 flex flex-wrap items-center gap-3">
                      <Link className="entity-link" to={`/activity/${matchedPayment.id}`}>
                        {matchedPayment.reference ?? matchedPayment.id}
                      </Link>
                      <span>{matchedPayment.rail} / {matchedPayment.initiation}</span>
                      <span className="font-semibold">{money(matchedPayment.amount)}</span>
                      <Link className="text-indigo-600 hover:underline dark:text-indigo-300" to={`/accounts?account=${matchedPayment.accountId}`}>
                        {matchedPayment.accountName}
                      </Link>
                    </dd>
                  </div>
                </>
              )}
            </dl>
          ) : (
            <p>This bill could not be found in the current account view.</p>
          )}
        </DetailPanel>
      )}
    </div>
  );
};

interface ModernTreasuryCounterparty {
  id: string;
  name: string;
  status: string;
  source: string;
  routingAccountFingerprints: string[];
  createdAt: string;
  updatedAt: string;
}

interface ModernTreasuryPayment {
  id: string;
  externalEventId: string | null;
  accountId: string;
  rail: string;
  direction: string;
  initiation: string;
  amount: number;
  currency: string;
  counterpartyRef: string | null;
  reference: string | null;
  memo: string | null;
  invoiceRef: string | null;
  eventTime: string;
  settlementDate: string | null;
}

interface JackHenryAccount {
  id: string;
  name: string;
  maskedAccountNumber: string | null;
  accountType: string | null;
  currency: string;
  balance: number | null;
  status: string;
}

export const IntegrationsPage = () => {
  const [searchParams] = useSearchParams();
  const accountId = searchParams.get('account');
  const accountQuery = accountId ? `?accountId=${encodeURIComponent(accountId)}` : '';
  const counterparties = useQuery({
    queryKey: ['modern-treasury', 'counterparties', accountId],
    queryFn: async () => {
      const response = await apiClient.get<ModernTreasuryCounterparty[]>(
        `/modern-treasury/counterparties${accountQuery}`
      );
      return response.data;
    },
  });
  const paymentOrders = useQuery({
    queryKey: ['modern-treasury', 'payment-orders', accountId],
    queryFn: async () => {
      const response = await apiClient.get<ModernTreasuryPayment[]>(
        `/modern-treasury/payment-orders${accountQuery}`
      );
      return response.data;
    },
  });
  const incomingPayments = useQuery({
    queryKey: ['modern-treasury', 'incoming-payment-details', accountId],
    queryFn: async () => {
      const response = await apiClient.get<ModernTreasuryPayment[]>(
        `/modern-treasury/incoming-payment-details${accountQuery}`
      );
      return response.data;
    },
  });
  const jackHenryCounterparties = useQuery({
    queryKey: ['jack-henry', 'counterparties', accountId],
    queryFn: async () => {
      const response = await apiClient.get<ModernTreasuryCounterparty[]>(
        `/jack-henry/counterparties${accountQuery}`
      );
      return response.data;
    },
  });
  const jackHenryPayments = useQuery({
    queryKey: ['jack-henry', 'transactions', accountId],
    queryFn: async () => {
      const response = await apiClient.get<ModernTreasuryPayment[]>(
        `/jack-henry/transactions${accountQuery}`
      );
      return response.data;
    },
  });
  const jackHenryAccounts = useQuery({
    queryKey: ['jack-henry', 'accounts', accountId],
    queryFn: async () => {
      const response = await apiClient.get<JackHenryAccount[]>(
        `/jack-henry/accounts${accountQuery}`
      );
      return response.data;
    },
  });

  const loading = counterparties.isLoading || paymentOrders.isLoading || incomingPayments.isLoading
    || jackHenryCounterparties.isLoading || jackHenryPayments.isLoading;

  if (loading) return <LoadingSpinner />;
  const payments = [...(paymentOrders.data ?? []), ...(incomingPayments.data ?? [])]
    .sort((left, right) => new Date(right.eventTime).getTime() - new Date(left.eventTime).getTime());

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div><p className="eyebrow">Payment operations</p><h1>Modern Treasury</h1><p>Sandbox counterparties and read-only transactions.</p></div>
        <span className="subtle-count">Sandbox · Read only</span>
      </header>

      <section className="grid gap-4 sm:grid-cols-3">
        <div className="panel p-5"><p className="eyebrow">Counterparties</p><p className="mt-2 text-2xl font-semibold">{counterparties.data?.length ?? 0}</p></div>
        <div className="panel p-5"><p className="eyebrow">Payment orders</p><p className="mt-2 text-2xl font-semibold">{paymentOrders.data?.length ?? 0}</p></div>
        <div className="panel p-5"><p className="eyebrow">Incoming payments</p><p className="mt-2 text-2xl font-semibold">{incomingPayments.data?.length ?? 0}</p></div>
      </section>

      <section className="panel overflow-hidden">
        <div className="border-b border-slate-200 px-5 py-4 dark:border-slate-800"><h2 className="font-semibold">Transactions</h2></div>
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Time</th><th>Rail</th><th>Direction</th><th>Counterparty</th><th>Reference</th><th>Status</th><th className="text-right">Amount</th></tr></thead>
            <tbody>
              {payments.map((payment) => (
                <tr key={`${payment.externalEventId ?? payment.id}:${payment.direction}`}>
                  <td>{formatDate(payment.eventTime)}</td>
                  <td>{payment.rail}</td>
                  <td>{payment.direction}</td>
                  <td>{payment.counterpartyRef ?? '—'}</td>
                  <td className="font-mono text-xs">{payment.reference ?? payment.externalEventId ?? '—'}</td>
                  <td>{payment.memo ?? '—'}</td>
                  <td className="text-right font-semibold tabular-nums">{money(payment.amount)}</td>
                </tr>
              ))}
              {payments.length === 0 && <tr><td colSpan={7} className="empty-row">No sandbox payment orders or incoming payments are available.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      <section className="panel overflow-hidden">
        <div className="border-b border-slate-200 px-5 py-4 dark:border-slate-800"><h2 className="font-semibold">Counterparties</h2></div>
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Name</th><th>Status</th><th>Source</th><th>Account fingerprints</th><th>Created</th></tr></thead>
            <tbody>
              {(counterparties.data ?? []).map((counterparty) => (
                <tr key={counterparty.id}>
                  <td className="font-medium">{counterparty.name}</td>
                  <td><StatusPill value={counterparty.status} /></td>
                  <td>{counterparty.source}</td>
                  <td>{counterparty.routingAccountFingerprints.join(', ') || '—'}</td>
                  <td>{formatDate(counterparty.createdAt)}</td>
                </tr>
              ))}
              {(counterparties.data ?? []).length === 0 && <tr><td colSpan={5} className="empty-row">No sandbox counterparties are available.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      <section className="panel overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-800">
          <h2 className="font-semibold">Jack Henry Payments Orchestrator</h2>
          <span className="subtle-count">Staging · Read only</span>
        </div>
        {jackHenryAccounts.error ? (
          <div className="p-6 text-sm text-slate-600 dark:text-slate-300">
            Sandbox accounts could not be loaded. Configure Jack Henry staging credentials and press F5.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="data-table">
              <thead><tr><th>Account</th><th>Type</th><th>Mask</th><th>Currency</th><th>Status</th><th className="text-right">Balance</th></tr></thead>
              <tbody>
                {(jackHenryAccounts.data ?? []).map((account) => (
                  <tr key={account.id}>
                    <td>
                      <div className="font-medium">{account.name}</div>
                      <div className="font-mono text-xs text-slate-500">{account.id}</div>
                    </td>
                    <td>{account.accountType ?? '—'}</td>
                    <td className="font-mono text-xs">{account.maskedAccountNumber ?? '—'}</td>
                    <td>{account.currency}</td>
                    <td><StatusPill value={account.status} /></td>
                    <td className="text-right font-semibold tabular-nums">
                      {account.balance === null ? '—' : money(account.balance)}
                    </td>
                  </tr>
                ))}
                {(jackHenryAccounts.data ?? []).length === 0
                  && <tr><td colSpan={6} className="empty-row">No Jack Henry staging accounts are available.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="panel overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-800">
          <h2 className="font-semibold">Jack Henry staging transactions</h2>
        </div>
        {jackHenryCounterparties.error || jackHenryPayments.error ? (
          <div className="p-6 text-sm text-slate-600 dark:text-slate-300">
            Not connected. Configure <code>JackHenry:ApiKey</code>, <code>JackHenry:PrivateKeyPath</code>,
            and <code>JackHenry:Mode=Live</code> to enable read-only staging access.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="data-table">
              <thead><tr><th>Time</th><th>Rail</th><th>Direction</th><th>Counterparty</th><th>Reference</th><th>Status</th><th className="text-right">Amount</th></tr></thead>
              <tbody>
                {(jackHenryPayments.data ?? []).map((payment) => (
                  <tr key={`${payment.externalEventId ?? payment.id}:${payment.direction}`}>
                    <td>{formatDate(payment.eventTime)}</td>
                    <td>{payment.rail}</td>
                    <td>{payment.direction}</td>
                    <td>{payment.counterpartyRef ?? '—'}</td>
                    <td className="font-mono text-xs">{payment.reference ?? payment.externalEventId ?? '—'}</td>
                    <td>{payment.memo ?? '—'}</td>
                    <td className="text-right font-semibold tabular-nums">{money(payment.amount)}</td>
                  </tr>
                ))}
                {(jackHenryPayments.data ?? []).length === 0
                  && <tr><td colSpan={7} className="empty-row">No Jack Henry staging transactions are available.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
};

export const AuditPage = () => (
  <div className="space-y-6">
    <header className="page-heading">
      <div><p className="eyebrow">Bank governance</p><h1>Audit</h1><p>Payment and policy activity for the bank portfolio.</p></div>
    </header>
    <section className="panel overflow-hidden">
      <table className="data-table">
        <thead><tr><th>Time</th><th>Activity</th><th>Customer</th><th>Outcome</th></tr></thead>
        <tbody>
          <tr><td>Today</td><td>Instant eligibility review</td><td>Northstar Components</td><td>38 potential payments</td></tr>
          <tr><td>Today</td><td>ACH payment monitored</td><td>Apex Facilities Group</td><td>17 ACH payments</td></tr>
          <tr><td>Today</td><td>Fallback review</td><td>Meridian Supply Co.</td><td>1 fallback</td></tr>
        </tbody>
      </table>
    </section>
  </div>
);

export const BankPage = () => {
  const location = useLocation();
  const isCustomers = location.pathname.endsWith('/customers');
  const isBaseline = location.pathname.endsWith('/baseline');

  if (isCustomers) {
    return (
      <div className="space-y-6">
        <header className="page-heading">
          <div><p className="eyebrow">Bank portfolio</p><h1>Business customers</h1><p>Customer-level activity and payment volume.</p></div>
          <Link className="subtle-count" to="/dashboard">Portfolio dashboard</Link>
        </header>
        <section className="grid gap-4 md:grid-cols-3">
          {[
            ['Northstar Components', '38 Instant · 18 ACH · 18 Wire'],
            ['Apex Facilities Group', '31 Instant · 17 ACH · 16 Wire'],
            ['Meridian Supply Co.', '33 Instant · 15 ACH · 17 Wire'],
          ].map(([name, activity]) => (
            <article key={name} className="panel p-5">
              <h2 className="font-semibold">{name}</h2>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{activity}</p>
              <Link className="mt-4 inline-block text-sm font-medium text-indigo-600 hover:underline" to="/dashboard">View portfolio activity</Link>
            </article>
          ))}
        </section>
      </div>
    );
  }

  if (isBaseline) {
    return (
      <div className="space-y-6">
        <header className="page-heading">
          <div><p className="eyebrow">Bank controls</p><h1>Baseline policies</h1><p>Portfolio-wide payment guardrails.</p></div>
        </header>
        <section className="grid gap-4 md:grid-cols-3">
          {[
            ['Instant eligibility', 'R/T and DDA eligibility review', 'Active'],
            ['ACH conversion', 'Potential conversion opportunity', 'Monitor'],
            ['Fallback review', 'Instant-to-ACH fallback monitoring', 'Active'],
          ].map(([name, description, status]) => (
            <article key={name} className="panel p-5">
              <div className="flex items-start justify-between gap-3"><h2 className="font-semibold">{name}</h2><span className="status-pill status-positive">{status}</span></div>
              <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">{description}</p>
            </article>
          ))}
        </section>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div><p className="eyebrow">Bank workspace</p><h1>Bank overview</h1><p>Portfolio-wide payment operations across business customers.</p></div>
        <Link className="subtle-count" to="/dashboard">Open portfolio dashboard</Link>
      </header>
      <section className="grid gap-4 md:grid-cols-3">
        {[
          ['Customers', '/bank/customers', 'Review business customer activity and accounts'],
          ['Baseline policies', '/bank/baseline', 'Configure portfolio-wide payment guardrails'],
          ['Audit', '/audit', 'Review operational and decision history'],
        ].map(([title, path, description]) => (
          <Link key={path} className="panel block p-5 transition hover:border-indigo-300" to={path}>
            <h2 className="font-semibold">{title}</h2>
            <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{description}</p>
          </Link>
        ))}
      </section>
    </div>
  );
};

export const AdminPage = () => (
  <UnavailablePage title="Platform administration" description="Tenant, simulator, and system-health APIs are not available yet." />
);

const StatusPill = ({ value }: { value: string }) => {
  const normalized = value.toLowerCase();
  const style = normalized === 'active' || normalized === 'approved' || normalized === 'paid'
    ? 'status-positive'
    : normalized === 'blocked' || normalized === 'rejected' || normalized === 'expired'
      ? 'status-danger'
      : 'status-neutral';
  return <span className={`status-pill ${style}`}>{value}</span>;
};

const enumLabel = (value: string | number, labels: string[]) =>
  typeof value === 'number' ? labels[value] ?? `Unknown (${value})` : value;

const DetailPanel = ({ title, id, closeTo, children }: { title: string; id: string; closeTo: string; children: React.ReactNode }) => (
  <section className="panel p-5 sm:p-6">
    <div className="flex items-start justify-between gap-4">
      <div><p className="eyebrow">Selected record</p><h2 className="text-lg font-semibold">{title}</h2><p className="mt-1 break-all font-mono text-xs text-slate-500">{id}</p></div>
      <Link to={closeTo} className="text-sm text-indigo-600 hover:underline dark:text-indigo-300">Close</Link>
    </div>
    <div className="mt-4 text-sm text-slate-600 dark:text-slate-300">{children}</div>
  </section>
);

const DataError = ({ label }: { label: string }) => (
  <p role="alert" className="text-sm text-red-700 dark:text-red-300">Could not load {label}. Check the API connection and retry.</p>
);

const UnavailablePage = ({ title, description }: { title: string; description: string }) => (
  <section className="panel mx-auto max-w-2xl p-8 text-center">
    <p className="eyebrow">Not connected</p>
    <h1 className="mt-2 text-2xl font-semibold">{title}</h1>
    <p className="mx-auto mt-3 max-w-lg text-sm text-slate-600 dark:text-slate-300">{description}</p>
    <Link className="mt-6 inline-flex items-center gap-2 text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-300" to="/dashboard">
      Return to dashboard <ArrowUpRight size={15} />
    </Link>
  </section>
);
