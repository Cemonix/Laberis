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
    dataSourceId: number;
    name: string;
    description?: string;
    type: DataSourceType;
    status: DataSourceStatus;
    assetCount: number;
    projectId: number;
    createdAt: string;
    updatedAt: string;
}

export interface FormPayloadDataSource {
    name: string;
    description?: string;
    type: DataSourceType;
}