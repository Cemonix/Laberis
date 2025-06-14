export interface ApiResponse<T> {
    data: T;
    message?: string;
    success: boolean;
}

export interface PaginatedResponse<T> {
    data: T[];
    totalCount: number;
    pageSize: number;
    currentPage: number;
    totalPages: number;
}

export interface ResponseError {
    message: string;
    code?: string;
    details?: Record<string, any>;
}