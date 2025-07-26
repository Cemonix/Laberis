import type { Point } from "@/types/common/point";

// --- Annotation Type Enum ---
export enum AnnotationType {
    POINT = 'point',
    LINE = 'line',
    BOUNDING_BOX = 'bounding_box',
    POLYLINE = 'polyline',
    POLYGON = 'polygon',
    TEXT = 'text'
}

// --- Specific Data Structures for the 'coordinates' field ---
export interface PointAnnotationData {
    type: AnnotationType.POINT;
    point: Point;
}

export interface LineAnnotationData {
    type: AnnotationType.LINE;
    pointFrom: Point;
    pointTo: Point;
}

export interface BoundingBoxAnnotationData {
    type: AnnotationType.BOUNDING_BOX;
    topLeft: Point;
    bottomRight: Point;
}

export interface PolylineAnnotationData {
    type: AnnotationType.POLYLINE;
    points: Point[];
}

export interface PolygonAnnotationData {
    type: AnnotationType.POLYGON;
    points: Point[];
}

export interface TextAnnotationData {
    type: AnnotationType.TEXT;
    text: string;
    point?: Point;
    width?: number;
    height?: number;
}

export type AnnotationCoordinates = 
    | BoundingBoxAnnotationData 
    | PointAnnotationData 
    | LineAnnotationData
    | PolylineAnnotationData 
    | PolygonAnnotationData
    | TextAnnotationData;

// --- Main Annotation Interface ---
export interface Annotation {
    // Frontend-specific fields
    clientId?: string;
    
    // Backend fields (matches AnnotationDto)
    annotationId?: number;
    annotationType: AnnotationType;
    data?: string;
    coordinates?: AnnotationCoordinates;
    labelId: number;
    assetId: number;
    taskId: number;
    notes?: string;
    confidenceScore?: number;
    parentAnnotationId?: number;
    isGroundTruth?: boolean;
    isPrediction?: boolean;
    version?: number;
    createdAt?: string;
    updatedAt?: string;
    annotatorEmail?: string;
}

export interface AnnotationDto {
    id: number;
    annotationType: string;
    data: string;
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
    annotationType: AnnotationType;
    data: string;
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
    annotationType?: AnnotationType;
    data?: string;
    isPrediction?: boolean;
    confidenceScore?: number;
    isGroundTruth?: boolean;
    notes?: string;
    labelId?: number;
}