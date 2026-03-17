import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || '';

const api = axios.create({
  baseURL: API_URL,
  timeout: 60000, // 60 seconds for AI generation
});

// Request interceptor to add JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export const authService = {
  login: (email, password) => api.post('/api/auth/login', { email, password }),
  register: (data) => api.post('/api/auth/register', data),
  me: () => api.get('/api/auth/me'),
  getUsers: () => api.get('/api/auth/users'),
};

export const projectService = {
  getAll: () => api.get('/api/projects'),
  getById: (id) => api.get(`/api/projects/${id}`),
  generate: (data) => api.post('/api/projects/generate', data),
  assignTask: (projectId, taskId, userId) => api.post(`/api/projects/${projectId}/assign`, { taskId, userId }),
  getDashboard: (id) => api.get(`/api/projects/${id}/dashboard`),
};

export const taskService = {
  getTasks: () => api.get('/api/tasks'),
  getMyTasks: () => api.get('/api/tasks/my-tasks'),
  completeTask: (id) => api.put(`/api/tasks/${id}/complete`),
  updateStatus: (id, status) => api.put(`/api/tasks/${id}`, { status }),
  createTask: (data) => api.post('/api/tasks', data),
};

export const dashboardService = {
  getGlobalStats: () => api.get('/api/dashboard'),
};

export default api;
