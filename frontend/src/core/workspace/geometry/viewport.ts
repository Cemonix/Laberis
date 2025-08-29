import type { Point } from "@/core/geometry/geometry.types";

export interface CanvasDisplaySettings {
    width: number;
    height: number;
}

export interface ImageDimensions {
    width: number;
    height: number;
}

export interface ViewState {
    offset: Point;
    zoomLevel: number;
}

/**
 * Calculate canvas dimensions based on available space and image size
 */
export const calculateCanvasDimensions = (
    availableWidth: number,
    availableHeight: number,
    imageNaturalDimensions?: ImageDimensions
): CanvasDisplaySettings => {
    if (availableWidth === 0 || availableHeight === 0) {
        return { width: 0, height: 0 };
    }

    let targetCanvasWidth = availableWidth;
    let targetCanvasHeight = availableHeight;

    if (imageNaturalDimensions && 
        imageNaturalDimensions.width > 0 && 
        imageNaturalDimensions.height > 0) {
        
        const imgWidth = imageNaturalDimensions.width;
        const imgHeight = imageNaturalDimensions.height;

        // Calculate scale to fit image within available space
        const scaleX = availableWidth / imgWidth;
        const scaleY = availableHeight / imgHeight;
        const scale = Math.min(scaleX, scaleY);

        targetCanvasWidth = Math.round(imgWidth * scale);
        targetCanvasHeight = Math.round(imgHeight * scale);
    }

    return { width: targetCanvasWidth, height: targetCanvasHeight };
};

/**
 * Calculate view state to center and fit image within canvas
 */
export const calculateCenterAndFitView = (
    canvasWidth: number,
    canvasHeight: number,
    imageNaturalDimensions?: ImageDimensions
): ViewState => {
    if (!imageNaturalDimensions || 
        imageNaturalDimensions.width === 0 || 
        imageNaturalDimensions.height === 0 ||
        canvasWidth === 0 || 
        canvasHeight === 0) {
        return { offset: { x: 0, y: 0 }, zoomLevel: 1.0 };
    }
    
    const { width: imgWidth, height: imgHeight } = imageNaturalDimensions;
    
    // Calculate scale to fit image within canvas
    const scaleX = canvasWidth / imgWidth;
    const scaleY = canvasHeight / imgHeight;
    const newZoom = Math.min(scaleX, scaleY);

    // Calculate offset to center the image
    const newOffsetX = (canvasWidth - imgWidth * newZoom) / 2;
    const newOffsetY = (canvasHeight - imgHeight * newZoom) / 2;

    return {
        offset: { x: newOffsetX, y: newOffsetY },
        zoomLevel: newZoom
    };
};

/**
 * Convert mouse coordinates to image coordinates
 */
export const mouseToImageCoordinates = (
    mouseX: number,
    mouseY: number,
    viewOffset: Point,
    zoomLevel: number
): Point => {
    const imageX = (mouseX - viewOffset.x) / zoomLevel;
    const imageY = (mouseY - viewOffset.y) / zoomLevel;
    return { x: imageX, y: imageY };
};

/**
 * Calculate zoom-adjusted zoom level for wheel events
 */
export const calculateZoomFromWheel = (
    currentZoom: number,
    wheelDelta: number,
    zoomSensitivity: number
): number => {
    const delta = wheelDelta * -1; // Invert for natural zoom
    const zoomFactor = Math.exp(delta * zoomSensitivity);
    return currentZoom * zoomFactor;
};

/**
 * Calculate new view offset to keep a specific image point under the mouse during zoom
 */
export const calculateZoomViewOffset = (
    mouseX: number,
    mouseY: number,
    imagePointX: number,
    imagePointY: number,
    newZoomLevel: number
): Point => {
    const newViewOffsetX = mouseX - imagePointX * newZoomLevel;
    const newViewOffsetY = mouseY - imagePointY * newZoomLevel;
    return { x: newViewOffsetX, y: newViewOffsetY };
};