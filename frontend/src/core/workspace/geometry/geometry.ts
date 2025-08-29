import type { Point } from "@/core/geometry/geometry.types";

/**
 * Check if a point is near another point (for handle detection)
 */
export const isPointNearHandle = (
    clickPoint: Point, 
    handlePoint: Point, 
    threshold: number = 10,
    zoomLevel: number = 1
): boolean => {
    const distance = Math.sqrt(
        Math.pow(clickPoint.x - handlePoint.x, 2) + 
        Math.pow(clickPoint.y - handlePoint.y, 2)
    );
    return distance <= threshold / zoomLevel;
};

/**
 * Check if a point is near a line segment
 */
export const isPointNearLine = (
    point: Point, 
    lineStart: Point, 
    lineEnd: Point, 
    threshold: number = 10,
    zoomLevel: number = 1
): boolean => {
    const adjustedThreshold = threshold / zoomLevel;
    
    // Calculate distance from point to line segment
    const A = point.x - lineStart.x;
    const B = point.y - lineStart.y;
    const C = lineEnd.x - lineStart.x;
    const D = lineEnd.y - lineStart.y;

    const dot = A * C + B * D;
    const lenSq = C * C + D * D;
    
    if (lenSq === 0) {
        // Line start and end are the same point
        return isPointNearHandle(point, lineStart, threshold, zoomLevel);
    }
    
    let param = dot / lenSq;
    param = Math.max(0, Math.min(1, param)); // Clamp to line segment
    
    const xx = lineStart.x + param * C;
    const yy = lineStart.y + param * D;
    
    const dx = point.x - xx;
    const dy = point.y - yy;
    const distance = Math.sqrt(dx * dx + dy * dy);
    
    return distance <= adjustedThreshold;
};

/**
 * Check if a point is inside a bounding box
 */
export const isPointInBoundingBox = (point: Point, topLeft: Point, bottomRight: Point): boolean => {
    return point.x >= topLeft.x && point.x <= bottomRight.x &&
           point.y >= topLeft.y && point.y <= bottomRight.y;
};

/**
 * Check if a point is near a polyline
 */
export const isPointNearPolyline = (
    point: Point, 
    points: Point[], 
    threshold: number = 10,
    zoomLevel: number = 1
): boolean => {
    if (points.length < 2) return false;
    
    for (let i = 0; i < points.length - 1; i++) {
        if (isPointNearLine(point, points[i], points[i + 1], threshold, zoomLevel)) {
            return true;
        }
    }
    return false;
};

/**
 * Check if a point is inside a polygon using ray casting algorithm
 */
export const isPointInPolygon = (point: Point, points: Point[]): boolean => {
    if (points.length < 3) return false;
    
    let inside = false;
    for (let i = 0, j = points.length - 1; i < points.length; j = i++) {
        if (((points[i].y > point.y) !== (points[j].y > point.y)) &&
            (point.x < (points[j].x - points[i].x) * (point.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)) {
            inside = !inside;
        }
    }
    return inside;
};

/**
 * Clamp a point to the boundaries of an image (teleport to border when out of bounds)
 */
export const clampPointToImageBounds = (
    point: Point,
    imageWidth: number,
    imageHeight: number
): Point => {
    return {
        x: Math.max(0, Math.min(imageWidth, point.x)),
        y: Math.max(0, Math.min(imageHeight, point.y))
    };
};

/**
 * Check if a point is within image boundaries
 */
export const isPointInImageBounds = (
    point: Point,
    imageWidth: number,
    imageHeight: number
): boolean => {
    return point.x >= 0 && point.x <= imageWidth && 
           point.y >= 0 && point.y <= imageHeight;
};