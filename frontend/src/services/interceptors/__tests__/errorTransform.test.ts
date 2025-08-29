
import { describe, it, expect } from 'vitest';
import { transformApiError } from '../errorTransform';
import {
    ServerError,
    NetworkError,
    ValidationError,
    ApiResponseError,
    UnauthorizedError
} from '@/core/errors/errors';
import type { ApiError } from '@/services/base';

describe('transformApiError', () => {
    const context = 'TestContext';

    it('should handle network errors when response is missing', () => {
        const apiError: Partial<ApiError> = {
            request: {},
            message: 'Network Error'
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(NetworkError);
        expect(error.message).toBe('Network Error');
    });

    it('should handle network errors with NETWORK_ERROR code', () => {
        const apiError: Partial<ApiError> = {
            code: 'NETWORK_ERROR',
            message: 'Connection failed'
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(NetworkError);
        expect(error.message).toBe('Connection failed');
    });

    it('should handle unauthorized errors (401)', () => {
        const apiError: Partial<ApiError> = {
            response: {
                status: 401,
                data: { message: 'Auth failed' },
                statusText: 'Unauthorized',
                headers: {},
                config: { headers: {} } as any
            }
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(UnauthorizedError);
        expect(error.message).toBe('Auth failed');
    });

    it('should handle validation errors (400 with errors object)', () => {
        const apiError: Partial<ApiError> = {
            response: {
                status: 400,
                data: {
                    message: 'Validation failed',
                    errors: {
                        field1: ['error1'],
                        field2: ['error2']
                    }
                },
                statusText: 'Bad Request',
                headers: {},
                config: {} as any
            }
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(ValidationError);
        expect(error.message).toBe('error1, error2');
    });

    it('should handle generic server errors', () => {
        const apiError: Partial<ApiError> = {
            response: {
                status: 500,
                data: { message: 'Internal Server Error' },
                statusText: 'Internal Server Error',
                headers: {},
                config: {} as any
            }
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(ServerError);
        expect(error.message).toBe('Internal Server Error');
        if (error instanceof ServerError) {
            expect(error.statusCode).toBe(500);
        }
    });

    it('should handle invalid response structure errors', () => {
        const apiError: Partial<ApiError> = {
            message: 'Invalid response structure'
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(ApiResponseError);
        expect(error.message).toBe('Invalid response structure');
    });

    it('should handle unknown errors as a fallback', () => {
        const apiError: Partial<ApiError> = {
            message: 'An unknown error'
        };
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(Error);
        expect(error.message).toBe(`${context}: An unknown error`);
    });

    it('should handle unknown errors with no message', () => {
        const apiError = {};
        const error = transformApiError(apiError, context);
        expect(error).toBeInstanceOf(Error);
        expect(error.message).toBe(`${context}: Unknown error occurred`);
    });
});
