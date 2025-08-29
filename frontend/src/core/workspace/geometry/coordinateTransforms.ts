import type { Point } from "@/core/geometry/geometry.types";
import { AnnotationType } from "@/core/workspace/annotation.types";
import { clampPointToImageBounds } from './geometry';

/**
 * Update annotation point coordinates locally (without backend save)
 */
export const updateAnnotationPointLocally = (
    annotation: any, 
    handleIndex: number, 
    newPoint: Point,
    imageDimensions?: { width: number; height: number }
): any => {
    // Apply boundary validation if image dimensions are provided
    const validatedPoint = imageDimensions 
        ? clampPointToImageBounds(newPoint, imageDimensions.width, imageDimensions.height)
        : newPoint;
    if (!annotation) return annotation;

    let updatedCoordinates = null;

    if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
        updatedCoordinates = {
            ...annotation.coordinates,
            point: validatedPoint
        };
    }
    else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
        updatedCoordinates = {
            ...annotation.coordinates,
            ...(handleIndex === 0 ? { pointFrom: validatedPoint } : { pointTo: validatedPoint })
        };
    }
    else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
        const { topLeft, bottomRight } = annotation.coordinates;

        if (handleIndex >= 0 && handleIndex < 4) {
            // Update the appropriate corner
            let newTopLeft = topLeft;
            let newBottomRight = bottomRight;

            switch (handleIndex) {
                case 0: // topLeft
                    newTopLeft = validatedPoint;
                    break;
                case 1: // topRight
                    newTopLeft = { x: topLeft.x, y: validatedPoint.y };
                    newBottomRight = { x: validatedPoint.x, y: bottomRight.y };
                    break;
                case 2: // bottomRight
                    newBottomRight = validatedPoint;
                    break;
                case 3: // bottomLeft
                    newTopLeft = { x: validatedPoint.x, y: topLeft.y };
                    newBottomRight = { x: bottomRight.x, y: validatedPoint.y };
                    break;
            }

            updatedCoordinates = {
                ...annotation.coordinates,
                topLeft: newTopLeft,
                bottomRight: newBottomRight
            };
        }
    }
    else if ((annotation.annotationType === AnnotationType.POLYLINE || annotation.annotationType === AnnotationType.POLYGON) 
             && annotation.coordinates?.type === annotation.annotationType) {
        const points = [...annotation.coordinates.points];
        if (handleIndex >= 0 && handleIndex < points.length) {
            points[handleIndex] = validatedPoint;
            updatedCoordinates = {
                ...annotation.coordinates,
                points
            };
        }
    }

    if (updatedCoordinates) {
        // Update the annotation locally only (no backend call)
        annotation.coordinates = updatedCoordinates;
    }

    return annotation;
};