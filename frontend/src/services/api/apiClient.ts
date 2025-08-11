import axios from 'axios';
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { env } from '@/config/env';
import type { useAuthStore } from '@/stores/authStore';

const apiClient: AxiosInstance = axios.create({
    baseURL: env.API_BASE_URL,
    timeout: 10000,
    withCredentials: true, // Enable sending cookies with requests
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
 * Check if a URL is an authentication endpoint that doesn't require token refresh
 */
function isAuthEndpoint(url?: string): boolean {
    if (!url) return false;
    return url.includes('/auth/login') || 
           url.includes('/auth/register') || 
           url.includes('/auth/refresh-token') ||
           url.includes('/auth/verify-email') ||
           url.includes('/auth/resend-email-verification');
}

/**
 * Sets up the Axios interceptors for handling authentication and token refresh.
 * This should be called once when the application initializes.
 * @param authStore An instance of the authentication store.
 */
export function setupInterceptors(authStore: ReturnType<typeof useAuthStore>): void {
    // Request Interceptor: Add JWT token whenever available
    apiClient.interceptors.request.use(
        (config) => {
            // Add token if we have a valid one, regardless of initialization state
            const token = authStore.getAccessToken;
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
                // Skip refresh for auth endpoints
                if (isAuthEndpoint(originalRequest.url)) {
                    return Promise.reject(error);
                }

                if (isRefreshing) {
                    // If a refresh is already in progress, queue the request.
                    return new Promise((resolve, reject) => {
                        failedQueue.push({ resolve, reject });
                    }).then(token => {
                        if (token) {
                            originalRequest.headers.Authorization = `Bearer ${token}`;
                            return apiClient(originalRequest);
                        }
                        return Promise.reject(new Error('Token refresh failed'));
                    });
                }

                originalRequest._retry = true;
                isRefreshing = true;

                try {
                    const refreshSuccess = await authStore.refreshTokens(); 

                    if (!refreshSuccess) {
                        // If refresh failed, reject the request and clear the queue
                        processQueue(new Error('Token refresh failed'), null);
                        
                        // Handle connection errors vs auth errors differently
                        if (authStore.connectionError) {
                            // Don't redirect to login on connection errors
                            throw new Error('Connection error during token refresh');
                        } else {
                            // Clear auth state and let the app handle redirect to login
                            authStore.clearAuthState();
                            throw new Error('Authentication failed - please log in again');
                        }
                    }

                    const newToken = authStore.getAccessToken;
                    processQueue(null, newToken);
                    originalRequest.headers.Authorization = `Bearer ${newToken}`;
                    return apiClient(originalRequest);
                } catch (refreshError: any) {
                    processQueue(refreshError, null);
                    throw refreshError;
                } finally {
                    isRefreshing = false;
                }
            }
            
            return Promise.reject(error);
        }
    );
}

export default apiClient;