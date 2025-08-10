import { AnnotationType } from "@/types/workspace/annotation";
import { drawBoundingBox, drawPoint, drawLine, drawPolyline, drawPolygon, drawEditHandle } from './annotationDrawer';

export interface AnnotationRenderContext {
    context: CanvasRenderingContext2D;
    annotation: any;
    color: string;
    baseColor: string;
    isSelected: boolean;
    zoomLevel: number;
    hoveredHandleIndex: number;
}

/**
 * Calculate zoom-adjusted sizes for rendering
 */
export const calculateRenderSizes = (zoomLevel: number) => {
    const baseLineWidth = 2;
    const baseThinLineWidth = 1;
    const basePointRadius = 4;
    const baseHandleSize = 8;
    
    // For zoom levels > 1 (zoomed in), keep thickness constant in screen pixels
    // For zoom levels < 1 (zoomed out), scale thickness proportionally but with reasonable limits
    const scaleFactor = zoomLevel > 1 ? 1 / zoomLevel : Math.min(1 / zoomLevel, 2);
    
    return {
        pointRadius: basePointRadius * scaleFactor,
        lineWidth: Math.max(baseLineWidth * scaleFactor, 1),
        thinLineWidth: Math.max(baseThinLineWidth * scaleFactor, 0.5),
        handleSize: Math.max(baseHandleSize * scaleFactor, 4)
    };
};

/**
 * Draw edit handles for points array
 */
const drawPointArrayHandles = (
    context: CanvasRenderingContext2D,
    points: { x: number; y: number }[],
    handleSize: number,
    baseColor: string,
    hoveredHandleIndex: number
) => {
    points.forEach((point, index) => {
        drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex === index);
    });
};

/**
 * Draw edit handles for bounding box corners
 */
const drawBoundingBoxHandles = (
    context: CanvasRenderingContext2D,
    topLeft: { x: number; y: number },
    bottomRight: { x: number; y: number },
    handleSize: number,
    baseColor: string,
    hoveredHandleIndex: number
) => {
    const corners = [
        topLeft,
        { x: bottomRight.x, y: topLeft.y },
        bottomRight,
        { x: topLeft.x, y: bottomRight.y }
    ];
    
    corners.forEach((corner, index) => {
        drawEditHandle(context, corner.x, corner.y, handleSize, baseColor, hoveredHandleIndex === index);
    });
};

/**
 * Render a point annotation
 */
export const renderPointAnnotation = (renderContext: AnnotationRenderContext) => {
    const { context, annotation, color, baseColor, isSelected, zoomLevel, hoveredHandleIndex } = renderContext;
    
    if (annotation.annotationType !== AnnotationType.POINT || annotation.coordinates?.type !== AnnotationType.POINT) {
        return;
    }

    const { pointRadius, thinLineWidth, handleSize } = calculateRenderSizes(zoomLevel);
    const { point } = annotation.coordinates;

    drawPoint(context, point.x, point.y, color, pointRadius, thinLineWidth);

    if (isSelected) {
        drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex === 0);
    }
};

/**
 * Render a bounding box annotation
 */
export const renderBoundingBoxAnnotation = (renderContext: AnnotationRenderContext) => {
    const { context, annotation, color, baseColor, isSelected, zoomLevel, hoveredHandleIndex } = renderContext;
    
    if (annotation.annotationType !== AnnotationType.BOUNDING_BOX || annotation.coordinates?.type !== AnnotationType.BOUNDING_BOX) {
        return;
    }

    const { handleSize } = calculateRenderSizes(zoomLevel);
    const { topLeft, bottomRight } = annotation.coordinates;
    const width = bottomRight.x - topLeft.x;
    const height = bottomRight.y - topLeft.y;

    drawBoundingBox(context, topLeft.x, topLeft.y, width, height, color);

    if (isSelected) {
        drawBoundingBoxHandles(context, topLeft, bottomRight, handleSize, baseColor, hoveredHandleIndex);
    }
};

/**
 * Render a line annotation
 */
export const renderLineAnnotation = (renderContext: AnnotationRenderContext) => {
    const { context, annotation, color, baseColor, isSelected, zoomLevel, hoveredHandleIndex } = renderContext;
    
    if (annotation.annotationType !== AnnotationType.LINE || annotation.coordinates?.type !== AnnotationType.LINE) {
        return;
    }

    const { lineWidth, handleSize } = calculateRenderSizes(zoomLevel);
    const { pointFrom, pointTo } = annotation.coordinates;

    drawLine(context, pointFrom, pointTo, color, lineWidth);

    if (isSelected) {
        drawEditHandle(context, pointFrom.x, pointFrom.y, handleSize, baseColor, hoveredHandleIndex === 0);
        drawEditHandle(context, pointTo.x, pointTo.y, handleSize, baseColor, hoveredHandleIndex === 1);
    }
};

/**
 * Render a polyline annotation
 */
export const renderPolylineAnnotation = (renderContext: AnnotationRenderContext) => {
    const { context, annotation, color, baseColor, isSelected, zoomLevel, hoveredHandleIndex } = renderContext;
    
    if (annotation.annotationType !== AnnotationType.POLYLINE || annotation.coordinates?.type !== AnnotationType.POLYLINE) {
        return;
    }

    const { lineWidth, handleSize } = calculateRenderSizes(zoomLevel);
    const { points } = annotation.coordinates;

    drawPolyline(context, points, color, lineWidth);

    if (isSelected) {
        drawPointArrayHandles(context, points, handleSize, baseColor, hoveredHandleIndex);
    }
};

/**
 * Render a polygon annotation
 */
export const renderPolygonAnnotation = (renderContext: AnnotationRenderContext) => {
    const { context, annotation, color, baseColor, isSelected, zoomLevel, hoveredHandleIndex } = renderContext;
    
    if (annotation.annotationType !== AnnotationType.POLYGON || annotation.coordinates?.type !== AnnotationType.POLYGON) {
        return;
    }

    const { lineWidth, handleSize } = calculateRenderSizes(zoomLevel);
    const { points } = annotation.coordinates;

    drawPolygon(context, points, color, lineWidth);

    if (isSelected) {
        drawPointArrayHandles(context, points, handleSize, baseColor, hoveredHandleIndex);
    }
};

/**
 * Render any annotation type using the appropriate renderer
 */
export const renderAnnotation = (renderContext: AnnotationRenderContext) => {
    const { annotation } = renderContext;

    switch (annotation.annotationType) {
        case AnnotationType.POINT:
            renderPointAnnotation(renderContext);
            break;
        case AnnotationType.BOUNDING_BOX:
            renderBoundingBoxAnnotation(renderContext);
            break;
        case AnnotationType.LINE:
            renderLineAnnotation(renderContext);
            break;
        case AnnotationType.POLYLINE:
            renderPolylineAnnotation(renderContext);
            break;
        case AnnotationType.POLYGON:
            renderPolygonAnnotation(renderContext);
            break;
    }
};