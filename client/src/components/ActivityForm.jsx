import { useState } from 'react';
import { errorMessage } from '../api/client';
import { todayKey } from '../utils/date';
import ErrorBanner from './ErrorBanner';

export default function ActivityForm({ types, initial, onSubmit, onCancel }) {
  const [form, setForm] = useState(() => ({
    type: initial?.type ?? types[0] ?? 'Running',
    date: initial?.dateKey ?? todayKey(),
    durationMinutes: initial?.durationMinutes ?? 30,
    distanceKm: initial?.distanceKm ?? '',
    calories: initial?.calories ?? '',
    notes: initial?.notes ?? '',
  }));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  const set = (key) => (event) => setForm((prev) => ({ ...prev, [key]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError(null);

    try {
      await onSubmit({
        type: form.type,
        date: form.date,
        durationMinutes: Number(form.durationMinutes),
        // Empty string must become null, not 0 — "not recorded" and "zero
        // kilometres" are different facts and the dashboard sums them differently.
        distanceKm: form.distanceKm === '' ? null : Number(form.distanceKm),
        calories: form.calories === '' ? null : Number(form.calories),
        notes: form.notes.trim() === '' ? null : form.notes.trim(),
      });
    } catch (err) {
      setError(errorMessage(err, 'Could not save the activity.'));
      setSaving(false);
    }
  };

  return (
    <form onSubmit={submit}>
      {error && <div style={{ marginBottom: 14 }}><ErrorBanner message={error} /></div>}

      <div className="form-grid">
        <div className="field">
          <label htmlFor="af-type">Activity type</label>
          <select id="af-type" className="input" value={form.type} onChange={set('type')}>
            {types.map((type) => <option key={type} value={type}>{type}</option>)}
          </select>
        </div>

        <div className="field">
          <label htmlFor="af-date">Date</label>
          <input id="af-date" type="date" className="input" value={form.date} onChange={set('date')} required />
        </div>

        <div className="field">
          <label htmlFor="af-duration">Duration (minutes)</label>
          <input
            id="af-duration" type="number" className="input" min="1" max="1440" required
            value={form.durationMinutes} onChange={set('durationMinutes')}
          />
        </div>

        <div className="field">
          <label htmlFor="af-distance">Distance (km)</label>
          <input
            id="af-distance" type="number" className="input" min="0" max="1000" step="0.1"
            value={form.distanceKm} onChange={set('distanceKm')} placeholder="optional"
          />
        </div>

        <div className="field">
          <label htmlFor="af-calories">Calories</label>
          <input
            id="af-calories" type="number" className="input" min="0" max="20000"
            value={form.calories} onChange={set('calories')} placeholder="optional"
          />
        </div>
      </div>

      <div className="field" style={{ marginTop: 14 }}>
        <label htmlFor="af-notes">Notes</label>
        <textarea
          id="af-notes" className="input" maxLength={500}
          value={form.notes} onChange={set('notes')} placeholder="optional"
        />
      </div>

      <div className="form-actions">
        <button type="button" className="btn" onClick={onCancel} disabled={saving}>Cancel</button>
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? 'Saving…' : initial ? 'Save changes' : 'Add activity'}
        </button>
      </div>
    </form>
  );
}
