/**
 * The API speaks "yyyy-MM-dd" calendar-day strings, never timestamps, so the
 * client must build them from LOCAL date parts. new Date().toISOString() would
 * convert to UTC first and, east of Greenwich, hand back yesterday's date for
 * anything logged before the offset — the same trap the server avoids with dateKey.
 */
export function todayKey() {
  return toKey(new Date());
}

export function toKey(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function daysAgoKey(days) {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return toKey(d);
}

/** "2026-08-01" -> "Sat 1 Aug" */
export function formatKey(key) {
  if (!key) return '';
  const [y, m, d] = key.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  return date.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' });
}

/** "2026-08-01" -> "1 Aug 2026" */
export function formatKeyLong(key) {
  if (!key) return '';
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d).toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}
