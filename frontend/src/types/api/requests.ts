/**
 * Common pagination parameters used across API endpoints
 */
export interface PaginationParams {
    pageNumber?: number;
    pageSize?: number;
}

/**
 * Common filtering parameters used across API endpoints
 */
export interface FilterParams {
    filterOn?: string;
    filterQuery?: string;
}

/**
 * Common sorting parameters used across API endpoints
 */
export interface SortParams {
    sortBy?: string;
    isAscending?: boolean;
}

/**
 * Common date range filtering parameters
 */
export interface DateRangeParams {
    dateFrom?: string;
    dateTo?: string;
}

/**
 * Combined base query parameters that most list endpoints support
 */
export interface QueryParams extends PaginationParams, FilterParams, SortParams {}

/**
 * Utility function to build URLSearchParams from API request parameters
 * Automatically handles common parameter types and converts values to strings
 * @param params - The parameters object to convert
 * @param excludeKeys - Keys to exclude from the query string (e.g., 'projectId' for path params)
 * @returns URLSearchParams object ready for use in API calls
 */
export function buildQueryParams<T extends Record<string, any>>(
    params: T, 
    excludeKeys: (keyof T)[] = []
): URLSearchParams {
    const queryParams = new URLSearchParams();
    
    Object.entries(params).forEach(([key, value]) => {
        if (excludeKeys.includes(key as keyof T) || value === undefined || value === null) {
            return;
        }
        
        if (typeof value === 'boolean') {
            queryParams.append(key, value.toString());
        } else if (typeof value === 'number') {
            queryParams.append(key, value.toString());
        } else if (typeof value === 'string' && value.length > 0) {
            queryParams.append(key, value);
        }
    });
    
    return queryParams;
}
