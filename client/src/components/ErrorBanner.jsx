export default function ErrorBanner({ message, onRetry }) {
  if (!message) return null;

  return (
    <div className="banner" role="alert">
      <span aria-hidden="true">⚠</span>
      <span style={{ flex: 1 }}>{message}</span>
      {onRetry && (
        <button type="button" className="btn btn-sm btn-ghost" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  );
}
