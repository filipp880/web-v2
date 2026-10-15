// src/hooks/useApi.ts

import { useState, useCallback, useRef } from 'react';
import { useToast } from '../contexts/ToastContext';

interface UseApiOptions<T> {
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  successMessage?: string;
  errorMessage?: string;
}

export function useApi<T, Args extends unknown[]>(
  apiCall: (...args: Args) => Promise<T>,
  options: UseApiOptions<T> = {}
) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const { showToast } = useToast();
  const latest = useRef({ apiCall, options, showToast });
  latest.current = { apiCall, options, showToast };

  const execute = useCallback(async (...args: Args): Promise<T | null> => {
    const { apiCall, options, showToast } = latest.current;
    setLoading(true);
    setError(null);
    try {
      const data = await apiCall(...args);
      if (options.successMessage) {
        showToast('success', options.successMessage);
      }
      options.onSuccess?.(data);
      return data;
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Произошла ошибка');
      setError(error);
      if (options.errorMessage) {
        showToast('error', options.errorMessage);
      } else if (err instanceof Error && err.message) {
        showToast('error', err.message);
      }
      options.onError?.(error);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  return { execute, loading, error };
}
