import React from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

export const DashboardPage: React.FC = () => {
  const data = [
    { name: 'Allowed', value: 142, fill: '#10b981' },
    { name: 'Rejected', value: 23, fill: '#ef4444' },
    { name: 'Escalated', value: 8, fill: '#f59e0b' },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Account protection status and activity overview</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="card p-6">
          <h3 className="text-gray-600 dark:text-gray-400 text-sm font-semibold uppercase">Total Allowed</h3>
          <p className="text-4xl font-bold text-green-600 dark:text-green-400 mt-2">142</p>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">Last 30 days</p>
        </div>

        <div className="card p-6">
          <h3 className="text-gray-600 dark:text-gray-400 text-sm font-semibold uppercase">Total Rejected</h3>
          <p className="text-4xl font-bold text-red-600 dark:text-red-400 mt-2">23</p>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">Last 30 days</p>
        </div>

        <div className="card p-6">
          <h3 className="text-gray-600 dark:text-gray-400 text-sm font-semibold uppercase">Open Exceptions</h3>
          <p className="text-4xl font-bold text-yellow-600 dark:text-yellow-400 mt-2">8</p>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">Requires attention</p>
        </div>
      </div>

      <div className="card p-6">
        <h3 className="text-lg font-semibold mb-4">Decision Outcomes (30 days)</h3>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" />
            <YAxis />
            <Tooltip />
            <Legend />
            <Bar dataKey="value" fill="#8884d8" />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
