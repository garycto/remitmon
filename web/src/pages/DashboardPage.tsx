import React from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { useDashboardActivity } from '../hooks/useDashboardActivity';
import type { ExpectedPayment, PaymentActivity } from '../hooks/useDashboardActivity';

const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount);

const formatDate = (date: string) =>
  new Date(date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });

const formatDateTime = (date: string) =>
  new Date(date).toLocaleString(undefined, { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });

const statusClass = (status: string) => {
  const normalized = status.toLowerCase();
  if (normalized === 'paid' || normalized === 'matched' || normalized === 'allowed') {
    return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300';
  }
  if (normalized === 'rejected') {
    return 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300';
  }
  if (normalized === 'escalated') {
    return 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300';
  }
  return 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
};

const InboundDecisionTable: React.FC<{
  payments: PaymentActivity[];
  obligations: ExpectedPayment[];
}> = ({ payments, obligations }) => (
  <div className="overflow-x-auto">
    <table className="data-table">
      <thead>
        <tr>
          <th>Inbound item</th>
          <th>Account</th>
          <th>Merchant / request</th>
          <th>Matched bill</th>
          <th>Business rule outcome</th>
          <th className="text-right">Amount</th>
        </tr>
      </thead>
      <tbody>
        {payments.map((payment) => {
          const matchedObligation = payment.matchedObligationId
            ? obligations.find((obligation) => obligation.id === payment.matchedObligationId)
            : undefined;
          return (
            <tr key={payment.id}>
              <td>
                <div className="font-semibold">{payment.rail} / {payment.initiation}</div>
                <div className="mt-1 text-xs text-slate-500">{formatDateTime(payment.eventTime)}</div>
              </td>
              <td>
                <Link className="entity-link" to={`/accounts?account=${payment.accountId}`}>
                  {payment.accountName}
                </Link>
              </td>
              <td>
                <Link className="entity-link" to="/counterparties">
                  {payment.counterpartyRef ?? 'Unknown merchant'}
                </Link>
                <div className="mt-1 text-xs text-slate-500">{payment.reference ?? payment.id}</div>
              </td>
              <td>
                {matchedObligation ? (
                  <Link className="entity-link" to={`/obligations/${matchedObligation.id}`}>
                    {matchedObligation.sourceRef}
                  </Link>
                ) : (
                  <span className="text-slate-500">{payment.matchedObligationRef ?? 'No bill match'}</span>
                )}
                <div className="mt-1 text-xs">
                  <span className={`rounded px-2 py-0.5 ${statusClass(payment.matchStatus)}`}>
                    {payment.matchStatus}
                  </span>
                </div>
              </td>
              <td>
                <div className="flex items-center gap-2">
                  <span className={`rounded px-2 py-1 text-xs font-semibold ${statusClass(payment.outcome)}`}>
                    {payment.outcome}
                  </span>
                  {payment.policyId && (
                    <Link className="text-xs text-indigo-600 hover:underline dark:text-indigo-300" to={`/policies/${payment.policyId}`}>
                      {payment.policyName}
                    </Link>
                  )}
                </div>
                <div className="mt-1 max-w-sm space-y-0.5 text-xs text-slate-500">
                  {payment.ruleHits.slice(0, 2).map((hit) => <div key={hit}>{hit}</div>)}
                </div>
              </td>
              <td className="text-right font-semibold tabular-nums">
                {formatCurrency(payment.amount, payment.currency)}
              </td>
            </tr>
          );
        })}
        {payments.length === 0 && (
          <tr><td colSpan={6} className="empty-row">No inbound RfP or ACH activity found.</td></tr>
        )}
      </tbody>
    </table>
  </div>
);

const ExpectedBillsTable: React.FC<{ payments: ExpectedPayment[] }> = ({ payments }) => (
  <div className="overflow-x-auto">
    <table className="data-table">
      <thead>
        <tr>
          <th>Bill / merchant</th>
          <th>Invoice reference</th>
          <th>Due date</th>
          <th>Status</th>
          <th className="text-right">Amount</th>
        </tr>
      </thead>
      <tbody>
        {payments.map((payment) => (
          <tr key={payment.id}>
            <td>
              <Link className="entity-link" to={`/obligations/${payment.id}`}>
                {payment.counterpartyName}
              </Link>
              <div className="mt-1 text-xs text-slate-500">{payment.source}</div>
            </td>
            <td className="font-mono text-xs">{payment.sourceRef}</td>
            <td>{formatDate(payment.dueDate)}</td>
            <td>
              <span className={`rounded px-2 py-1 text-xs font-semibold ${statusClass(payment.status)}`}>
                {payment.status}
              </span>
            </td>
            <td className="text-right font-semibold tabular-nums">
              {formatCurrency(payment.amount, 'USD')}
            </td>
          </tr>
        ))}
        {payments.length === 0 && (
          <tr><td colSpan={5} className="empty-row">No bills or expected obligations found.</td></tr>
        )}
      </tbody>
    </table>
  </div>
);

