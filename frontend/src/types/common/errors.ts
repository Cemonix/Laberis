/**
 * Common error classes for the application
 */

export class AppError extends Error {
    constructor(message: string, public readonly cause?: Error) {
        super(message);
        this.name = 'AppError';
        
        // Maintain proper stack trace for where our error was thrown (only available on V8)
        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, AppError);
        }
    }
}

// Network-related errors
export class NetworkError extends AppError {
    constructor(message: string, cause?: Error) {
        super(`Network error: ${message}`, cause);
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
        super(`Server error: ${message}`);
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
