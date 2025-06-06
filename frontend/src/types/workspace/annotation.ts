import type { Point } from "@/types/common/point";

// --- Specific Data Structures for the 'coordinates' field ---
export interface PointAnnotationData {
    type: 'point';
    point: Point;
}

export interface LineAnnotationData {
    type: 'line';
    pointFrom: Point;
    pointTo: Point;
}

export interface BoundingBoxAnnotationData {
    type: 'bounding_box';
    topLeft: Point;
    bottomRight: Point;
}

export interface PolylineAnnotationData {
    type: 'polyline';
    points: Point[];
}

export interface PolygonAnnotationData {
    type: 'polygon';
    points: Point[];
}

export interface TextAnnotationData {
    type: 'text';
    text: string;
    point?: Point;
    width?: number;
    height?: number;
}

export type AnnotationCoordinates = 
    | BoundingBoxAnnotationData 
    | PointAnnotationData 
    | PolylineAnnotationData 
    | PolygonAnnotationData
    | TextAnnotationData;

// --- Main Annotation Interface ---
export type AnnotationTypeValue = 'bounding_box' | 'polygon' | 'polyline' | 'line' | 'point' | 'text';

export interface Annotation {
    clientId?: string;
    annotationId?: number;
    annotationType: AnnotationTypeValue;
    labelId: number;
    coordinates: AnnotationCoordinates; 
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
    annotatorUserId?: string;
}