export interface AssetDto {
    id: number;
    externalId: string;
    filename: string;
    mimeType: string;
    sizeBytes: number;
    width?: number;
    height?: number;
    status: string;
    createdAt: string;
    updatedAt: string;
    projectId: number;
    dataSourceId: number;
}

export interface UploadResult {
    asset?: AssetDto;
    filename: string;
    success: boolean;
    errorMessage?: string;
    errorType?: string;
}

export interface BulkUploadResult {
    results: UploadResult[];
    totalFiles: number;
    successfulUploads: number;
    failedUploads: number;
    allSuccessful: boolean;
    summary: string;
}

export interface UploadProgress {
    loaded: number;
    total: number;
    percentage: number;
}

export type ProgressCallback = (progress: number) => void;
