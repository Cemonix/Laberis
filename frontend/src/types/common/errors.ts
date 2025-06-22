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

abstract class AlertError extends AppError {
    alertTitle: string;

    constructor(message: string, alertTitle: string, cause?: Error) {
        super(message, cause);
        this.alertTitle = alertTitle;
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

// Authentication and authorization errors
// TODO: Implement specific auth errors if needed

// Store-related errors
export class StoreError extends AlertError {
    constructor(message: string, public readonly action?: string) {
        super(message, 'Store Error');
        this.name = 'StoreError';
    }
}

export class ToolError extends AlertError {
    constructor(message: string, public readonly toolName?: string) {
        super(message, 'Tool Error');
        this.name = 'ToolError';
    }
}
