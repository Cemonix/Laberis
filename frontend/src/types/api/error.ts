import type { AxiosError } from 'axios';

export interface ApiErrorData {
    message: string;
    errors?: Record<string, string[]>;
    stackTrace?: string;
}

export interface ApiError extends AxiosError<ApiErrorData> {}
