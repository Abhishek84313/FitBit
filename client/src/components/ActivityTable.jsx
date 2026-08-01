import { duration, compact } from '../utils/format';
import { formatKeyLong } from '../utils/date';

export default function ActivityTable({ activities, onEdit, onDelete, pendingDeleteId }) {
  return (
    <div className="scroll-x">
      <table className="table responsive">
        <thead>
          <tr>
            <th scope="col">Date</th>
            <th scope="col">Type</th>
            <th scope="col" className="num">Duration</th>
            <th scope="col" className="num">Distance</th>
            <th scope="col" className="num">Calories</th>
            <th scope="col">Notes</th>
            <th scope="col"><span className="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          {activities.map((activity) => (
            <tr key={activity.id}>
              <td data-label="Date">{formatKeyLong(activity.dateKey)}</td>
              <td data-label="Type">{activity.type}</td>
              <td data-label="Duration" className="num">{duration(activity.durationMinutes)}</td>
              <td data-label="Distance" className="num">
                {activity.distanceKm ? `${activity.distanceKm.toFixed(1)} km` : '—'}
              </td>
              <td data-label="Calories" className="num">
                {activity.calories ? compact(activity.calories) : '—'}
              </td>
              <td data-label="Notes" className="muted">{activity.notes || '—'}</td>
              <td data-label="Actions">
                <div className="row-actions">
                  <button type="button" className="btn btn-sm btn-ghost" onClick={() => onEdit(activity)}>
                    Edit
                  </button>
                  <button
                    type="button"
                    className={pendingDeleteId === activity.id ? 'btn btn-sm btn-danger' : 'btn btn-sm btn-ghost'}
                    onClick={() => onDelete(activity)}
                  >
                    {pendingDeleteId === activity.id ? 'Confirm delete' : 'Delete'}
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
