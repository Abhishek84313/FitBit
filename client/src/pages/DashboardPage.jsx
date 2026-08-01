import { useCallback, useState } from 'react';
import { Link } from 'react-router-dom';
import { dashboardSummary } from '../api/dashboard';
import { useAsync } from '../hooks/useAsync';
import { useAuth } from '../hooks/useAuth';
import { compact, duration, km } from '../utils/format';
import StatCard from '../components/StatCard';
import TrendChart from '../components/TrendChart';
import TrendTable from '../components/TrendTable';
import GoalCard from '../components/GoalCard';
import Spinner from '../components/Spinner';
import ErrorBanner from '../components/ErrorBanner';
import EmptyState from '../components/EmptyState';

const RANGES = [7, 14, 30];

export default function DashboardPage() {
  const { user } = useAuth();
  const [days, setDays] = useState(7);
  const [showTable, setShowTable] = useState(false);

  const load = useCallback(() => dashboardSummary(days), [days]);
  const { data, error, loading, refreshing, reload } = useAsync(load, [days]);

  if (loading) return <Spinner block />;
  if (error) return <ErrorBanner message={error} onRetry={reload} />;
  if (!data) return null;

  const comparison = `previous ${days} days`;
  const isEmpty = data.totals.activities === 0 && data.goals.length === 0;

  return (
    <div className={refreshing ? 'is-refreshing' : undefined}>
      <div className="page-head">
        <div>
          <h1>Hello, {user?.displayName?.split(' ')[0] ?? 'there'}</h1>
          <p>
            {data.currentStreakDays > 0
              ? `${data.currentStreakDays}-day streak · ${data.from} to ${data.to}`
              : `${data.from} to ${data.to}`}
          </p>
        </div>

        {/* One page-level range selector scoping BOTH the tiles and the chart.
            A per-chart filter that silently disagrees with the tiles beside it
            is a classic dashboard bug. */}
        <div className="segmented" role="group" aria-label="Date range">
          {RANGES.map((range) => (
            <button
              key={range}
              type="button"
              aria-pressed={days === range}
              onClick={() => setDays(range)}
            >
              {range} days
            </button>
          ))}
        </div>
      </div>

      {isEmpty ? (
        <div className="card">
          <EmptyState
            title="Nothing logged yet"
            message="Add your first activity and this dashboard will fill in — totals, trend and goal progress."
          />
          <div style={{ textAlign: 'center' }}>
            <Link className="btn btn-primary" to="/activities">Log an activity</Link>
          </div>
        </div>
      ) : (
        <div className="stack">
          <div className="stat-grid">
            <StatCard
              label="Activities" value={compact(data.totals.activities)}
              current={data.totals.activities} previous={data.previous.activities}
              comparisonLabel={comparison}
            />
            <StatCard
              label="Active minutes" value={duration(data.totals.durationMinutes)}
              current={data.totals.durationMinutes} previous={data.previous.durationMinutes}
              comparisonLabel={comparison}
            />
            <StatCard
              label="Distance" value={km(data.totals.distanceKm)}
              current={data.totals.distanceKm} previous={data.previous.distanceKm}
              comparisonLabel={comparison}
            />
            <StatCard
              label="Calories" value={compact(data.totals.calories)}
              current={data.totals.calories} previous={data.previous.calories}
              comparisonLabel={comparison}
            />
          </div>

          <div className="card">
            <div className="card-head">
              <div>
                {/* Single series, so no legend box — the title names what is plotted */}
                <h2>Active minutes · last {days} days</h2>
                <div className="card-sub">Days with nothing logged show a stub on the baseline.</div>
              </div>
              <button
                type="button"
                className="btn btn-sm btn-ghost"
                aria-pressed={showTable}
                onClick={() => setShowTable((v) => !v)}
              >
                {showTable ? 'Chart' : 'Table'}
              </button>
            </div>

            {showTable ? <TrendTable trend={data.trend} /> : <TrendChart trend={data.trend} />}
          </div>

          <div>
            <div className="page-head" style={{ marginBottom: 12 }}>
              <h2>Goal progress</h2>
              <Link className="btn btn-sm" to="/goals">Manage goals</Link>
            </div>

            {data.goals.length === 0 ? (
              <div className="card">
                <EmptyState
                  title="No active goals"
                  message="Set a goal and its progress will track your logged activities automatically."
                />
                <div style={{ textAlign: 'center' }}>
                  <Link className="btn btn-primary" to="/goals">Create a goal</Link>
                </div>
              </div>
            ) : (
              <div className="goal-grid">
                {data.goals.map((goal) => <GoalCard key={goal.id} goal={goal} />)}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
