/**
 * Common error classes for the application
 */

export class AppError extends Error {
    constructor(message: string, public readonly cause?: Error) {
        super(message);
        this.name = 'AppError';
        
        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, AppError);
        }
    }
}

abstract class AlertError extends AppError {
    alertTitle: string;

    constructor(message: string, alertTitle: string, cause?: Error) {
        super(message, cause);
        this.alertTitle = alertTitle;
    }
}

export class UserVisibleError extends AlertError {
    constructor(title: string, message: string, cause?: Error) {
        super(message, title, cause);
        this.name = 'UserVisibleError';
    }
}

// Network-related errors
export class NetworkError extends AppError {
    constructor(message: string = 'A network error occurred. Please check your connection.', cause?: Error) {
        super(message, cause);
        this.name = 'NetworkError';
    }
}

// API-related errors
export class ApiResponseError extends AppError {
    constructor(message: string = 'Invalid response structure from API') {
        super(message);
        this.name = 'ApiResponseError';
    }
}

export class ServerError extends AppError {
    constructor(
        message: string, 
        public readonly statusCode?: number,
        public readonly serverResponse?: any
    ) {
        super(message);
        this.name = 'ServerError';
    }
}

// Validation errors
export class ValidationError extends AppError {
    constructor(message: string, public readonly field?: string) {
        super(message);
        this.name = 'ValidationError';
    }
}

// Store-related errors
export class StoreError extends AlertError {
    constructor(message: string, public readonly action?: string) {
        super(message, 'Store Error');
        this.name = 'StoreError';
    }
}

// Tool-specific errors in the workspace
export class ToolError extends AlertError {
    constructor(message: string, public readonly toolName?: string) {
        super(message, 'Tool Error');
        this.name = 'ToolError';
    }
}

// Type guards for error classification
/**
 * Type guard to check if an error is an API response error
 */
export const isApiResponseError = (error: unknown): error is ApiResponseError => {
    return error instanceof ApiResponseError;
};

/**
 * Type guard to check if an error is a validation error
 */
export const isValidationError = (error: unknown): error is ValidationError => {
    return error instanceof ValidationError;
};

/**
 * Type guard to check if an error is a server error
 */
export const isServerError = (error: unknown): error is ServerError => {
    return error instanceof ServerError;
};

/**
 * Type guard to check if an error is a network error
 */
export const isNetworkError = (error: unknown): error is NetworkError => {
    return error instanceof NetworkError;
};

/**
 * Type guard to check if an error is a user visible error
 */
export const isUserVisibleError = (error: unknown): error is UserVisibleError => {
    return error instanceof UserVisibleError;
};

/**
 * Type guard to check if an error is a tool error
 */
export const isToolError = (error: unknown): error is ToolError => {
    return error instanceof ToolError;
};