import { useFocusEffect } from '@react-navigation/native';
import { useCallback, useState } from 'react';
import { useToast } from '../context/ToastContext';
import { getErrorMessage } from '../utils/format';

interface UseAsyncDataOptions {
  loadOnFocus?: boolean;
  errorMessage?: string;
}

export function useAsyncData<T>(
  loader: () => Promise<T>,
  deps: React.DependencyList,
  options: UseAsyncDataOptions = {},
) {
  const { loadOnFocus = true, errorMessage } = options;
  const { showError } = useToast();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    async (isRefresh = false) => {
      if (isRefresh) setRefreshing(true);
      else if (data === null) setLoading(true);
      setError(null);

      try {
        const result = await loader();
        setData(result);
      } catch (e) {
        const msg = getErrorMessage(e, errorMessage);
        setError(msg);
        showError(msg);
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    deps,
  );

  const refresh = useCallback(() => load(true), [load]);

  if (loadOnFocus) {
    useFocusEffect(
      useCallback(() => {
        load();
      }, [load]),
    );
  }

  return {
    data,
    loading: loading && data === null,
    refreshing,
    error,
    refresh,
    reload: () => load(false),
    setData,
  };
}
