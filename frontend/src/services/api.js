import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || '/api';

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
  login: (email, password) => api.post('/auth/login', { email, password }),
  register: (data) => api.post('/auth/register', data),
  me: () => api.get('/auth/me'),
  getUsers: () => api.get('/auth/users'),
};

export const projectService = {
  getAll: () => api.get('/projects'),
  getById: (id) => api.get(`/projects/${id}`),
  generate: (data) => api.post('/projects/generate', data),
  assignTask: (projectId, taskId, userId) => api.post(`/projects/${projectId}/assign`, { taskId, userId }),
  getDashboard: (id) => api.get(`/projects/${id}/dashboard`),
};

export const taskService = {
  getTasks: () => api.get('/tasks'),
  getMyTasks: () => api.get('/tasks/my-tasks'),
  completeTask: (id) => api.put(`/tasks/${id}/complete`),
  updateStatus: (id, status) => api.put(`/tasks/${id}`, { status }),
  createTask: (data) => api.post('/tasks', data),
};

export const dashboardService = {
  getGlobalStats: () => api.get('/dashboard'),
};

export default api;
