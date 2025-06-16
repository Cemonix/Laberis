export interface PaginatedResponse<T> {
    data: T[];
    currentPage: number;
    pageSize: number;
    totalPages: number;
    totalItems: number; 
}
