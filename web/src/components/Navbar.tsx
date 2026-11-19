import React from 'react';
import { Menu, X, Moon, Sun } from 'lucide-react';

interface NavbarProps {
  isDark: boolean;
  onToggleDark: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ isDark, onToggleDark }) => {
  const [mobileOpen, setMobileOpen] = React.useState(false);

  return (
    <nav className="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex items-center">
            <h1 className="text-2xl font-bold text-blue-600 dark:text-blue-400">RemitMon</h1>
          </div>

          <div className="hidden md:flex items-center gap-8">
            <a href="/" className="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400">
              Dashboard
            </a>
            <a href="/accounts" className="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400">
              Accounts
            </a>
            <a href="/policies" className="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400">
              Policies
            </a>
            <a href="/approvals" className="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400">
              Approvals
            </a>
            <button
              onClick={onToggleDark}
              className="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700"
              aria-label="Toggle dark mode"
            >
              {isDark ? <Sun className="w-5 h-5" /> : <Moon className="w-5 h-5" />}
            </button>
          </div>

          <div className="md:hidden flex items-center gap-4">
            <button
              onClick={onToggleDark}
              className="p-2 rounded-lg bg-gray-100 dark:bg-gray-800"
            >
              {isDark ? <Sun className="w-5 h-5" /> : <Moon className="w-5 h-5" />}
            </button>
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="p-2 rounded-lg bg-gray-100 dark:bg-gray-800"
            >
              {mobileOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>

        {mobileOpen && (
          <div className="md:hidden py-4 border-t border-gray-200 dark:border-gray-700">
            <a href="/" className="block py-2 text-gray-700 dark:text-gray-300 hover:text-blue-600">
              Dashboard
            </a>
            <a href="/accounts" className="block py-2 text-gray-700 dark:text-gray-300 hover:text-blue-600">
              Accounts
            </a>
            <a href="/policies" className="block py-2 text-gray-700 dark:text-gray-300 hover:text-blue-600">
              Policies
            </a>
            <a href="/approvals" className="block py-2 text-gray-700 dark:text-gray-300 hover:text-blue-600">
              Approvals
            </a>
          </div>
        )}
      </div>
    </nav>
  );
};
