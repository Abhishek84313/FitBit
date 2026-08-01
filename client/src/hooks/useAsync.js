import { useCallback, useEffect, useRef, useState } from 'react';
import { errorMessage } from '../api/client';

/**
 * Runs an async loader whenever `deps` change.
 *
 * `refreshing` is separate from `loading` on purpose: the first load shows a
 * spinner, but a refetch (a filter change, a page change) keeps the previous
 * render on screen and dims it. Swapping in a skeleton on every keystroke causes
 * a layout jump.
 */
export function useAsync(loader, deps = []) {
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const hasLoaded = useRef(false);
  const runId = useRef(0);

  const run = useCallback(() => {
    const id = ++runId.current;
    if (hasLoaded.current) setRefreshing(true);
    else setLoading(true);

    return loader()
      .then((result) => {
        if (id !== runId.current) return;   // a newer request already won
        setData(result);
        setError(null);
        hasLoaded.current = true;
      })
      .catch((err) => {
        if (id !== runId.current) return;
        setError(errorMessage(err));
      })
      .finally(() => {
        if (id !== runId.current) return;
        setLoading(false);
        setRefreshing(false);
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    run();
  }, [run]);

  return { data, error, loading, refreshing, reload: run, setData };
}
