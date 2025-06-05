// --- Specific Data Structures for the 'coordinates' field ---
export interface PointAnnotationData {
    type: 'point';
    x: number;
    y: number;
}

export interface BoundingBoxAnnotationData {
    type: 'bounding_box';
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface PolylineAnnotationData {
    type: 'polyline';
    points: number[]; // [x1, y1, x2, y2, ..., xn, yn]
}

export interface PolygonAnnotationData {
    type: 'polygon';
    points: number[];
}

export interface TextAnnotationData {
    type: 'text';
    text: string;
    x?: number;
    y?: number;
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
export type AnnotationTypeValue = 'bounding_box' | 'polygon' | 'polyline' | 'point' | 'text';

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