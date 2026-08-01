import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  activityTypes, createActivity, deleteActivity, listActivities, updateActivity,
} from '../api/activities';
import { errorMessage } from '../api/client';
import { useAsync } from '../hooks/useAsync';
import ActivityTable from '../components/ActivityTable';
import ActivityForm from '../components/ActivityForm';
import FilterBar from '../components/FilterBar';
import Modal from '../components/Modal';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';
import EmptyState from '../components/EmptyState';

export default function ActivitiesPage() {
  // Filters live in the URL so a filtered view is linkable and survives a refresh.
  const [params, setParams] = useSearchParams();
  const [types, setTypes] = useState([]);
  const [editing, setEditing] = useState(null);      // activity | 'new' | null
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [actionError, setActionError] = useState(null);

  const type = params.get('type') ?? '';
  const from = params.get('from') ?? '';
  const to = params.get('to') ?? '';
  const page = Number(params.get('page') ?? 1);

  useEffect(() => {
    activityTypes().then(setTypes).catch(() => setTypes([]));
  }, []);

  const load = useCallback(
    () => listActivities({
      type: type || undefined,
      from: from || undefined,
      to: to || undefined,
      page,
      pageSize: 20,
    }),
    [type, from, to, page],
  );

  const { data, error, loading, refreshing, reload } = useAsync(load, [type, from, to, page]);

  const updateParam = (key, value) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    next.delete('page');            // a changed filter invalidates the page cursor
    setParams(next, { replace: true });
  };

  const goToPage = (nextPage) => {
    const next = new URLSearchParams(params);
    if (nextPage <= 1) next.delete('page');
    else next.set('page', String(nextPage));
    setParams(next, { replace: true });
  };

  const save = async (payload) => {
    if (editing === 'new') await createActivity(payload);
    else await updateActivity(editing.id, payload);
    setEditing(null);
    await reload();
  };

  const remove = async (activity) => {
    // Two-step confirm rather than window.confirm — the button becomes the confirm.
    if (pendingDeleteId !== activity.id) {
      setPendingDeleteId(activity.id);
      return;
    }

    try {
      setActionError(null);
      await deleteActivity(activity.id);
      setPendingDeleteId(null);
      await reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not delete the activity.'));
      setPendingDeleteId(null);
    }
  };

  const hasFilters = Boolean(type || from || to);

  return (
    <div>
      <div className="page-head">
        <div>
          <h1>Activities</h1>
          <p>Log what you did, then filter the history.</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setEditing('new')}>
          Add activity
        </button>
      </div>

      <div className="stack">
        <div className="card">
          <FilterBar
            types={types}
            type={type}
            from={from}
            to={to}
            hasFilters={hasFilters}
            onChange={updateParam}
            onClear={() => setParams(new URLSearchParams(), { replace: true })}
          />
        </div>

        {actionError && <ErrorBanner message={actionError} />}

        <div className="card">
          {loading ? (
            <Spinner block />
          ) : error ? (
            <ErrorBanner message={error} onRetry={reload} />
          ) : data.items.length === 0 ? (
            <EmptyState
              title={hasFilters ? 'No activities match those filters' : 'No activities yet'}
              message={
                hasFilters
                  ? 'Try widening the date range or clearing the type filter.'
                  : 'Log your first workout and it will show up here and on your dashboard.'
              }
              actionLabel={hasFilters ? 'Clear filters' : 'Add activity'}
              onAction={
                hasFilters
                  ? () => setParams(new URLSearchParams(), { replace: true })
                  : () => setEditing('new')
              }
            />
          ) : (
            <div className={refreshing ? 'is-refreshing' : undefined}>
              <ActivityTable
                activities={data.items}
                onEdit={setEditing}
                onDelete={remove}
                pendingDeleteId={pendingDeleteId}
              />

              <div className="pagination">
                <span>
                  Showing {(data.page - 1) * data.pageSize + 1}–
                  {Math.min(data.page * data.pageSize, data.total)} of {data.total}
                </span>
                <div className="row">
                  <button
                    type="button" className="btn btn-sm"
                    disabled={data.page <= 1} onClick={() => goToPage(data.page - 1)}
                  >
                    Previous
                  </button>
                  <span>Page {data.page} of {Math.max(data.totalPages, 1)}</span>
                  <button
                    type="button" className="btn btn-sm"
                    disabled={data.page >= data.totalPages} onClick={() => goToPage(data.page + 1)}
                  >
                    Next
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {editing && (
        <Modal
          title={editing === 'new' ? 'Add activity' : 'Edit activity'}
          onClose={() => setEditing(null)}
        >
          <ActivityForm
            types={types}
            initial={editing === 'new' ? null : editing}
            onSubmit={save}
            onCancel={() => setEditing(null)}
          />
        </Modal>
      )}
    </div>
  );
}
