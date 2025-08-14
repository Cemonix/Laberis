import apiClient from '../apiClient';
import { AppLogger } from '@/utils/logger';
import { transformApiError, isValidApiResponse, isValidPaginatedResponse, isValidBlobResponse } from '@/services/utils';
import type { PaginatedResponse } from '@/types/api/paginatedResponse';
import type { QueryParams } from '@/types/api';

/**
 * Base service class providing common functionality for all API services.
 * Includes standardized error handling, logging, response validation, and URL building.
 */
export abstract class BaseService {
    protected readonly logger: ReturnType<typeof AppLogger.createServiceLogger>;
    protected abstract readonly baseUrl: string;

    constructor(serviceName: string) {
        this.logger = AppLogger.createServiceLogger(serviceName);
    }

    /**
     * Builds the complete URL for API endpoints.
     * Can be overridden by services that need custom URL building logic.
     */
    protected getBaseUrl(path: string = ''): string {
        const cleanPath = path.startsWith('/') ? path.slice(1) : path;
        const cleanBaseUrl = this.baseUrl.endsWith('/') ? this.baseUrl.slice(0, -1) : this.baseUrl;
        
        if (!cleanPath) {
            return cleanBaseUrl;
        }
        
        return `${cleanBaseUrl}/${cleanPath}`;
    }

    /**
     * Builds URLs with parameter substitution for RESTful endpoints.
     * Example: buildUrl('/projects/{projectId}/labels/{labelId}', { projectId: 1, labelId: 2 })
     * Returns: '/projects/1/labels/2'
     */
    protected buildUrl(template: string, params: Record<string, string | number> = {}): string {
        let url = template;
        
        Object.entries(params).forEach(([key, value]) => {
            url = url.replace(`{${key}}`, String(value));
        });
        
        return url;
    }

    /**
     * Generic GET request with response validation and error handling
     */
    protected async get<T>(
        url: string, 
        options: { 
            params?: QueryParams;
            responseType?: 'json' | 'blob';
            validateResponse?: boolean;
        } = {}
    ): Promise<T> {
        const { params, responseType = 'json', validateResponse = true } = options;
        
        this.logger.info(`GET request to: ${url}`, params);
        
        try {
            const response = await apiClient.get<T>(url, { 
                params,
                ...(responseType === 'blob' && { responseType: 'blob' })
            });
            
            if (validateResponse) {
                if (responseType === 'blob' && !isValidBlobResponse(response)) {
                    throw new Error('Invalid blob response structure');
                } else if (responseType === 'json' && !isValidApiResponse(response)) {
                    throw new Error('Invalid API response structure');
                }
            }
            
            this.logger.info(`GET request successful: ${url}`);
            return response.data;
        } catch (error) {
            this.logger.error(`GET request failed: ${url}`, error);
            throw transformApiError(error, `Failed to fetch data from ${url}`);
        }
    }

    /**
     * Generic GET request for paginated responses
     */
    protected async getPaginated<T>(
        url: string,
        params: QueryParams = {}
    ): Promise<PaginatedResponse<T>> {
        this.logger.info(`GET paginated request to: ${url}`, params);
        
        try {
            const response = await apiClient.get<PaginatedResponse<T>>(url, { params });
            
            if (!isValidPaginatedResponse(response)) {
                throw new Error('Invalid paginated response structure');
            }
            
            this.logger.info(`GET paginated request successful: ${url}`, {
                currentPage: response.data.currentPage,
                totalPages: response.data.totalPages,
                totalItems: response.data.totalItems
            });
            
            return response.data;
        } catch (error) {
            this.logger.error(`GET paginated request failed: ${url}`, error);
            throw transformApiError(error, `Failed to fetch paginated data from ${url}`);
        }
    }

    /**
     * Generic POST request with response validation and error handling
     */
    protected async post<TRequest, TResponse>(
        url: string,
        data: TRequest,
        validateResponse: boolean = true
    ): Promise<TResponse> {
        this.logger.info(`POST request to: ${url}`, data);
        
        try {
            const response = await apiClient.post<TResponse>(url, data);

            if (validateResponse && !isValidApiResponse(response)) {
                throw new Error('Invalid API response structure');
            }
            
            this.logger.info(`POST request successful: ${url}`);
            return response.data;
        } catch (error) {
            this.logger.error(`POST request failed: ${url}`, error);
            throw transformApiError(error, `Failed to create resource at ${url}`);
        }
    }

    /**
     * Generic PUT request with response validation and error handling
     */
    protected async put<TRequest, TResponse>(
        url: string,
        data: TRequest,
        validateResponse: boolean = true
    ): Promise<TResponse> {
        this.logger.info(`PUT request to: ${url}`, data);
        
        try {
            const response = await apiClient.put<TResponse>(url, data);
            
            if (validateResponse && !isValidApiResponse(response)) {
                throw new Error('Invalid API response structure');
            }
            
            this.logger.info(`PUT request successful: ${url}`);
            return response.data;
        } catch (error) {
            this.logger.error(`PUT request failed: ${url}`, error);
            throw transformApiError(error, `Failed to update resource at ${url}`);
        }
    }

    /**
     * Generic PATCH request with response validation and error handling
     */
    protected async patch<TRequest, TResponse>(
        url: string,
        data: TRequest,
        validateResponse: boolean = true
    ): Promise<TResponse> {
        this.logger.info(`PATCH request to: ${url}`, data);
        
        try {
            const response = await apiClient.patch<TResponse>(url, data);
            
            if (validateResponse && !isValidApiResponse(response)) {
                throw new Error('Invalid API response structure');
            }
            
            this.logger.info(`PATCH request successful: ${url}`);
            return response.data;
        } catch (error) {
            this.logger.error(`PATCH request failed: ${url}`, error);
            throw transformApiError(error, `Failed to update resource at ${url}`);
        }
    }

    /**
     * Generic DELETE request with error handling
     */
    protected async delete(url: string): Promise<void> {
        this.logger.info(`DELETE request to: ${url}`);
        
        try {
            const response = await apiClient.delete(url);
            
            if (response.status !== 204 && response.status !== 200) {
                throw new Error(`Unexpected status code: ${response.status}`);
            }
            
            this.logger.info(`DELETE request successful: ${url}`);
        } catch (error) {
            this.logger.error(`DELETE request failed: ${url}`, error);
            throw transformApiError(error, `Failed to delete resource at ${url}`);
        }
    }
}