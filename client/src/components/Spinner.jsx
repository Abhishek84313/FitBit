export default function Spinner({ block = false, label = 'Loading' }) {
  return (
    <div className={block ? 'spinner-block' : undefined} role="status" aria-live="polite">
      <div className="spinner" />
      <span className="sr-only">{label}</span>
    </div>
  );
}
