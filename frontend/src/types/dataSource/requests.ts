import type { DataSourceType, DataSourceStatus } from './dataSource';

export interface CreateDataSourceRequest {
    name: string;
    description?: string;
    sourceType: DataSourceType;
    connectionDetails?: string;
}

export interface UpdateDataSourceRequest {
    name: string;
    description?: string;
    status: DataSourceStatus;
    connectionDetails?: string;
}

export interface DataSourceListParams {
    projectId: number;
    page?: number;
    pageSize?: number;
    status?: DataSourceStatus;
    sourceType?: DataSourceType;
    search?: string;
    sortBy?: 'name' | 'createdAt' | 'status';
    sortOrder?: 'asc' | 'desc';
}

export interface DataSourceStatsResponse {
    totalDataSources: number;
    activeDataSources: number;
    totalAssets: number;
    syncingDataSources: number;
}

export interface AvailableDataSourceTypesResponse {
    availableTypes: DataSourceType[];
}
