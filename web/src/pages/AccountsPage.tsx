import React from 'react';
import { accountModeLabel, accountStatusLabel, useAccounts } from '../hooks/useAccounts';
import { Card } from '../components/Card';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { useSearchParams } from 'react-router-dom';
import { useDashboardActivity } from '../hooks/useDashboardActivity';

export const AccountsPage: React.FC = () => {
  const { data: accounts, isLoading, error } = useAccounts();
  const [searchParams] = useSearchParams();
  const accountId = searchParams.get('account') ?? undefined;
  const { data: activity } = useDashboardActivity(accountId);

  if (isLoading) return <LoadingSpinner />;
  if (error) return <div className="text-red-600 dark:text-red-400">Error loading accounts</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Accounts</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Manage protected bank accounts</p>
      </div>

      <section className="grid gap-4 xl:grid-cols-[1fr_280px]">
        <div className="panel p-5">
          <div className="mb-4 flex items-center justify-between">
            <div><p className="eyebrow">Intraday activity</p><h2 className="mt-1 font-semibold">Sent & Received by Hour</h2></div>
            <div className="flex gap-3 text-xs font-semibold"><span className="text-blue-600">Sent</span><span className="text-emerald-600">Received</span></div>
          </div>
          <div className="grid grid-cols-[38px_repeat(12,minmax(18px,1fr))] gap-1 text-xs">
            <span />
            {Array.from({ length: 12 }, (_, index) => <span key={index} className="text-center text-slate-400">{index * 2}</span>)}
            {(['Sent', 'Received'] as const).map((direction) => (
              <React.Fragment key={direction}>
                <span className="py-1 font-semibold text-slate-500">{direction.slice(0, 3)}</span>
                {Array.from({ length: 12 }, (_, index) => {
                  const hour = index * 2;
                  const count = (activity?.payments ?? []).filter((payment) =>
                    new Date(payment.eventTime).getHours() >= hour
                    && new Date(payment.eventTime).getHours() < hour + 2
                    && (direction === 'Sent'
                      ? payment.direction.toLowerCase() === 'outbound'
                      : payment.direction.toLowerCase() === 'inbound')).length;
                  const intensity = count === 0 ? 0 : Math.min(1, count / 4);
                  return (
                    <span
                      key={hour}
                      className="h-9 rounded"
                      title={`${direction}: ${count} payments at ${hour}:00`}
                      style={{ backgroundColor: direction === 'Sent' ? `rgba(59,130,246,${0.12 + intensity * 0.88})` : `rgba(16,165,106,${0.12 + intensity * 0.88})` }}
                    />
                  );
                })}
              </React.Fragment>
            ))}
          </div>
        </div>
        <aside className="panel p-5">
          <p className="eyebrow">Running balance</p>
          <p className="mt-3 text-3xl font-bold tabular-nums">$1,250,420.18</p>
          <p className="mt-1 text-sm text-slate-500">Available across selected accounts</p>
          <div className="mt-5 space-y-3 border-t border-slate-200 pt-4 dark:border-slate-700">
            <div className="flex justify-between text-sm"><span className="text-slate-500">Today sent</span><strong>$86,240</strong></div>
            <div className="flex justify-between text-sm"><span className="text-slate-500">Today received</span><strong>$124,880</strong></div>
            <div className="flex justify-between text-sm"><span className="text-slate-500">Pending</span><strong>$38,510</strong></div>
          </div>
        </aside>
      </section>

      <div className="grid gap-4">
        {accounts?.map((account) => {
          const status = accountStatusLabel(account.status);
          const mode = accountModeLabel(account.mode);
          return (
          <Card key={account.id} className="hover:shadow-lg transition-shadow cursor-pointer">
            <div className="flex justify-between items-start">
              <div>
                <h3 className="text-lg font-semibold">{account.displayName}</h3>
                <p className="text-gray-600 dark:text-gray-400 text-sm">{account.maskedAccountNumber}</p>
              </div>
              <div className="space-y-1 text-right">
                <span className={`inline-block px-2 py-1 rounded text-xs font-semibold ${
                  status === 'Active'
                    ? 'badge-green' 
                    : 'badge-red'
                }`}>
                  {status}
                </span>
                <span className={`inline-block px-2 py-1 rounded text-xs font-semibold ml-2 ${
                  mode === 'Enforce'
                    ? 'badge-red' 
                    : 'badge-blue'
                }`}>
                  {mode}
                </span>
              </div>
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-4">
              Created {new Date(account.createdAt).toLocaleDateString()}
            </p>
          </Card>
          );
        })}
      </div>
    </div>
  );
};
