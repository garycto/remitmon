import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Banner } from './components/Banner';
import { Navbar } from './components/Navbar';
import { DashboardPage } from './pages/DashboardPage';
import { AccountsPage } from './pages/AccountsPage';
import './index.css';

const queryClient = new QueryClient();

function App() {
  const [isDark, setIsDark] = useState(localStorage.getItem('theme') === 'dark');

  React.useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }, [isDark]);

  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <div className={isDark ? 'dark' : ''}>
          <div className="min-h-screen bg-white dark:bg-gray-950 text-gray-900 dark:text-gray-50">
            <Banner isDevelopment={true} message="" />
            <Navbar isDark={isDark} onToggleDark={() => setIsDark(!isDark)} />
            
            <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/accounts" element={<AccountsPage />} />
                <Route path="*" element={<div className="text-center py-12">Page not found</div>} />
              </Routes>
            </main>
          </div>
        </div>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
