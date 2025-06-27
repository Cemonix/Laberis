import type { ApiError } from "@/types/api/error";
import {
    ServerError,
    NetworkError,
    ValidationError,
    ApiResponseError
} from "@/types/common/errors";

/**
 * Transforms API errors into typed error objects for consistent error handling across all services
 * @param error - The caught error object from an API call
 * @param context - Context string to provide information about where the error occurred (required for better debugging)
 * @returns A properly typed error object that can be handled by the error handling pipeline
 */
export const transformApiError = (error: unknown, context: string): Error => {
    const apiError = error as ApiError;
    
    // Handle network errors (no response received)
    if (apiError.code === 'NETWORK_ERROR' || (apiError.request && !apiError.response)) {
        return new NetworkError(apiError.message || 'Network connection failed');
    }
    
    // Handle server responses
    if (apiError.response) {
        const { status, data } = apiError.response;
        
        // Handle validation errors (400 with validation structure)
        if (status === 400 && data?.errors) {
            const errorMessages = Object.values(data.errors).flat().join(', ');
            return new ValidationError(errorMessages);
        }
        
        // Handle other server errors with proper status codes
        const serverMessage = data?.message || apiError.message || 'Server error occurred';
        return new ServerError(serverMessage, status, data);
    }
    
    // Handle invalid response structure errors
    if (apiError.message?.includes('Invalid response structure')) {
        return new ApiResponseError(apiError.message);
    }
    
    // Fallback for unknown errors
    const errorMessage = `${context}: ${apiError.message || 'Unknown error occurred'}`;
    
    return new Error(errorMessage);
};
