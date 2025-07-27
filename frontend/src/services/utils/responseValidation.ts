/**
 * Utility functions for validating API responses across all services
 */

/**
 * Validates that an API response has the expected structure
 * @param response - The API response to validate
 * @param expectedDataType - 'object' for single objects, 'array' for arrays
 * @returns true if valid, false otherwise
 */
export const isValidApiResponse = (response: any, expectedDataType: 'object' | 'array' | 'blob' = 'object'): boolean => {
    if (!response?.data) return false;

    else if (expectedDataType === 'blob') {
        return response.data instanceof Blob;
    }
    else if (expectedDataType === 'array') {
        return Array.isArray(response.data);
    }

    return typeof response.data === 'object' && response.data !== null;
};

/**
 * Validates that a paginated API response has the expected structure
 * @param response - The paginated API response to validate
 * @returns true if valid, false otherwise
 */
export const isValidPaginatedResponse = (response: any): boolean => {
    return response?.data?.data && Array.isArray(response.data.data);
};

/**
 * Validates that an API response contains data and has the expected shape
 * @param response - The API response to validate
 * @param validator - Optional custom validation function
 * @returns true if valid, false otherwise
 */
export const isValidResponseWithCustomValidation = (
    response: any, 
    validator?: (data: any) => boolean
): boolean => {
    if (!isValidApiResponse(response)) return false;
    
    if (validator) {
        return validator(response.data);
    }
    
    return true;
};

/**
 * Validates that a response contains valid blob data
 * @param response - The API response to validate
 * @returns true if valid, false otherwise
 */
export const isValidBlobResponse = (response: any): boolean => {
    return response && response.data && response.data instanceof Blob;
};
