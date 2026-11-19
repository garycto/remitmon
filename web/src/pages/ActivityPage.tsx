import { useMemo, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { Activity, ArrowDownLeft, ArrowUpRight, Search } from 'lucide-react';
import { useDashboardActivity } from '../hooks/useDashboardActivity';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type { PaymentActivity } from '../hooks/useDashboardActivity';

type ActivityFilter = 'All' | 'Inbound' | 'Outbound' | 'RfP';

const money = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount);

const dateTime = (date: string) =>
  new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));

export const ActivityPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const { eventId } = useParams();
  const { data, isLoading, error } = useDashboardActivity(searchParams.get('account') ?? undefined);
  const [filter, setFilter] = useState<ActivityFilter>('All');
  const [search, setSearch] = useState('');
  const selectedId = eventId ?? searchParams.get('drawer')?.replace(/^event:/, '');

  const payments = useMemo(() => {
    const all = data?.payments ?? [];
    return all.filter((payment) => {
      const matchesFilter = filter === 'All'
        || payment.direction === filter
        || (filter === 'RfP' && payment.initiation === 'RfP');
      const query = search.trim().toLowerCase();
      const matchesSearch = !query || [
        payment.accountName,
        payment.counterpartyRef,
        payment.invoiceRef,
        payment.reference,
        payment.rail,
        payment.initiation,
      ].some((value) => value?.toLowerCase().includes(query));
      return matchesFilter && matchesSearch;
    });
  }, [data?.payments, filter, search]);

  if (isLoading) return <LoadingSpinner />;
  if (error || !data) {
    return <p role="alert" className="text-sm text-red-700 dark:text-red-300">Could not load payment activity.</p>;
  }

  const selected = data.payments.find((payment) => payment.id === selectedId);

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Payments</p>
          <h1>Activity</h1>
          <p>Inbound requests and payment events across protected accounts.</p>
        </div>
        <span className="subtle-count"><Activity size={16} /> {payments.length} events</span>
      </header>

      <section className="panel">
        <div className="flex flex-col gap-3 border-b border-slate-200 p-4 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-wrap gap-2" aria-label="Activity filters">
            {(['All', 'Inbound', 'Outbound', 'RfP'] as const).map((value) => (
              <button
                key={value}
                type="button"
                className={`filter-chip ${filter === value ? 'filter-chip-active' : ''}`}
                aria-pressed={filter === value}
                onClick={() => setFilter(value)}
              >
                {value}
              </button>
            ))}
          </div>
          <label className="search-field sm:w-72">
            <Search size={16} aria-hidden="true" />
            <span className="sr-only">Search activity</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Reference or counterparty" />
          </label>
        </div>
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead>
              <tr><th>Time</th><th>Rail / initiation</th><th>Counterparty</th><th>Reference</th><th>Direction</th><th className="text-right">Amount</th></tr>
            </thead>
            <tbody>
              {payments.map((payment) => (
                <tr
                  key={payment.id}
                  className={selectedId === payment.id ? 'row-selected' : ''}
                >
                  <td>
                    <Link
                      className="entity-link"
                      to={`?${withDrawer(searchParams, payment.id)}`}
                      aria-label={`Open payment event from ${dateTime(payment.eventTime)}`}
                    >
                      {dateTime(payment.eventTime)}
                    </Link>
                  </td>
                  <td><span className={`status-pill ${payment.initiation === 'Pull' ? 'status-warning' : 'status-neutral'}`}>{payment.rail} · {payment.initiation}</span></td>
                  <td>{payment.counterpartyRef ?? 'Unknown counterparty'}</td>
                  <td>{payment.invoiceRef ?? payment.reference ?? '—'}</td>
                  <td><span className="inline-flex items-center gap-1">{payment.direction === 'Inbound' ? <ArrowDownLeft size={14} /> : <ArrowUpRight size={14} />}{payment.direction}</span></td>
                  <td className="text-right font-semibold tabular-nums">{money(payment.amount, payment.currency)}</td>
                </tr>
              ))}
              {payments.length === 0 && <tr><td colSpan={6} className="empty-row">No activity matches these filters.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      {selected && (
        <div className="drawer-backdrop" role="presentation" onClick={() => setSearchParams((current) => {
          const next = new URLSearchParams(current);
          next.delete('drawer');
          return next;
        })}>
          <aside className="decision-drawer" role="dialog" aria-modal="true" aria-labelledby="event-drawer-title" onClick={(event) => event.stopPropagation()}>
            <div className="flex items-start justify-between gap-4">
              <div><p className="eyebrow">Payment event</p><h2 id="event-drawer-title">Event details</h2></div>
              <button className="icon-button" aria-label="Close event details" onClick={() => setSearchParams((current) => {
                const next = new URLSearchParams(current);
                next.delete('drawer');
                return next;
              })}>×</button>
            </div>
            <EventDetails payment={selected} />
            <p className="mt-5 rounded-lg bg-amber-50 p-3 text-sm text-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
              Decision reasons and raw event payload are not currently exposed by the API.
            </p>
          </aside>
        </div>
      )}
    </div>
  );
};

const withDrawer = (searchParams: URLSearchParams, eventId: string) => {
  const next = new URLSearchParams(searchParams);
  next.set('drawer', `event:${eventId}`);
  return next.toString();
};

const EventDetails = ({ payment }: { payment: PaymentActivity }) => (
  <dl className="detail-list mt-6">
    <div><dt>Amount</dt><dd>{money(payment.amount, payment.currency)}</dd></div>
    <div><dt>Counterparty</dt><dd>{payment.counterpartyRef ?? 'Unknown'}</dd></div>
    <div><dt>Account</dt><dd>{payment.accountName}</dd></div>
    <div><dt>Rail</dt><dd>{payment.rail} · {payment.initiation}</dd></div>
    <div><dt>Direction</dt><dd>{payment.direction}</dd></div>
    <div><dt>Reference</dt><dd>{payment.invoiceRef ?? payment.reference ?? '—'}</dd></div>
    <div><dt>Memo</dt><dd>{payment.memo ?? '—'}</dd></div>
    <div><dt>Event time</dt><dd>{dateTime(payment.eventTime)}</dd></div>
    <div><dt>Settlement</dt><dd>{payment.settlementDate ? dateTime(payment.settlementDate) : 'Pending'}</dd></div>
  </dl>
);
