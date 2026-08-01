import { useCallback, useState } from 'react';
import { createGoal, deleteGoal, listGoals } from '../api/goals';
import { errorMessage } from '../api/client';
import { useAsync } from '../hooks/useAsync';
import GoalCard from '../components/GoalCard';
import GoalForm from '../components/GoalForm';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';
import EmptyState from '../components/EmptyState';

export default function GoalsPage() {
  const [pendingDeleteId, setPendingDeleteId] = useState(null);
  const [actionError, setActionError] = useState(null);

  const load = useCallback(() => listGoals(false), []);
  const { data, error, loading, refreshing, reload } = useAsync(load, []);

  const create = async (payload) => {
    await createGoal(payload);
    await reload();
  };

  const remove = async (goal) => {
    if (pendingDeleteId !== goal.id) {
      setPendingDeleteId(goal.id);
      return;
    }

    try {
      setActionError(null);
      await deleteGoal(goal.id);
      setPendingDeleteId(null);
      await reload();
    } catch (err) {
      setActionError(errorMessage(err, 'Could not delete the goal.'));
      setPendingDeleteId(null);
    }
  };

  return (
    <div>
      <div className="page-head">
        <div>
          <h1>Goals</h1>
          <p>Progress is computed server-side, so these always match your dashboard.</p>
        </div>
      </div>

      <div className="stack">
        <GoalForm onSubmit={create} />

        {actionError && <ErrorBanner message={actionError} />}

        {loading ? (
          <Spinner block />
        ) : error ? (
          <ErrorBanner message={error} onRetry={reload} />
        ) : data.length === 0 ? (
          <div className="card">
            <EmptyState
              title="No goals yet"
              message="Create one above — it will start tracking against activities you have already logged."
            />
          </div>
        ) : (
          <div className={`goal-grid ${refreshing ? 'is-refreshing' : ''}`}>
            {data.map((goal) => (
              <GoalCard
                key={goal.id}
                goal={goal}
                onDelete={remove}
                pendingDeleteId={pendingDeleteId}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
