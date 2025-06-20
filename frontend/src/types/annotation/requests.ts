/**
 * Request parameter types for annotation API endpoints
 */

export interface AnnotationListParams {
    filterOn?: string;
    filterQuery?: string;
    sortBy?: string;
    isAscending?: boolean;
    pageNumber?: number;
    pageSize?: number;
}

export interface CreateAnnotationRequest {
    annotationType: string;
    data: string;
    isPrediction?: boolean;
    confidenceScore?: number;
    isGroundTruth?: boolean;
    version?: number;
    notes?: string;
    taskId: number;
    assetId: number;
    labelId: number;
    annotatorUserId?: string;
    parentAnnotationId?: number;
}

export interface UpdateAnnotationRequest {
    annotationType?: string;
    data?: string;
    isPrediction?: boolean;
    confidenceScore?: number;
    isGroundTruth?: boolean;
    notes?: string;
    labelId?: number;
}
