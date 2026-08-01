import api from './client';

export const listGoals = (activeOnly = false) =>
  api.get('/goals', { params: { activeOnly } }).then((r) => r.data);

export const createGoal = (payload) => api.post('/goals', payload).then((r) => r.data);

export const updateGoal = (id, payload) => api.put(`/goals/${id}`, payload).then((r) => r.data);

export const deleteGoal = (id) => api.delete(`/goals/${id}`);
