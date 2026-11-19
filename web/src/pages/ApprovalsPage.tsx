import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { AlertTriangle, Clock3 } from 'lucide-react';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { useApprovals, useResolveApproval } from '../hooks/useApprovals';
import type { ApprovalRecord } from '../hooks/useApprovals';

const remaining = (deadline: string | null, now: number) => {
  if (!deadline) return 'No deadline';
  const milliseconds = new Date(deadline).getTime() - now;
  if (milliseconds <= 0) return 'Expired';
  const hours = Math.floor(milliseconds / 3_600_000);
  const minutes = Math.floor((milliseconds % 3_600_000) / 60_000);
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
};

const dateTime = (date: string) =>
  new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(date));

export const ApprovalsPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const accountId = new URLSearchParams(location.search).get('account') ?? undefined;
  const { data: approvals, isLoading, error } = useApprovals(accountId);
  const [note, setNote] = useState('');
  const [reason, setReason] = useState('');
  const [now, setNow] = useState(Date.now());
  const resolve = useResolveApproval();

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => window.clearInterval(timer);
  }, []);

  if (isLoading) return <LoadingSpinner />;
  if (error || !approvals) {
    return <p role="alert" className="text-sm text-red-700 dark:text-red-300">Could not load the approvals queue.</p>;
  }

  const openApprovals = approvals
    .filter((approval) => approval.status === 'Open')
    .sort((left, right) => deadlineValue(left) - deadlineValue(right));
  const selected = id ? approvals.find((approval) => approval.id === id) : undefined;

  const submit = async (approval: ApprovalRecord, action: 'approve' | 'reject') => {
    if (!note.trim() || (action === 'reject' && !reason.trim())) return;
    try {
      await resolve.mutateAsync({ id: approval.id, action, note: note.trim(), reason: reason.trim() });
      setNote('');
      setReason('');
      navigate({ pathname: '/approvals', search: location.search });
    } catch {
      // The mutation error is rendered below.
    }
  };

  return (
    <div className="space-y-6">
      <header className="page-heading">
        <div>
          <p className="eyebrow">Review queue</p>
          <h1>Approvals</h1>
          <p>Open decisions requiring review, sorted by deadline.</p>
        </div>
        <span className="subtle-count"><AlertTriangle size={16} /> {openApprovals.length} open</span>
      </header>

      <section className="panel overflow-hidden">
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>Status</th><th>Approval</th><th>Created</th><th>Deadline</th><th>Assigned to</th></tr></thead>
            <tbody>
              {openApprovals.map((approval) => (
                <tr key={approval.id} className={id === approval.id ? 'row-selected' : ''}>
                  <td><span className="status-pill status-warning">{approval.status}</span></td>
                  <td><Link className="entity-link" to={{ pathname: `/approvals/${approval.id}`, search: location.search }}>{approval.id.slice(0, 8)}</Link></td>
                  <td>{dateTime(approval.createdAt)}</td>
                  <td><Deadline expiresAt={approval.expiresAt} now={now} /></td>
                  <td>{approval.assignedToUserId ?? 'Unassigned'}</td>
                </tr>
              ))}
              {openApprovals.length === 0 && <tr><td colSpan={5} className="empty-row">No open approvals.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      {selected && (
        <section className="panel p-5 sm:p-6" aria-labelledby="approval-detail-title">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div><p className="eyebrow">Approval detail</p><h2 id="approval-detail-title" className="text-xl font-semibold">{selected.id}</h2></div>
            <span className={`status-pill ${selected.status === 'Open' ? 'status-warning' : 'status-neutral'}`}>{selected.status}</span>
          </div>
          <dl className="detail-list mt-5">
            <div><dt>Decision</dt><dd>{selected.decisionId}</dd></div>
            <div><dt>Created</dt><dd>{dateTime(selected.createdAt)}</dd></div>
            <div><dt>Deadline</dt><dd><Deadline expiresAt={selected.expiresAt} now={now} /></dd></div>
            <div><dt>Assigned to</dt><dd>{selected.assignedToUserId ?? 'Unassigned'}</dd></div>
          </dl>
          {selected.status === 'Open' && (
            <div className="mt-6 max-w-xl space-y-3">
              <label className="form-label">Required resolution note
                <textarea className="form-control" value={note} onChange={(event) => setNote(event.target.value)} rows={3} />
              </label>
              <label className="form-label">Rejection reason
                <input className="form-control" value={reason} onChange={(event) => setReason(event.target.value)} />
              </label>
              {resolve.error && <p role="alert" className="text-sm text-red-700 dark:text-red-300">The approval could not be updated. Please retry.</p>}
              <div className="flex flex-wrap gap-2">
                <button className="btn-primary" disabled={resolve.isPending || !note.trim()} onClick={() => void submit(selected, 'approve')}>Approve</button>
                <button className="btn-danger" disabled={resolve.isPending || !note.trim() || !reason.trim()} onClick={() => void submit(selected, 'reject')}>Reject</button>
                <Link className="btn-secondary" to={{ pathname: '/approvals', search: location.search }}>Close</Link>
              </div>
            </div>
          )}
        </section>
      )}
    </div>
  );
};

const deadlineValue = (approval: ApprovalRecord) =>
  approval.expiresAt ? new Date(approval.expiresAt).getTime() : Number.MAX_SAFE_INTEGER;

const Deadline = ({ expiresAt, now }: { expiresAt: string | null; now: number }) => {
  const milliseconds = expiresAt ? new Date(expiresAt).getTime() - now : Number.MAX_SAFE_INTEGER;
  const severity = milliseconds <= 0 || milliseconds < 30 * 60_000
    ? 'deadline-critical'
    : milliseconds < 4 * 3_600_000
      ? 'deadline-warning'
      : '';
  return (
    <span className={`inline-flex items-center gap-1 ${severity}`}>
      <Clock3 size={14} aria-hidden="true" />
      <span>{remaining(expiresAt, now)}</span>
    </span>
  );
};
