import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to attach JWT
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('its_access_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for 401 refresh token handling
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: Error | null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve();
    }
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry && !originalRequest.url?.includes('/auth/login') && !originalRequest.url?.includes('/auth/refresh')) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(() => apiClient(originalRequest))
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('its_refresh_token');
      if (!refreshToken) {
        localStorage.removeItem('its_access_token');
        localStorage.removeItem('its_refresh_token');
        localStorage.removeItem('its_user');
        window.location.href = '/login';
        return Promise.reject(error);
      }

      try {
        const response = await axios.post(`${API_BASE_URL}/auth/refresh`, { refreshToken });
        const { accessToken, refreshToken: newRefreshToken, user } = response.data;

        localStorage.setItem('its_access_token', accessToken);
        localStorage.setItem('its_refresh_token', newRefreshToken);
        if (user) {
          localStorage.setItem('its_user', JSON.stringify(user));
        }

        apiClient.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
        processQueue(null);

        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError as Error);
        localStorage.removeItem('its_access_token');
        localStorage.removeItem('its_refresh_token');
        localStorage.removeItem('its_user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
