// TODO: Properly implement according to the backend API
export enum DataSourceType {
    S3_COMPATIBLE = 's3_compatible',
    LOCAL_STORAGE = 'local_storage',
    DATABASE = 'database',
}

export interface DataSource {
    dataSourceId: number;
    name: string;
    description?: string;
    type: DataSourceType;
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