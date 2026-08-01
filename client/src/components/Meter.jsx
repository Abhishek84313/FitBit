import { metricValue, percent } from '../utils/format';

const STATUS = {
  onTrack: { label: 'On track', icon: '→' },
  atRisk: { label: 'At risk', icon: '!' },
  achieved: { label: 'Achieved', icon: '✓' },
  missed: { label: 'Missed', icon: '✕' },
};

export default function Meter({ goal }) {
  const status = STATUS[goal.status] ?? STATUS.onTrack;
  // The bar geometry clamps at 100%, but the caption shows the true value so an
  // over-target goal reads "32 / 30 km — achieved" rather than a silent 100%.
  const width = Math.min(100, Math.max(0, goal.progressPercent));

  return (
    <div className="meter">
      <div
        className="meter-track"
        role="progressbar"
        aria-valuenow={Math.round(goal.progressPercent)}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={goal.title}
      >
        <div className={`meter-fill ${goal.status}`} style={{ width: `${width}%` }} />
      </div>

      <div className="meter-caption">
        {/* The value is never gated behind hover */}
        <span>
          <strong>{percent(goal.progressPercent)}</strong>
          {' · '}
          {metricValue(goal.metric, goal.currentValue)} / {metricValue(goal.metric, goal.targetValue)}
        </span>
        {/* Icon + text, so status never rests on color alone */}
        <span className={`pill ${goal.status}`}>
          <span aria-hidden="true">{status.icon}</span>
          {status.label}
        </span>
      </div>
    </div>
  );
}
