import axios from 'axios';
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { env } from '@/config/env';
import type { useAuthStore } from '@/stores/authStore';

const apiClient: AxiosInstance = axios.create({
    baseURL: env.API_BASE_URL,
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
    },
});

// A queue for requests that failed due to a 401 error while the token is being refreshed.
let failedQueue: Array<{
    resolve: (value?: any) => void;
    reject: (error?: any) => void;
}> = [];

let isRefreshing = false;

const processQueue = (error: Error | null, token: string | null = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

/**
 * Sets up the Axios interceptors for handling authentication and token refresh.
 * This should be called once when the application initializes.
 * @param authStore An instance of the authentication store.
 */
export function setupInterceptors(authStore: ReturnType<typeof useAuthStore>): void {
    
    // Request Interceptor: Add the JWT token to every outgoing request.
    apiClient.interceptors.request.use(
        (config) => {
            const token = authStore.getAccessToken; // Directly and synchronously access the getter
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
            return config;
        },
        (error) => {
            return Promise.reject(error);
        }
    );

    // Response Interceptor: Handle token expiration and refresh logic.
    apiClient.interceptors.response.use(
        (response: AxiosResponse) => response,
        async (error) => {
            const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

            // Check if the error is a 401 and we haven't retried this request yet.
            if (error.response?.status === 401 && !originalRequest._retry) {
                if (isRefreshing) {
                    // If a refresh is already in progress, queue the request.
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then(token => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return apiClient(originalRequest);
                    });
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    await authStore.refreshTokens(); 

                    const newToken = authStore.getAccessToken;
                    processQueue(null, newToken);
                    originalRequest.headers.Authorization = `Bearer ${newToken}`;
                    return apiClient(originalRequest);
                } catch (refreshError: any) {
                    processQueue(refreshError, null);
                    await authStore.logout();
                    return Promise.reject(refreshError);
                } finally {
                    isRefreshing = false;
                }
            }
            
            console.error('API Error:', error.response?.data || error.message);
            return Promise.reject(error);
        }
    );
}

export default apiClient;