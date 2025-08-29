import axios from 'axios';
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { env } from '@/config/env';
import type { useAuthStore } from '@/stores/authStore';
import type { usePermissionStore } from '@/stores/permissionStore';
import { isAuthOrPublicPath, isAuthPath } from '@/router/routes';
import { isPermissionChangingEndpoint, getPermissionChangeDescription } from '@/core/interceptors';
import { AppLogger } from '@/core/logger/logger';

const logger = AppLogger.createServiceLogger('ApiClient');

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
 * Sets up the Axios interceptors for handling authentication and token refresh.
 * This should be called once when the application initializes.
 * @param authStore An instance of the authentication store.
 * @param permissionStore An instance of the permission store.
 */
export function setupInterceptors(
    authStore: ReturnType<typeof useAuthStore>,
    permissionStore: ReturnType<typeof usePermissionStore>
): void {
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

    // Response Interceptor: Handle token expiration and refresh logic, plus automatic permission refresh.
    apiClient.interceptors.response.use(
        async (response: AxiosResponse) => {
            // Check if this was a successful request to a permission-changing endpoint
            const { url, method } = response.config;
            if (response.status >= 200 && response.status < 300 && 
                isPermissionChangingEndpoint(url, method || 'GET')) {
                
                const description = getPermissionChangeDescription(url, method || 'GET');
                logger.info(`Permission-changing request completed: ${method?.toUpperCase()} ${url} - ${description}`);
                
                // Automatically refresh permissions in the background
                try {
                    await permissionStore.refreshPermissions();
                    logger.info('Automatically refreshed permissions after API call');
                } catch (error) {
                    // Don't fail the original request if permission refresh fails
                    logger.warn('Failed to automatically refresh permissions after API call', error);
                }
            }
            
            return response;
        },
        async (error) => {
            const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

            // Check if the error is a 401 and we haven't retried this request yet.
            if (error.response?.status === 401 && !originalRequest._retry) {
                // Skip refresh for auth endpoints and public paths
                const requestUrl = originalRequest.url ?? '';
                if (isAuthOrPublicPath(requestUrl) || requestUrl.startsWith('/auth')) {
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
                    // Determine the context for the token refresh based on current route
                    const currentRoute = window.location.pathname;
                    let context: 'auth-page' | 'public-page' | 'protected-page';
                    
                    if (isAuthOrPublicPath(currentRoute)) {
                        context = isAuthPath(currentRoute) ? 'auth-page' : 'public-page';
                    } else {
                        context = 'protected-page';
                    }

                    const refreshSuccess = await authStore.ensureValidToken(context); 

                    if (!refreshSuccess) {
                        const refreshError = new Error(
                            context === 'protected-page' 
                                ? 'Authentication failed - please log in again' 
                                : 'Token refresh failed'
                        );

                        processQueue(refreshError, null);

                        if (context === 'protected-page') {
                            authStore.clearAuthState();
                        }
                        
                        return Promise.reject(refreshError);
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