export const DashboardPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const { data, isLoading, isError, error } = useDashboardActivity(searchParams.get('account') ?? undefined);

  if (isLoading) return <LoadingSpinner />;
  if (isError || !data) {
    return (
      <div className="panel p-6" role="alert">
        <h1 className="text-lg font-semibold text-red-700 dark:text-red-300">Unable to load payment activity</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-300">
          {error instanceof Error ? error.message : 'The dashboard sources are unavailable.'}
        </p>
      </div>
    );
  }

  const inboundQueue = data.payments.filter((payment) =>
    payment.direction === 'Inbound'
    && (payment.initiation === 'RfP' || payment.rail === 'ACH'));
  const openBills = data.expectedPayments.filter((payment) =>
    payment.status === 'Open' || payment.status === 'Matched');
  const rejected = inboundQueue.filter((payment) => payment.outcome === 'Rejected');
  const escalated = inboundQueue.filter((payment) => payment.outcome === 'Escalated');
  const matched = inboundQueue.filter((payment) => payment.matchStatus === 'Matched');

  const summaryCards = [
    { label: 'RfP / ACH activity', count: inboundQueue.length, detail: 'Awaiting or completed decision', tone: 'text-slate-950 dark:text-white' },
    { label: 'Bill matched', count: matched.length, detail: 'Married to an expected bill', tone: 'text-emerald-700 dark:text-emerald-300' },
    { label: 'Rejected', count: rejected.length, detail: 'Stop list or policy reject', tone: 'text-red-700 dark:text-red-300' },
    { label: 'Escalated', count: escalated.length, detail: 'Limit or human review', tone: 'text-amber-700 dark:text-amber-300' },
  ];

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Payment protection</p>
          <h1>Payment activity</h1>
          <p>RfPs and ACH items matched to bills, then evaluated by active account rules.</p>
        </div>
        <Link className="subtle-count hover:border-indigo-300" to="/activity">View full activity</Link>
      </header>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {summaryCards.map((card) => (
          <div key={card.label} className="panel p-5">
            <p className="text-xs font-bold uppercase tracking-wide text-slate-500">{card.label}</p>
            <p className={`mt-2 text-3xl font-bold ${card.tone}`}>{card.count}</p>
            <p className="mt-1 text-xs text-slate-500">{card.detail}</p>
          </div>
        ))}
      </section>

      <section className="panel overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-800">
          <div>
            <p className="eyebrow">Decision queue</p>
            <h2 className="mt-1 font-semibold">RfP and ACH payment activity</h2>
          </div>
          <Link className="text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-300" to="/policies">Rules engine</Link>
        </div>
        <InboundDecisionTable payments={inboundQueue} obligations={data.expectedPayments} />
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.45fr_0.55fr]">
        <div className="panel overflow-hidden">
          <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-800">
            <div>
              <p className="eyebrow">Accounting</p>
              <h2 className="mt-1 font-semibold">Bills and expected payments</h2>
            </div>
            <Link className="text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-300" to="/obligations">Open obligations</Link>
          </div>
          <ExpectedBillsTable payments={openBills} />
        </div>

        <aside className="panel p-5">
          <p className="eyebrow">Rapid links</p>
          <h2 className="mt-1 font-semibold">Investigate</h2>
          <div className="mt-4 space-y-2">
            <Link className="block rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium hover:border-indigo-300 dark:border-slate-800" to="/accounts">
              Protected accounts
            </Link>
            <Link className="block rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium hover:border-indigo-300 dark:border-slate-800" to="/counterparties">
              Merchants and stop lists
            </Link>
            <Link className="block rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium hover:border-indigo-300 dark:border-slate-800" to="/policies">
              Limits and rule policies
            </Link>
            <Link className="block rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium hover:border-indigo-300 dark:border-slate-800" to="/activity">
              Raw payment activity
            </Link>
          </div>
        </aside>
      </section>
    </div>
  );
};
