import React, { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { Download } from 'lucide-react';
import { CartesianGrid, Line, LineChart, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { useAccounts } from '../hooks/useAccounts';
import { useUpdateAccountMode } from '../hooks/useAccounts';
import { useDashboardActivity, type PaymentActivity } from '../hooks/useDashboardActivity';
import { apiClient } from '../lib/api';


const railCategory = (rail: string) => rail.toLowerCase() === 'instant' ? 'instant'
  : rail.toLowerCase() === 'rtp' ? 'instant'
  : rail.toLowerCase() === 'fednow' ? 'instant'
  : rail.toLowerCase() === 'ach' ? 'ach'
  : rail.toLowerCase() === 'wire' ? 'wire'
  : 'other';

const formatCount = (value: number) => new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
const isFallback = (payment: PaymentActivity) =>
  payment.ruleHits.some((hit) => /fallback|ach/i.test(hit))
  || `${payment.memo ?? ''} ${payment.initiation ?? ''}`.toLowerCase().includes('fallback');

interface JackHenryAccount {
  id: string;
  name: string;
  maskedAccountNumber: string;
  accountType: string;
  currency: string;
  balance: number;
  status: string;
}

interface PolicyRecord {
  id: string;
  accountId: string;
  name: string;
  status: string | number;
  version: number;
  effectiveFrom: string;
  updatedAt: string;
}

const CustomerWorkspace: React.FC<{ accountId: string }> = ({ accountId }) => {
  const { data: accounts = [] } = useAccounts();
  const selectedAccount = accountId
    ? accounts.find((account) => account.id === accountId)
    : accounts[0];
  const selectedId = selectedAccount?.id;
  const updateMode = useUpdateAccountMode(selectedId ?? '');
  const [ruleName, setRuleName] = useState('');

  const balances = useQuery({
    queryKey: ['jack-henry', 'accounts'],
    queryFn: async () => {
      const { data } = await apiClient.get<JackHenryAccount[]>('/jack-henry/accounts');
      return data;
    },
  });

  const policies = useQuery({
    queryKey: ['customer', 'policies', selectedId],
    queryFn: async () => {
      const { data } = await apiClient.get<PolicyRecord[]>(`/policies/account/${selectedId}`);
      return data;
    },
    enabled: !!selectedId,
  });

  const createPolicy = useMutation({
    mutationFn: async () => {
      const existingDraft = (policies.data ?? []).find((policy) =>
        typeof policy.status === 'number' ? policy.status === 0 : policy.status.toLowerCase() === 'draft');
      let policyId = existingDraft?.id;
      if (!policyId) {
        const created = await apiClient.post<PolicyRecord>('/policies', {
          accountId: selectedId,
          name: 'Customer payment rules',
        });
        policyId = created.data.id;
      }
      return apiClient.post(`/policies/${policyId}/rules`, {
        ruleType: 'CustomRule',
        scopeType: 'Account',
        scopeRef: selectedId,
        parameters: { description: ruleName.trim() },
        action: 'Allow',
        priority: (policies.data?.length ?? 0) + 1,
      });
    },
    onSuccess: () => {
      setRuleName('');
      void policies.refetch();
    },
  });

  if (!selectedAccount) {
    return <p className="text-sm text-slate-500">Select a customer account to view balances and rules.</p>;
  }

  return (
    <section className="grid gap-4 xl:grid-cols-[1fr_1fr]">
      <article className="card p-5">
        <div className="flex items-start justify-between gap-3">
          <div><p className="text-xs font-bold uppercase tracking-wide text-slate-400">Account</p><h2 className="mt-1 text-lg font-bold">{selectedAccount.displayName}</h2></div>
          <select
            className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-bold dark:border-slate-700 dark:bg-slate-900"
            value={typeof selectedAccount.mode === 'number' ? (selectedAccount.mode === 1 ? 'Enforce' : 'Monitor') : selectedAccount.mode}
            onChange={(event) => updateMode.mutate(event.target.value)}
            aria-label="Account mode"
          >
            <option>Monitor</option>
            <option>Enforce</option>
          </select>
        </div>
        <div className="mt-5 grid gap-3 sm:grid-cols-3">
          {(balances.data ?? []).map((balance) => (
            <div key={balance.id} className="rounded-xl border border-slate-200 p-4 dark:border-slate-700">
              <p className="text-xs font-semibold text-slate-500">{balance.name}</p>
              <p className="mt-2 text-2xl font-bold tabular-nums">{new Intl.NumberFormat('en-US', { style: 'currency', currency: balance.currency }).format(balance.balance)}</p>
              <p className="mt-1 text-xs text-slate-400">{balance.maskedAccountNumber} · {balance.status}</p>
            </div>
          ))}
          {!balances.isLoading && (balances.data ?? []).length === 0 && <p className="text-sm text-slate-500">Balances unavailable.</p>}
        </div>
      </article>

      <article className="card p-5">
        <h2 className="text-lg font-bold">Rules</h2>
        <div className="mt-4 space-y-2">
          {(policies.data ?? []).map((policy) => (
            <div key={policy.id} className="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-3 dark:border-slate-700">
              <div><p className="text-sm font-semibold">{policy.name}</p><p className="text-xs text-slate-500">Version {policy.version}</p></div>
              <span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                {typeof policy.status === 'number' ? ['Draft', 'Active', 'Archived'][policy.status] ?? 'Unknown' : policy.status}
              </span>
            </div>
          ))}
          {!policies.isLoading && (policies.data ?? []).length === 0 && <p className="text-sm text-slate-500">No rules configured.</p>}
        </div>
        <form
          className="mt-4 flex gap-2"
          onSubmit={(event) => {
            event.preventDefault();
            if (ruleName.trim()) createPolicy.mutate();
          }}
        >
          <input
            className="h-10 min-w-0 flex-1 rounded-lg border border-slate-200 px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            value={ruleName}
            onChange={(event) => setRuleName(event.target.value)}
            placeholder="New rule name"
            aria-label="New rule name"
          />
          <button className="h-10 rounded-lg bg-blue-600 px-4 text-xs font-bold text-white disabled:opacity-50" disabled={!ruleName.trim() || createPolicy.isPending} type="submit">
            Add rule
          </button>
        </form>
      </article>
    </section>
  );
};

const statusMatches = (payment: PaymentActivity, status: string) => {
  if (status === 'all') return true;
  const outcome = payment.outcome.toLowerCase();
  if (status === 'completed') return outcome === 'allowed' || payment.matchStatus === 'Matched';
  if (status === 'failed') return outcome === 'rejected';
  return true;
};

export const DashboardPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const accountId = searchParams.get('account') ?? '';
  const isCustomerView = searchParams.get('view') === 'customer';
  const { data: accounts = [] } = useAccounts();
  const { data, isLoading, isError, error } = useDashboardActivity(
    accountId || undefined,
    accounts.map((account) => account.id),
  );

  const [dateRange, setDateRange] = useState('30');
  const [comparison, setComparison] = useState('Prior 30 Days');
  const [rail, setRail] = useState('all');
  const [status, setStatus] = useState('all');
  const [heatmapView, setHeatmapView] = useState<'hour' | 'amount' | 'type'>('hour');

  const setCustomer = (id: string) => {
    const next = new URLSearchParams(searchParams);
    if (id) next.set('account', id);
    else next.delete('account');
    setSearchParams(next);
  };

  useEffect(() => {
    if (isCustomerView && !accountId && accounts.length) {
      setCustomer(accounts[0].id);
    }
  }, [isCustomerView, accountId, accounts]);

  const filtered = useMemo(() => {
    if (!data) return [];
    const payments = data.payments.filter((payment) =>
      statusMatches(payment, status)
      && (rail === 'all' || railCategory(payment.rail) === rail)
      );

    if (dateRange === 'today') {
      const now = new Date();
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      return payments.filter((payment) => new Date(payment.eventTime) >= start);
    }
    if (dateRange === 'yesterday') {
      const now = new Date();
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1);
      const end = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      return payments.filter((payment) => {
        const eventTime = new Date(payment.eventTime);
        return eventTime >= start && eventTime < end;
      });
    }
    if (dateRange === 'all') return payments;
    const dated = payments.map((payment) => new Date(payment.eventTime).getTime()).filter(Number.isFinite);
    if (!dated.length) return payments;
    const anchor = Math.max(...dated);
    const start = anchor - Number(dateRange) * 24 * 60 * 60 * 1000;
    return payments.filter((payment) => new Date(payment.eventTime).getTime() >= start);
  }, [data, dateRange, rail, status]);

  const countByRail = (category: string) => filtered.filter((payment) => railCategory(payment.rail) === category).length;
  const fallbackCount = filtered.filter(isFallback).length;
  const totalCount = filtered.length;
  const instantCount = countByRail('instant');
  const achCount = countByRail('ach');
  const wireCount = countByRail('wire');
  const achEligibleCount = filtered.filter((payment) =>
    railCategory(payment.rail) === 'ach' && payment.achInstantEligible).length;
  const achNotEligibleCount = achCount - achEligibleCount;
  const achPotentialRate = achCount ? Math.round(achEligibleCount / achCount * 100) : 0;
  const sentAmount = filtered
    .filter((payment) => payment.direction.toLowerCase() === 'outbound')
    .reduce((sum, payment) => sum + payment.amount, 0);
  const receivedAmount = filtered
    .filter((payment) => payment.direction.toLowerCase() === 'inbound')
    .reduce((sum, payment) => sum + payment.amount, 0);
  const runningBalance = 1_250_420.18 - sentAmount + receivedAmount;
  const formatMoney = (amount: number) => new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 2,
  }).format(amount);

  const amountTrend = useMemo(() => {
    const buckets = new Map<string, { sent: number; received: number }>();
    filtered.forEach((payment) => {
      const date = new Date(payment.eventTime);
      if (Number.isNaN(date.getTime())) return;
      const key = date.toISOString().slice(0, 10);
      const bucket = buckets.get(key) ?? { sent: 0, received: 0 };
      if (payment.direction.toLowerCase() === 'outbound') bucket.sent += payment.amount;
      else bucket.received += payment.amount;
      buckets.set(key, bucket);
    });
    return [...buckets.entries()]
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([date, values]) => ({
        date: new Date(`${date}T12:00:00`).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
        Sent: values.sent,
        Received: values.received,
        Net: values.sent - values.received,
      }));
  }, [filtered]);

  const customerRows = useMemo(() => {
    const rows = new Map<string, { instant: number; ach: number; wire: number; fallbacks: number }>();
    filtered.forEach((payment) => {
      const name = payment.accountName.includes(' — ')
        ? payment.accountName.split(' — ')[0]
        : payment.accountName || 'Unknown customer';
      const row = rows.get(name) ?? { instant: 0, ach: 0, wire: 0, fallbacks: 0 };
      const category = railCategory(payment.rail);
      if (category === 'instant' || category === 'ach' || category === 'wire') row[category] += 1;
      if (isFallback(payment)) row.fallbacks += 1;
      rows.set(name, row);
    });
    return [...rows.entries()].sort((left, right) => right[1].instant + right[1].ach + right[1].wire - (left[1].instant + left[1].ach + left[1].wire)).slice(0, 10);
  }, [filtered]);

  if (isLoading) return <LoadingSpinner />;
  if (isError || !data) {
    return (
      <div className="card p-6" role="alert">
        <h1 className="text-lg font-semibold text-red-700">Unable to load payment activity</h1>
        <p className="mt-2 text-sm text-slate-600">{error instanceof Error ? error.message : 'The dashboard sources are unavailable.'}</p>
      </div>
    );
  }

  const trendText = comparison === 'No comparison' ? 'No comparison' : comparison;

  const kpis = [
    { label: 'Instant Payments', value: formatCount(instantCount), amount: filtered.filter((payment) => railCategory(payment.rail) === 'instant').reduce((sum, payment) => sum + payment.amount, 0), tone: 'bg-blue-600' },
    { label: 'ACH Payments', value: formatCount(achCount), amount: filtered.filter((payment) => railCategory(payment.rail) === 'ach').reduce((sum, payment) => sum + payment.amount, 0), tone: 'bg-emerald-600' },
    { label: 'Wire Payments', value: formatCount(wireCount), amount: filtered.filter((payment) => railCategory(payment.rail) === 'wire').reduce((sum, payment) => sum + payment.amount, 0), tone: 'bg-orange-400' },
    { label: 'Instant → ACH Fallbacks', value: formatCount(fallbackCount), amount: filtered.filter(isFallback).reduce((sum, payment) => sum + payment.amount, 0), tone: 'bg-violet-500' },
  ];

  return (
    <div className="min-w-0 space-y-5">
      <header className="flex flex-col justify-between gap-4 xl:flex-row xl:items-start">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-950 dark:text-white">{isCustomerView ? 'Customer Portfolio Overview' : 'Bank Portfolio Overview'}</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{isCustomerView ? 'Payment activity for the selected business customer' : 'Payment activity across all business customers'}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <select className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold dark:border-slate-700 dark:bg-slate-900" value={dateRange} onChange={(event) => setDateRange(event.target.value)} aria-label="Date range">
            <option value="today">Today</option>
            <option value="yesterday">Yesterday</option>
            <option value="7">Last 7 days</option>
            <option value="30">Last 30 days</option>
            <option value="90">Last 90 days</option>
            <option value="all">All available</option>
          </select>
          <select className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold dark:border-slate-700 dark:bg-slate-900" value={comparison} onChange={(event) => setComparison(event.target.value)} aria-label="Comparison period">
            <option>Prior 30 Days</option>
            <option>Prior 90 Days</option>
            <option>No comparison</option>
          </select>
          <button className="h-10 rounded-lg border border-blue-600 px-3 text-xs font-semibold text-blue-600" type="button" onClick={() => window.print()}><Download size={14} className="mr-2 inline" />Export</button>
        </div>
      </header>

      <section className="flex flex-wrap items-center gap-2" aria-label="Dashboard filters">
        {isCustomerView ? (
          <div className="flex h-10 items-center rounded-lg border border-slate-200 bg-slate-50 px-3 text-xs font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
            {accounts.find((account) => account.id === accountId)?.displayName ?? 'Selected customer'}
          </div>
        ) : (
          <select className="h-10 min-w-48 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold dark:border-slate-700 dark:bg-slate-900" value={accountId} onChange={(event) => setCustomer(event.target.value)} aria-label="Business customer">
            <option value="">All business customers</option>
            {accounts.map((account) => <option key={account.id} value={account.id}>{account.displayName}</option>)}
          </select>
        )}
        <select className="h-10 min-w-40 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold dark:border-slate-700 dark:bg-slate-900" value={rail} onChange={(event) => setRail(event.target.value)} aria-label="Payment type">
          <option value="all">All payment types</option>
          <option value="instant">Instant</option>
          <option value="ach">ACH</option>
          <option value="wire">Wire</option>
        </select>
        <select className="h-10 min-w-40 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold dark:border-slate-700 dark:bg-slate-900" value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Payment status">
          <option value="all">All statuses</option>
          <option value="completed">Completed</option>
          <option value="failed">Returned / Failed</option>
        </select>
      </section>

      <section className="grid gap-3 xl:grid-cols-[1.55fr_0.45fr]" aria-label="Intraday activity">
        <article className="card p-5 !border-slate-800 !bg-[#111f31] text-slate-100" aria-labelledby="heatmap-title">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
            <div><h2 id="heatmap-title" className="text-lg font-bold">Heatmap</h2><p className="mt-1 text-xs text-slate-500">Payment activity · Eastern Time</p></div>
            <div className="heatmap-tabs" role="group" aria-label="Heatmap view">
              <button type="button" className={heatmapView === 'hour' ? 'tab-active' : ''} aria-pressed={heatmapView === 'hour'} onClick={() => setHeatmapView('hour')}>By hour</button>
              <button type="button" className={heatmapView === 'amount' ? 'tab-active' : ''} aria-pressed={heatmapView === 'amount'} onClick={() => setHeatmapView('amount')}>By amount</button>
              <button type="button" className={heatmapView === 'type' ? 'tab-active' : ''} aria-pressed={heatmapView === 'type'} onClick={() => setHeatmapView('type')}>By type</button>
            </div>
          </div>
          <div className="heatmap-scroll">
            <div className="min-w-[520px]">
              <div className="heatmap-axis">
                <span />
                <div className="heatmap-grid" style={{ gridTemplateColumns: heatmapView === 'type' ? 'repeat(3, 1fr)' : 'repeat(8, 1fr)' }}>
                  {(heatmapView === 'hour'
                    ? ['Midnight','3A','6A','9A','12P','3P','6P','9P']
                    : heatmapView === 'amount'
                      ? ['.5','1','5','10','25','50','100','250+']
                      : ['Instant','ACH','Wire']
                  ).map((label) => <span className="heatmap-name" key={label}>{label}</span>)}
                </div>
              </div>
              {(heatmapView === 'type'
                ? ([
                    ['Received', 'inbound'],
                    ['Sent', 'outbound'],
                  ] as const)
                : ([
                    ['Received', 'inbound'],
                    ['Sent', 'outbound'],
                  ] as const)
              ).map(([row]) => {
                const binCount = heatmapView === 'type' ? 3 : 8;
                const counts = Array.from({ length: binCount }, (_, index) => filtered.filter((payment) => {
                  if (row === 'Sent' ? payment.direction.toLowerCase() !== 'outbound' : payment.direction.toLowerCase() !== 'inbound') return false;
                  if (heatmapView === 'type') {
                    const category = railCategory(payment.rail);
                    return ['instant', 'ach', 'wire'][index] === category;
                  }
                  return heatmapView === 'hour'
                    ? new Date(payment.eventTime).getHours() >= index * 3 && new Date(payment.eventTime).getHours() < index * 3 + 3
                    : payment.amount >= [0, 500, 1000, 5000, 10000, 25000, 50000, 100000][index]
                      && (index === 7 || payment.amount < [500, 1000, 5000, 10000, 25000, 50000, 100000, 250000][index + 1]);
                }).length);
                const max = Math.max(...counts, 1);
                const rowTotal = counts.reduce((sum, count) => sum + count, 0);
                const gradient = row === 'Received'
                  ? 'linear-gradient(90deg,rgb(18,130,80),rgb(30,175,105),rgb(75,210,145))'
                  : 'linear-gradient(90deg,rgb(125,25,25),rgb(180,35,35),rgb(220,65,65))';
                return (
                  <div key={row} className="mb-3">
                    <div className="heatmap-direction-row">
                      <strong>{row}</strong>
                      <div className="heatmap-activity-bar" style={{
                        background: `linear-gradient(90deg, ${counts.map((count, index) => {
                          const intensity = count / max;
                          const color = row === 'Received'
                            ? `rgb(${[18,130,80].map((channel, part) => Math.round(channel + ([75,210,145][part] - channel) * intensity)).join(',')})`
                            : `rgb(${[125,25,25].map((channel, part) => Math.round(channel + ([220,65,65][part] - channel) * intensity)).join(',')})`;
                          const segment = heatmapView === 'type' ? (index + .5) * (100 / 3) : (index + .5) * 12.5;
                          return `${color} ${segment}%`;
                        }).join(', ')})`,
                      }} />
                    </div>
                    <div className="heatmap-legend heatmap-direction-legend"><span>0</span><span className="heatmap-gradient" style={{ background: gradient }} /><span>{max}</span><span className="heatmap-legend-caption">{rowTotal} payments</span></div>
                  </div>
                );
              })}
            </div>
          </div>
        </article>
        <aside className="card p-5">
          <p className="eyebrow">Current Balance</p>
          <p className="mt-3 text-3xl font-bold tabular-nums">{formatMoney(runningBalance)}</p>
          <p className="mt-1 text-sm text-slate-500">Available for selected date range</p>
          <div className="mt-5 space-y-3 border-t border-slate-200 pt-4 text-sm dark:border-slate-700">
            <div className="flex justify-between"><span className="text-slate-500">Sent</span><strong>{formatMoney(sentAmount)}</strong></div>
            <div className="flex justify-between"><span className="text-slate-500">Received</span><strong>{formatMoney(receivedAmount)}</strong></div>
          </div>
        </aside>
      </section>

      <section className="card p-5" aria-label="Sent received and net amounts">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <div><p className="eyebrow">Amount trend</p><h2 className="mt-1 text-lg font-bold">Sent, Received & Net</h2></div>
          <div className="flex gap-4 text-xs font-semibold">
            <span className="text-red-700"><i className="mr-1 inline-block h-2.5 w-2.5 rounded-full bg-red-700" />Sent</span>
            <span className="text-emerald-700"><i className="mr-1 inline-block h-2.5 w-2.5 rounded-full bg-emerald-600" />Received</span>
            <span className="text-blue-700"><i className="mr-1 inline-block h-2.5 w-2.5 rounded-full bg-blue-700" />Net</span>
          </div>
        </div>
        {amountTrend.length ? (
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={amountTrend} margin={{ top: 8, right: 12, bottom: 0, left: 8 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#dce4ec" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis
                  domain={[0, (dataMax: number) => Math.max(100000, dataMax)]}
                  allowDataOverflow={false}
                  tick={{ fontSize: 12 }}
                  tickFormatter={(value) => new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: 'USD',
                    notation: 'compact',
                    maximumFractionDigits: 0,
                  }).format(Number(value))}
                />
                <Tooltip
                  formatter={(value: number, name: string) => [formatMoney(Number(value)), name]}
                  labelStyle={{ fontWeight: 700 }}
                />
                <ReferenceLine
                  y={100000}
                  stroke="#000000"
                  strokeWidth={4}
                  label={{ value: 'Capacity · $100K', position: 'top', fill: '#000000', fontSize: 12, fontWeight: 800 }}
                />
                <Line type="monotone" dataKey="Sent" stroke="#b91c1c" strokeWidth={3} dot={{ r: 3 }} />
                <Line type="monotone" dataKey="Received" stroke="#047857" strokeWidth={3} dot={{ r: 3 }} />
                <Line type="monotone" dataKey="Net" stroke="#1d4ed8" strokeWidth={3} dot={{ r: 3 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        ) : <p className="flex h-64 items-center justify-center text-sm text-slate-500">No amount activity matches these filters.</p>}
      </section>

      <section className="card p-5" aria-label="Payment summary">
        <div className="mb-4 flex items-end justify-between">
          <div><p className="text-xs font-bold uppercase tracking-wide text-slate-400">Total Payments</p><p className="mt-1 text-3xl font-bold tabular-nums text-slate-950 dark:text-white">{formatCount(totalCount)}</p></div>
          <p className="text-xs font-semibold text-slate-400">{trendText}</p>
        </div>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {kpis.map((kpi) => {
            const percent = totalCount ? Math.round(Number(kpi.value.replace(/,/g, '')) / totalCount * 100) : 0;
            return (
              <article key={kpi.label} className="rounded-xl border border-slate-200 p-4 dark:border-slate-700">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-bold text-slate-700 dark:text-slate-200">{kpi.label}</p>
                  <p className="text-sm font-bold tabular-nums">{percent}%</p>
                </div>
                <p className="mt-2 text-xl font-bold tabular-nums">{new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(kpi.amount)}</p>
                <div className="mt-3 h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                  <div className={`h-full rounded-full ${kpi.tone}`} style={{ width: `${percent}%` }} />
                </div>
              </article>
            );
          })}
        </div>
      </section>

      {isCustomerView && <CustomerWorkspace accountId={accountId} />}

      <section className="card p-5">
        <h2 className="text-lg font-bold">ACH-to-Instant Opportunity</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-3">
          <div><p className="text-3xl font-bold">{formatCount(achEligibleCount)}</p><p className="mt-1 text-sm text-slate-500">ACH Payments Eligible for Instant</p></div>
          <div><p className="text-3xl font-bold">{achPotentialRate}%</p><p className="mt-1 text-sm text-slate-500">Potential Instant conversion rate</p></div>
          <div><p className="text-3xl font-bold">{formatCount(achNotEligibleCount)}</p><p className="mt-1 text-sm text-slate-500">ACH not eligible for Instant</p></div>
        </div>
      </section>

      <section className="grid gap-3 xl:grid-cols-[0.9fr_1.15fr]">
        <article className="card overflow-hidden p-5">
          <h2 className="mb-4 text-lg font-bold">Business Customer Activity</h2>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead><tr className="bg-slate-100 dark:bg-slate-800"><th>Business Customer</th><th>Instant</th><th>ACH</th><th>Wire</th><th>Fallbacks</th></tr></thead>
              <tbody>
                {customerRows.map(([name, row]) => (
                  <tr key={name}><td>{name}</td><td>{formatCount(row.instant)}</td><td>{formatCount(row.ach)}</td><td>{formatCount(row.wire)}</td><td>{formatCount(row.fallbacks)}</td></tr>
                ))}
                {!customerRows.length && <tr><td colSpan={5} className="py-6 text-center text-slate-500">No customer activity matches these filters.</td></tr>}
              </tbody>
            </table>
          </div>
          <Link className="mt-4 inline-block text-sm font-bold text-blue-600" to="/accounts">View all customers →</Link>
        </article>
      </section>
    </div>
  );
};
