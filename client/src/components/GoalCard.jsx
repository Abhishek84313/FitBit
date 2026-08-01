import Meter from './Meter';
import { METRIC_LABELS, PERIOD_LABELS } from '../utils/format';
import { formatKeyLong } from '../utils/date';

export default function GoalCard({ goal, onDelete, pendingDeleteId }) {
  return (
    <div className="card">
      <div className="goal-head">
        <div>
          <h3>{goal.title}</h3>
          <div className="goal-meta">
            {METRIC_LABELS[goal.metric] ?? goal.metric} · {PERIOD_LABELS[goal.period] ?? goal.period}
            {' · '}
            {formatKeyLong(goal.windowStart)} – {formatKeyLong(goal.windowEnd)}
          </div>
        </div>

        {onDelete && (
          <button
            type="button"
            className={pendingDeleteId === goal.id ? 'btn btn-sm btn-danger' : 'btn btn-sm btn-ghost'}
            onClick={() => onDelete(goal)}
          >
            {pendingDeleteId === goal.id ? 'Confirm' : 'Delete'}
          </button>
        )}
      </div>

      <Meter goal={goal} />
    </div>
  );
}
