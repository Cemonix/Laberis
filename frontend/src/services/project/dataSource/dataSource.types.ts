import type { QueryParams } from '@/services/base/requests';

export enum DataSourceType {
    MINIO_BUCKET = 'MINIO_BUCKET',
    S3_BUCKET = 'S3_BUCKET',
    GSC_BUCKET = 'GSC_BUCKET',
    AZURE_BLOB_STORAGE = 'AZURE_BLOB_STORAGE',
    LOCAL_DIRECTORY = 'LOCAL_DIRECTORY',
    DATABASE = 'DATABASE',
    API = 'API',
    OTHER = 'OTHER',
}

export enum DataSourceStatus {
    ACTIVE = 'ACTIVE',
    INACTIVE = 'INACTIVE',
    SYNCING = 'SYNCING',
    ERROR = 'ERROR',
    ARCHIVED = 'ARCHIVED',
}

export interface DataSource {
    id: number;
    name: string;
    description?: string;
    sourceType: DataSourceType;
    status: DataSourceStatus;
    isDefault: boolean;
    createdAt: string;
    projectId: number;
    assetCount?: number;
}

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

export interface DataSourceListParams extends QueryParams {
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