import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Activity,
  AlertTriangle,
  Building2,
  ChevronDown,
  Command,
  FileClock,
  FileText,
  LayoutDashboard,
  Landmark,
  Moon,
  Search,
  Settings,
  Sun,
  Users,
  WalletCards,
  X,
} from 'lucide-react';
import { accountModeLabel, useAccounts } from '../hooks/useAccounts';
import { useApprovals } from '../hooks/useApprovals';

export type UserRole = 'BusinessApprover' | 'BusinessAdmin' | 'BankAdmin' | 'PlatformAdmin';
export const activeRole: UserRole = 'BusinessAdmin';

interface NavigationItem {
  label: string;
  path: string;
  icon: typeof LayoutDashboard;
  roles: UserRole[];
}

const navigation: NavigationItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, roles: ['BusinessApprover', 'BusinessAdmin', 'PlatformAdmin'] },
  { label: 'Activity', path: '/activity', icon: Activity, roles: ['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Approvals', path: '/approvals', icon: AlertTriangle, roles: ['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Policies', path: '/policies', icon: FileText, roles: ['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Counterparties', path: '/counterparties', icon: Users, roles: ['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Obligations', path: '/obligations', icon: WalletCards, roles: ['BusinessApprover', 'BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Integrations', path: '/integrations', icon: Settings, roles: ['BusinessAdmin', 'PlatformAdmin'] },
  { label: 'Audit', path: '/audit', icon: FileClock, roles: ['BusinessAdmin', 'BankAdmin', 'PlatformAdmin'] },
  { label: 'Accounts', path: '/accounts', icon: Building2, roles: ['BusinessApprover', 'BusinessAdmin'] },
  { label: 'Bank overview', path: '/bank', icon: Landmark, roles: ['BankAdmin', 'PlatformAdmin'] },
  { label: 'Customers', path: '/bank/customers', icon: Users, roles: ['BankAdmin', 'PlatformAdmin'] },
  { label: 'Baseline policies', path: '/bank/baseline', icon: FileText, roles: ['BankAdmin', 'PlatformAdmin'] },
  { label: 'Simulator', path: '/admin/simulator', icon: Activity, roles: ['PlatformAdmin'] },
  { label: 'Tenants', path: '/admin/tenants', icon: Building2, roles: ['PlatformAdmin'] },
  { label: 'System health', path: '/admin/health', icon: Activity, roles: ['PlatformAdmin'] },
];

const approvalThresholds = { criticalMs: 30 * 60_000, warningMs: 4 * 3_600_000 };

export const AppShell = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  const { data: accounts = [], isLoading: accountsLoading, isError: accountsError } = useAccounts();
  const { data: approvals, isLoading: approvalsLoading, isError: approvalsError } = useApprovals(searchParams.get('account') ?? undefined);
  const [menuOpen, setMenuOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [shortcutHelpOpen, setShortcutHelpOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [isDark, setIsDark] = useState(localStorage.getItem('theme') === 'dark');
  const [now, setNow] = useState(Date.now());
  const pendingGoShortcut = useRef<number | undefined>(undefined);

  // The API's current-user response has no role claim, so the shell uses the prototype's business-admin view.
  const visibleNavigation = useMemo(() => navigation.filter((item) => item.roles.includes(activeRole)), []);
  const accountId = searchParams.get('account') ?? '';
  const activeAccount = accounts.find((account) => account.id === accountId);
  const activeMode = activeAccount ? accountModeLabel(activeAccount.mode) : 'Mixed mode';
  const activeModeClass = activeMode === 'Enforce' ? 'mode-enforce' : activeMode === 'Monitor' ? 'mode-monitor' : 'status-neutral';
  const openApprovals = (approvals ?? []).filter((approval) => approval.status === 'Open');
  const nearestDeadline = openApprovals
    .map((approval) => approval.expiresAt ? new Date(approval.expiresAt).getTime() : Number.POSITIVE_INFINITY)
    .filter(Number.isFinite)
    .sort((left, right) => left - right)[0];

  const commandResults = useMemo(() => {
    const term = query.trim().toLowerCase();
    const pages = visibleNavigation
      .filter((item) => !term || item.label.toLowerCase().includes(term))
      .map((item) => ({ kind: 'page' as const, label: item.label, path: item.path }));
    const accountResults = accounts
      .filter((account) => !term || account.displayName.toLowerCase().includes(term))
      .map((account) => ({ kind: 'account' as const, label: account.displayName, path: `/dashboard?account=${account.id}`, mode: account.mode }));
    return [...pages, ...accountResults].slice(0, 20);
  }, [accounts, query, visibleNavigation]);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', isDark);
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
  }, [isDark]);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 15_000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!userMenuOpen) return;
    const closeOnOutsideClick = (event: MouseEvent) => {
      if (event.target instanceof Element && !event.target.closest('[data-user-menu]')) setUserMenuOpen(false);
    };
    window.addEventListener('mousedown', closeOnOutsideClick);
    return () => window.removeEventListener('mousedown', closeOnOutsideClick);
  }, [userMenuOpen]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setPaletteOpen(true);
        return;
      }
      if (isTypingTarget(event.target)) return;
      if (event.key === '?' && !isTypingTarget(event.target)) {
        event.preventDefault();
        setShortcutHelpOpen(true);
        return;
      }
      const key = event.key.toLowerCase();
      if (pendingGoShortcut.current !== undefined) {
        window.clearTimeout(pendingGoShortcut.current);
        pendingGoShortcut.current = undefined;
        const pathByKey: Record<string, string> = { d: '/dashboard', a: '/activity', q: '/approvals', p: '/policies', c: '/counterparties', o: '/obligations' };
        const path = pathByKey[key];
        if (path && visibleNavigation.some((item) => item.path === path)) navigate(`${path}${location.search}`);
        return;
      }
      if (key === 'g') {
        pendingGoShortcut.current = window.setTimeout(() => {
          pendingGoShortcut.current = undefined;
        }, 800);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => {
      window.removeEventListener('keydown', onKeyDown);
      if (pendingGoShortcut.current !== undefined) window.clearTimeout(pendingGoShortcut.current);
    };
  }, [location.search, navigate, visibleNavigation]);

  useEffect(() => {
    if (!searchParams.has('account')) {
      const savedId = localStorage.getItem('remitmon.selectedAccount');
      if (savedId && accounts.some((account) => account.id === savedId)) {
        const next = new URLSearchParams(searchParams);
        next.set('account', savedId);
        setSearchParams(next, { replace: true });
      }
    }
  }, [accounts, searchParams, setSearchParams]);

  const setAccount = (id: string) => {
    const next = new URLSearchParams(searchParams);
    if (id) {
      next.set('account', id);
      localStorage.setItem('remitmon.selectedAccount', id);
    } else {
      next.delete('account');
      localStorage.removeItem('remitmon.selectedAccount');
    }
    setSearchParams(next);
  };

  const openCommandResult = (path: string) => {
    navigate(path);
    setPaletteOpen(false);
    setQuery('');
  };

  const activeTitle = visibleNavigation.find((item) => location.pathname === item.path || location.pathname.startsWith(`${item.path}/`))?.label ?? 'RemitMon';

  return (
    <div className="app-shell">
      <div className="shell-frame">
        <aside className={`sidebar ${menuOpen ? 'sidebar-open' : ''}`} aria-label="Primary navigation">
          <div className="brand-lockup">
            <div className="brand-mark">R</div>
            <div><p className="brand-name">RemitMon</p><p className="brand-caption">Payment protection</p></div>
            <button className="icon-button ml-auto lg:hidden" aria-label="Close navigation" onClick={() => setMenuOpen(false)}><X size={18} /></button>
          </div>
          <div className="nav-section-label">Workspace</div>
          <nav className="nav-list">
            {visibleNavigation.map(({ label, path, icon: Icon }) => (
              <NavLink
                key={path}
                to={`${path}${location.search}`}
                onClick={() => setMenuOpen(false)}
                className={({ isActive }) => `nav-link ${isActive ? 'nav-link-active' : ''}`}
              >
                <Icon size={18} strokeWidth={1.8} /><span>{label}</span>
                {label === 'Approvals' && !approvalsLoading && !approvalsError && openApprovals.length > 0 && <span className="nav-count">{openApprovals.length}</span>}
              </NavLink>
            ))}
          </nav>
          <div className="sidebar-bottom">
            <div className="sidebar-status">
              <span className={`status-dot ${accountsError ? 'status-dot-error' : ''}`} />
              {accountsError ? 'API unavailable' : accountsLoading ? 'Checking API…' : 'API connected'}
            </div>
            <p className="sidebar-version">Prototype · Business admin</p>
          </div>
        </aside>

        <div className="shell-content">
          <header className="topbar">
            <div className="flex min-w-0 items-center gap-3">
              <button className="icon-button lg:hidden" aria-label="Open navigation" onClick={() => setMenuOpen(true)}>
                <span className="menu-glyph" aria-hidden="true">☰</span>
              </button>
              <div className="breadcrumb"><span>Workspace</span><span className="breadcrumb-slash">/</span><strong>{activeTitle}</strong></div>
            </div>

            <div className="topbar-actions">
              <label className="account-select">
                <Building2 size={16} aria-hidden="true" />
                <span className="sr-only">Switch account</span>
                <select
                  aria-label="Switch account"
                  value={accountId}
                  onChange={(event) => setAccount(event.target.value)}
                  disabled={accountsLoading}
                >
                  <option value="">                  {accountsError ? 'Accounts unavailable' : accountsLoading ? 'Loading accounts…' : 'All accounts'}</option>
                  {accounts.map((account) => <option key={account.id} value={account.id}>{account.displayName}</option>)}
                </select>
                <ChevronDown size={14} aria-hidden="true" />
              </label>
              <span className={`mode-badge ${activeModeClass}`} title={activeAccount ? `Account mode: ${activeMode}` : 'Showing all account modes'}>
                {activeMode}
              </span>
              <button className="global-search" onClick={() => setPaletteOpen(true)} aria-label="Open command palette">
                <Search size={16} /><span>Search</span><kbd><Command size={11} /> K</kbd>
              </button>
              <Link to={`/approvals${location.search}`} className={`approval-pill ${nearestDeadline && nearestDeadline - now <= approvalThresholds.criticalMs ? 'approval-critical' : nearestDeadline && nearestDeadline - now <= approvalThresholds.warningMs ? 'approval-warning' : ''}`} aria-label={approvalsError ? 'Approvals could not be loaded' : `${openApprovals.length} open approvals${nearestDeadline ? `, nearest deadline ${formatRemaining(nearestDeadline, now)}` : ''}`}>
                <AlertTriangle size={16} />
                <span>{approvalsError ? '!' : approvalsLoading ? '…' : openApprovals.length}</span>
                {nearestDeadline && <span className="approval-deadline">{formatRemaining(nearestDeadline, now)}</span>}
              </Link>
              <button className="icon-button" aria-label={`Switch to ${isDark ? 'light' : 'dark'} theme`} onClick={() => setIsDark((current) => !current)}>
                {isDark ? <Sun size={18} /> : <Moon size={18} />}
              </button>
              <div className="user-menu" data-user-menu>
                <button className="user-avatar" aria-label="Open profile menu" aria-haspopup="menu" aria-expanded={userMenuOpen} title="Prototype business admin" onClick={() => setUserMenuOpen((open) => !open)}>BA</button>
                {userMenuOpen && (
                  <div className="user-menu-panel" role="menu">
                    <p className="user-menu-title">Prototype user</p>
                    <p className="user-menu-subtitle">Business admin · presentation role</p>
                    <button role="menuitem" onClick={() => {
                      navigate(`/accounts${location.search}`);
                      setUserMenuOpen(false);
                    }}>Manage accounts</button>
                    <button role="menuitem" onClick={() => {
                      setIsDark((current) => !current);
                      setUserMenuOpen(false);
                    }}>Toggle appearance</button>
                  </div>
                )}
              </div>
            </div>
          </header>

          <main className="main-content"><Outlet /></main>
        </div>
      </div>

      {menuOpen && <button className="mobile-backdrop lg:hidden" aria-label="Close navigation" onClick={() => setMenuOpen(false)} />}
      {paletteOpen && (
        <div className="modal-backdrop" onMouseDown={(event) => {
          if (event.target === event.currentTarget) setPaletteOpen(false);
        }}>
          <section className="command-dialog" role="dialog" aria-modal="true" aria-labelledby="command-title">
            <h2 id="command-title" className="sr-only">Command palette</h2>
            <div className="command-input-wrap">
              <Search size={18} aria-hidden="true" />
              <input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={(event) => {
                if (event.key === 'Escape') setPaletteOpen(false);
                if (event.key === 'Enter' && commandResults[0]) openCommandResult(commandResults[0].path);
              }} placeholder="Search pages and accounts…" aria-label="Search pages and accounts" />
              <kbd>ESC</kbd>
            </div>
            <div className="command-results">
              <p className="command-group-title">Quick access</p>
              {commandResults.map((result) => (
                <button key={`${result.kind}:${result.path}`} className="command-result" onClick={() => openCommandResult(result.path)}>
                  {result.kind === 'page' ? <FileClock size={17} /> : <Building2 size={17} />}
                  <span>{result.label}</span>
                  <span className="command-result-type">{result.kind === 'page' ? 'Page' : accountModeLabel(result.mode)}</span>
                </button>
              ))}
              {commandResults.length === 0 && <p className="command-empty">No pages or accounts match “{query}”.</p>}
            </div>
          </section>
        </div>
      )}
      {shortcutHelpOpen && (
        <div className="modal-backdrop" onMouseDown={(event) => {
          if (event.target === event.currentTarget) setShortcutHelpOpen(false);
        }}>
          <section className="command-dialog max-w-md" role="dialog" aria-modal="true" aria-labelledby="shortcuts-title">
            <div className="flex items-center justify-between"><h2 id="shortcuts-title" className="text-lg font-semibold">Keyboard shortcuts</h2><button className="icon-button" aria-label="Close shortcuts" onClick={() => setShortcutHelpOpen(false)}><X size={17} /></button></div>
            <dl className="shortcut-list">
              <div><dt>Open command palette</dt><dd><kbd>⌘ K</kbd> / <kbd>Ctrl K</kbd></dd></div>
              <div><dt>Dashboard</dt><dd><kbd>G</kbd> then <kbd>D</kbd></dd></div>
              <div><dt>Activity</dt><dd><kbd>G</kbd> then <kbd>A</kbd></dd></div>
              <div><dt>Approvals</dt><dd><kbd>G</kbd> then <kbd>Q</kbd></dd></div>
              <div><dt>Policies</dt><dd><kbd>G</kbd> then <kbd>P</kbd></dd></div>
              <div><dt>Counterparties</dt><dd><kbd>G</kbd> then <kbd>C</kbd></dd></div>
              <div><dt>Obligations</dt><dd><kbd>G</kbd> then <kbd>O</kbd></dd></div>
              <div><dt>Show this help</dt><dd><kbd>?</kbd></dd></div>
            </dl>
          </section>
        </div>
      )}
    </div>
  );
};

function isTypingTarget(target: EventTarget | null) {
  return target instanceof HTMLElement
    && (target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));
}

function formatRemaining(deadline: number, now: number) {
  if (deadline <= now) return 'Expired';
  const minutes = Math.max(0, Math.floor((deadline - now) / 60_000));
  const hours = Math.floor(minutes / 60);
  return hours > 0 ? `${hours}h ${minutes % 60}m` : `${minutes}m`;
}
