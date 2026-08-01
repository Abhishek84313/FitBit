import { delta as computeDelta } from '../utils/format';

/**
 * Stat tile contract: label (sentence case, no trailing colon) · value
 * (semibold, auto-compacted, proportional figures) · delta (signed, vs a named
 * period, with an arrow glyph so direction is never color-alone).
 */
export default function StatCard({ label, value, current, previous, comparisonLabel }) {
  const change = computeDelta(current, previous);

  return (
    <div className="card">
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}</div>
      {change ? (
        <div className={`stat-delta ${change.direction}`}>
          <span className="delta-figure">
            <span aria-hidden="true">
              {change.direction === 'up' ? '↑' : change.direction === 'down' ? '↓' : '→'}
            </span>{' '}
            {change.label}
          </span>
          <span>vs {comparisonLabel}</span>
        </div>
      ) : (
        <div className="stat-delta">
          <span>no {comparisonLabel} data</span>
        </div>
      )}
    </div>
  );
}
