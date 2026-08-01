import api from './client';

export const dashboardSummary = (days = 7) =>
  api.get('/dashboard/summary', { params: { days } }).then((r) => r.data);
