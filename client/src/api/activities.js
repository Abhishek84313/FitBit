import api from './client';

export const listActivities = (params) =>
  api.get('/activities', { params }).then((r) => r.data);

export const activityTypes = () => api.get('/activities/types').then((r) => r.data);

export const createActivity = (payload) => api.post('/activities', payload).then((r) => r.data);

export const updateActivity = (id, payload) =>
  api.put(`/activities/${id}`, payload).then((r) => r.data);

export const deleteActivity = (id) => api.delete(`/activities/${id}`);
