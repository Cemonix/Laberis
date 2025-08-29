export interface ImageDimensions {
    width: number;
    height: number;
}

export enum AssetStatus {
    PENDING_IMPORT = 'PENDING_IMPORT',
    IMPORTED = 'IMPORTED',
    IMPORT_ERROR = 'IMPORT_ERROR',
    PENDING_PROCESSING = 'PENDING_PROCESSING',
    PROCESSING = 'PROCESSING',
    PROCESSING_ERROR = 'PROCESSING_ERROR',
    READY_FOR_ANNOTATION = 'READY_FOR_ANNOTATION',
    ANNOTATION_IN_PROGRESS = 'ANNOTATION_IN_PROGRESS',
    ANNOTATED = 'ANNOTATED',
    PENDING_REVIEW = 'PENDING_REVIEW',
    REVIEW_IN_PROGRESS = 'REVIEW_IN_PROGRESS',
    REVIEW_ACCEPTED = 'REVIEW_ACCEPTED',
    REVIEW_REJECTED = 'REVIEW_REJECTED',
    EXPORTED = 'EXPORTED',
    ARCHIVED = 'ARCHIVED',
}

export interface Asset {
    id: number;
    filename: string;
    height: number;
    width: number;
    mimeType: string;
    sizeBytes: number;
    durationMs: number;
    status: AssetStatus;
    createdAt: string;
    updatedAt: string;
    imageUrl?: string;
}

/**
 * Result of transforming an asset for workspace use
 */
export interface WorkspaceAsset {
    assetData: Asset;
    imageUrl: string | null;
    naturalDimensions: ImageDimensions | null;
}

/**
 * Result of asset loading operation
 */
export type AssetLoadResult = {
    success: true;
    asset: Asset;
    imageUrl: string | null;
    naturalDimensions: ImageDimensions | null;
} | {
    success: false;
    error: string;
}

/**
 * Result of validation operations
 */
export interface AssetValidationResult {
    isValid: boolean;
    errors: string[];
}
