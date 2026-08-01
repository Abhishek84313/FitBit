import { compact, duration } from '../utils/format';
import { formatKey } from '../utils/date';

/**
 * The chart's table-view twin. This is the accessibility fallback and it makes
 * every value reachable without hover — a tooltip must never be the only path to
 * a number.
 */
export default function TrendTable({ trend }) {
  return (
    <div className="scroll-x">
      <table className="table">
        <thead>
          <tr>
            <th scope="col">Day</th>
            <th scope="col" className="num">Activities</th>
            <th scope="col" className="num">Active minutes</th>
            <th scope="col" className="num">Distance (km)</th>
            <th scope="col" className="num">Calories</th>
          </tr>
        </thead>
        <tbody>
          {trend.map((point) => (
            <tr key={point.dateKey}>
              <th scope="row" style={{ fontWeight: 550 }}>{formatKey(point.dateKey)}</th>
              <td className="num">{point.activities}</td>
              <td className="num">{point.durationMinutes ? duration(point.durationMinutes) : '—'}</td>
              <td className="num">{point.distanceKm ? point.distanceKm.toFixed(1) : '—'}</td>
              <td className="num">{point.calories ? compact(point.calories) : '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
