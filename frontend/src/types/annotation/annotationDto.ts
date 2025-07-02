export interface AnnotationDto {
    id: number;
    annotationType: string;
    data: string; // JSON string containing coordinates
    isPrediction: boolean;
    confidenceScore?: number;
    isGroundTruth: boolean;
    version: number;
    notes?: string;
    createdAt: string;
    updatedAt: string;
    taskId: number;
    assetId: number;
    labelId: number;
    annotatorEmail: string;
    parentAnnotationId?: number;
}

export interface CreateAnnotationDto {
    annotationType: string;
    data: string; // JSON string containing coordinates
    isPrediction?: boolean;
    confidenceScore?: number;
    isGroundTruth?: boolean;
    version?: number;
    notes?: string;
    taskId: number;
    assetId: number;
    labelId: number;
    annotatorEmail?: string;
    parentAnnotationId?: number;
}

export interface UpdateAnnotationDto {
    annotationType?: string;
    data?: string;
    isPrediction?: boolean;
    confidenceScore?: number;
    isGroundTruth?: boolean;
    notes?: string;
    labelId?: number;
}
