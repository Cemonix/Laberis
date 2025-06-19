import type { DataSourceType, DataSourceStatus } from './dataSource';
import type { BaseListParams } from '../api';

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

export interface DataSourceListParams extends BaseListParams {
    projectId?: number;
    status?: DataSourceStatus;
    sourceType?: DataSourceType;
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
