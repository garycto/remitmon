import React from 'react';
import { AlertCircle, CheckCircle, Clock } from 'lucide-react';

interface BannerProps {
  type?: 'info' | 'warning' | 'error' | 'success';
  message: string;
}

export const Banner: React.FC<BannerProps> = ({ type = 'info', message }) => {
  const bgColor = {
    info: 'bg-blue-100 dark:bg-blue-900',
    warning: 'bg-yellow-100 dark:bg-yellow-900',
    error: 'bg-red-100 dark:bg-red-900',
    success: 'bg-green-100 dark:bg-green-900',
  }[type];

  const icon = {
    info: <Clock className="w-4 h-4" />,
    warning: <AlertCircle className="w-4 h-4" />,
    error: <AlertCircle className="w-4 h-4" />,
    success: <CheckCircle className="w-4 h-4" />,
  }[type];

  return (
    <div className={`${bgColor} border-b-2 py-2 px-4`}>
      <div className="flex items-center gap-2 text-sm font-semibold">
        {icon}
        {message}
      </div>
    </div>
  );
};
