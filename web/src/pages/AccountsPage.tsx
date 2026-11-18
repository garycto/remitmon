import React from 'react';
import { useAccounts } from '../hooks/useAccounts';
import { Card } from './Card';
import { LoadingSpinner } from './LoadingSpinner';

export const AccountsPage: React.FC = () => {
  const { data: accounts, isLoading, error } = useAccounts();

  if (isLoading) return <LoadingSpinner />;
  if (error) return <div className="text-red-600 dark:text-red-400">Error loading accounts</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Accounts</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Manage protected bank accounts</p>
      </div>

      <div className="grid gap-4">
        {accounts?.map((account) => (
          <Card key={account.id} className="hover:shadow-lg transition-shadow cursor-pointer">
            <div className="flex justify-between items-start">
              <div>
                <h3 className="text-lg font-semibold">{account.displayName}</h3>
                <p className="text-gray-600 dark:text-gray-400 text-sm">{account.maskedAccountNumber}</p>
              </div>
              <div className="space-y-1 text-right">
                <span className={`inline-block px-2 py-1 rounded text-xs font-semibold ${
                  account.status === 'Active' 
                    ? 'badge-green' 
                    : 'badge-red'
                }`}>
                  {account.status}
                </span>
                <span className={`inline-block px-2 py-1 rounded text-xs font-semibold ml-2 ${
                  account.mode === 'Enforce' 
                    ? 'badge-red' 
                    : 'badge-blue'
                }`}>
                  {account.mode}
                </span>
              </div>
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-4">
              Created {new Date(account.createdAt).toLocaleDateString()}
            </p>
          </Card>
        ))}
      </div>
    </div>
  );
};
