export default function FilterBar({ types, type, from, to, onChange, onClear, hasFilters }) {
  return (
    <div className="filter-bar">
      <div className="field">
        <label htmlFor="fb-type">Type</label>
        <select
          id="fb-type"
          className="input"
          value={type}
          onChange={(event) => onChange('type', event.target.value)}
        >
          <option value="">All types</option>
          {types.map((option) => <option key={option} value={option}>{option}</option>)}
        </select>
      </div>

      <div className="field">
        <label htmlFor="fb-from">From</label>
        <input
          id="fb-from" type="date" className="input" value={from}
          onChange={(event) => onChange('from', event.target.value)}
        />
      </div>

      <div className="field">
        <label htmlFor="fb-to">To</label>
        <input
          id="fb-to" type="date" className="input" value={to}
          onChange={(event) => onChange('to', event.target.value)}
        />
      </div>

      {hasFilters && (
        <button type="button" className="btn btn-ghost" onClick={onClear}>
          Clear filters
        </button>
      )}
    </div>
  );
}
