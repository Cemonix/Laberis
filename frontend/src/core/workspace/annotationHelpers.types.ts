import type { Annotation, BoundingBoxAnnotationData, PointAnnotationData, PolygonAnnotationData, PolylineAnnotationData, LineAnnotationData } from './annotation.types';
import { AnnotationType } from './annotation.types';

/**
 * Proper typed annotation interfaces that match actual annotation coordinate structures
 */
export interface BoundingBoxAnnotation extends Omit<Annotation, 'coordinates'> {
    annotationType: AnnotationType.BOUNDING_BOX;
    coordinates: BoundingBoxAnnotationData;
}

export interface PointAnnotation extends Omit<Annotation, 'coordinates'> {
    annotationType: AnnotationType.POINT;
    coordinates: PointAnnotationData;
}

export interface PolygonAnnotation extends Omit<Annotation, 'coordinates'> {
    annotationType: AnnotationType.POLYGON;
    coordinates: PolygonAnnotationData;
}

export interface PolylineAnnotation extends Omit<Annotation, 'coordinates'> {
    annotationType: AnnotationType.POLYLINE;
    coordinates: PolylineAnnotationData;
}

export interface LineAnnotation extends Omit<Annotation, 'coordinates'> {
    annotationType: AnnotationType.LINE;
    coordinates: LineAnnotationData;
}

/**
 * Union type for all typed annotations
 */
export type TypedAnnotation = 
    | BoundingBoxAnnotation 
    | PointAnnotation 
    | PolygonAnnotation 
    | PolylineAnnotation 
    | LineAnnotation;

/**
 * Clean type guard functions using discriminated unions
 */
export const AnnotationTypeGuards = {
    isBoundingBox(annotation: Annotation): annotation is BoundingBoxAnnotation {
        return annotation.annotationType === AnnotationType.BOUNDING_BOX && 
               annotation.coordinates !== undefined &&
               annotation.coordinates.type === AnnotationType.BOUNDING_BOX;
    },

    isPoint(annotation: Annotation): annotation is PointAnnotation {
        return annotation.annotationType === AnnotationType.POINT && 
               annotation.coordinates !== undefined &&
               annotation.coordinates.type === AnnotationType.POINT;
    },

    isPolygon(annotation: Annotation): annotation is PolygonAnnotation {
        return annotation.annotationType === AnnotationType.POLYGON && 
               annotation.coordinates !== undefined &&
               annotation.coordinates.type === AnnotationType.POLYGON;
    },

    isPolyline(annotation: Annotation): annotation is PolylineAnnotation {
        return annotation.annotationType === AnnotationType.POLYLINE && 
               annotation.coordinates !== undefined &&
               annotation.coordinates.type === AnnotationType.POLYLINE;
    },

    isLine(annotation: Annotation): annotation is LineAnnotation {
        return annotation.annotationType === AnnotationType.LINE && 
               annotation.coordinates !== undefined &&
               annotation.coordinates.type === AnnotationType.LINE;
    },

    /**
     * Checks if annotation has multi-point coordinates (polygon or polyline)
     */
    hasPoints(annotation: Annotation): annotation is PolygonAnnotation | PolylineAnnotation {
        return AnnotationTypeGuards.isPolygon(annotation) || AnnotationTypeGuards.isPolyline(annotation);
    }
} as const;

/**
 * Helper functions for working with annotation coordinates
 */
export const AnnotationHelpers = {
    /**
     * Get points array for polygon/polyline annotations
     */
    getPoints(annotation: PolygonAnnotation | PolylineAnnotation): Array<{ x: number; y: number }> {
        return annotation.coordinates.points;
    },

    /**
     * Get formatted points string for SVG
     */
    getPointsString(annotation: PolygonAnnotation | PolylineAnnotation): string {
        return annotation.coordinates.points
            .map(point => `${point.x},${point.y}`)
            .join(' ');
    },

    /**
     * Get scaled points string for thumbnails
     */
    getScaledPointsString(
        annotation: PolygonAnnotation | PolylineAnnotation, 
        originalWidth: number | undefined, 
        originalHeight: number | undefined, 
        targetSize: number
    ): string {
        if (!originalWidth || !originalHeight) return '';
        
        return annotation.coordinates.points
            .map(point => {
                const scaledX = (point.x / originalWidth) * targetSize;
                const scaledY = (point.y / originalHeight) * targetSize;
                return `${scaledX},${scaledY}`;
            })
            .join(' ');
    },

    /**
     * Scale a single coordinate
     */
    scaleCoordinate(coordinate: number, originalSize: number | undefined, targetSize: number): number {
        if (!originalSize || originalSize === 0) return coordinate;
        return (coordinate / originalSize) * targetSize;
    }
} as const;