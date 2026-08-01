import axios from 'axios';

export const TOKEN_KEY = 'fitbit.token';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
});

// Read the token from localStorage, NOT from React state. This interceptor is
// registered once at module scope, outside React — closing over a state variable
// would capture null at registration time and every request after login would go
// out unauthenticated.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const url = error.config?.url ?? '';
    // Exempt /auth/* from the redirect: otherwise a wrong password on the login
    // page redirects *to* the login page, remounting the form and wiping the
    // error message before it can be read — it looks like the button does nothing.
    if (error.response?.status === 401 && !url.includes('/auth/')) {
      localStorage.removeItem(TOKEN_KEY);
      window.location.assign('/login');
    }
    return Promise.reject(error);
  },
);

/** Pulls the API's { message } out of an axios error, with a usable fallback. */
export function errorMessage(error, fallback = 'Something went wrong.') {
  return error?.response?.data?.message ?? error?.message ?? fallback;
}

export default api;
