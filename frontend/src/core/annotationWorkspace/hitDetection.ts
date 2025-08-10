import type { Point } from "@/types/common/point";
import { AnnotationType } from "@/types/workspace/annotation";
import {
    isPointNearHandle,
    isPointNearLine,
    isPointInBoundingBox,
    isPointNearPolyline,
    isPointInPolygon
} from "./geometry";

export interface AnnotationHitResult {
    annotation: any;
    handleIndex: number;
}

/**
 * Check if a point hits a specific annotation
 */
export const checkAnnotationHit = (
    annotation: any, 
    imagePoint: Point, 
    isSelected: boolean = false,
    zoomLevel: number = 1
): AnnotationHitResult | null => {
    // Check if clicking on a handle (only for selected annotation)
    if (isSelected) {
        if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
            if (isPointNearHandle(imagePoint, annotation.coordinates.point, 10, zoomLevel)) {
                return { annotation, handleIndex: 0 };
            }
        }
        else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
            if (isPointNearHandle(imagePoint, annotation.coordinates.pointFrom, 10, zoomLevel)) {
                return { annotation, handleIndex: 0 };
            }
            if (isPointNearHandle(imagePoint, annotation.coordinates.pointTo, 10, zoomLevel)) {
                return { annotation, handleIndex: 1 };
            }
        }
        else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
            const { topLeft, bottomRight } = annotation.coordinates;
            const corners = [
                topLeft,
                { x: bottomRight.x, y: topLeft.y },
                bottomRight,
                { x: topLeft.x, y: bottomRight.y }
            ];
            
            for (let j = 0; j < corners.length; j++) {
                if (isPointNearHandle(imagePoint, corners[j], 10, zoomLevel)) {
                    return { annotation, handleIndex: j };
                }
            }
        }
        else if ((annotation.annotationType === AnnotationType.POLYLINE || annotation.annotationType === AnnotationType.POLYGON) 
                 && annotation.coordinates?.type === annotation.annotationType) {
            const points = annotation.coordinates.points;
            for (let j = 0; j < points.length; j++) {
                if (isPointNearHandle(imagePoint, points[j], 10, zoomLevel)) {
                    return { annotation, handleIndex: j };
                }
            }
        }
    }
    
    // Check if clicking on the annotation itself (for selection)
    if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
        if (isPointNearHandle(imagePoint, annotation.coordinates.point, 15, zoomLevel)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
        const { pointFrom, pointTo } = annotation.coordinates;
        if (isPointNearLine(imagePoint, pointFrom, pointTo, 10, zoomLevel)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
        const { topLeft, bottomRight } = annotation.coordinates;
        if (isPointInBoundingBox(imagePoint, topLeft, bottomRight)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.POLYLINE && annotation.coordinates?.type === AnnotationType.POLYLINE) {
        const { points } = annotation.coordinates;
        if (isPointNearPolyline(imagePoint, points, 10, zoomLevel)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.POLYGON && annotation.coordinates?.type === AnnotationType.POLYGON) {
        const { points } = annotation.coordinates;
        if (isPointInPolygon(imagePoint, points) || isPointNearPolyline(imagePoint, points, 10, zoomLevel)) {
            return { annotation, handleIndex: -1 };
        }
    }
    
    return null;
};

/**
 * Find annotation at a specific point, prioritizing selected annotation
 */
export const findAnnotationAtPoint = (
    imagePoint: Point,
    annotations: any[],
    selectedAnnotationId: number | null,
    zoomLevel: number = 1
): AnnotationHitResult | null => {
    if (!annotations) return null;

    // First check if we're clicking on the selected annotation (highest priority)
    const selectedAnnotation = annotations.find(a => a.annotationId === selectedAnnotationId);
    if (selectedAnnotation) {
        const result = checkAnnotationHit(selectedAnnotation, imagePoint, true, zoomLevel);
        if (result) return result;
    }

    // Then check other annotations in reverse order to prioritize top annotations
    for (let i = annotations.length - 1; i >= 0; i--) {
        const annotation = annotations[i];
        
        // Skip selected annotation as we already checked it
        if (annotation.annotationId === selectedAnnotationId) continue;
        
        // Check this annotation for hits
        const result = checkAnnotationHit(annotation, imagePoint, false, zoomLevel);
        if (result) return result;
    }
    
    return null;
};