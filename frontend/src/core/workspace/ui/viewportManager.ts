import type { Point } from "@/core/geometry/geometry.types";
import type { ImageDimensions } from "@/core/asset/asset.types";

/**
 * Viewport configuration constants
 */
export const VIEWPORT_CONFIG = {
    MIN_ZOOM: 0.1,
    MAX_ZOOM: 10.0,
    ZOOM_SENSITIVITY: 0.005,
    DEFAULT_ZOOM: 1.0,
    DEFAULT_OFFSET: { x: 0, y: 0 }
} as const;

/**
 * Viewport state interface
 */
export interface ViewportState {
    viewOffset: Point;
    zoomLevel: number;
}

/**
 * Zoom configuration
 */
export interface ZoomConfig {
    minZoom: number;
    maxZoom: number;
    zoomSensitivity: number;
}

/**
 * ViewportManager handles all viewport-related operations including zoom and pan.
 * This is a utility class that doesn't require reactivity.
 */
export class ViewportManager {
    private readonly config = VIEWPORT_CONFIG;

    /**
     * Get zoom configuration
     */
    getZoomConfig(): ZoomConfig {
        return {
            minZoom: this.config.MIN_ZOOM,
            maxZoom: this.config.MAX_ZOOM,
            zoomSensitivity: this.config.ZOOM_SENSITIVITY,
        };
    }

    /**
     * Get initial viewport state
     */
    getInitialViewportState(): ViewportState {
        return {
            viewOffset: { ...this.config.DEFAULT_OFFSET },
            zoomLevel: this.config.DEFAULT_ZOOM
        };
    }

    /**
     * Clamp zoom level to valid range
     */
    clampZoom(level: number): number {
        return Math.max(this.config.MIN_ZOOM, Math.min(level, this.config.MAX_ZOOM));
    }

    /**
     * Validate and clamp viewport offset
     */
    clampOffset(offset: Point): Point {
        // TODO: For now, we don't clamp the offset to allow free panning
        // This could be extended to clamp to image boundaries if needed
        return { ...offset };
    }

    /**
     * Calculate zoom level for fit-to-screen
     */
    calculateFitToScreenZoom(
        imageNaturalDimensions: ImageDimensions,
        canvasDisplayDimensions: ImageDimensions
    ): number {
        if (!imageNaturalDimensions || !canvasDisplayDimensions) {
            return this.config.DEFAULT_ZOOM;
        }

        const scaleX = canvasDisplayDimensions.width / imageNaturalDimensions.width;
        const scaleY = canvasDisplayDimensions.height / imageNaturalDimensions.height;
        
        // Use the smaller scale to ensure the entire image fits
        const fitZoom = Math.min(scaleX, scaleY);
        
        return this.clampZoom(fitZoom);
    }

    /**
     * Calculate image aspect ratio
     */
    calculateImageAspectRatio(imageNaturalDimensions: ImageDimensions | null): number | null {
        if (!imageNaturalDimensions) {
            return null;
        }
        return imageNaturalDimensions.width / imageNaturalDimensions.height;
    }

    /**
     * Apply zoom delta while preserving center point
     */
    applyZoomDelta(
        currentZoom: number,
        zoomDelta: number,
        center?: Point
    ): { zoomLevel: number; viewOffset?: Point } {
        const newZoom = this.clampZoom(currentZoom + zoomDelta);
        
        // If no center point provided, just return the new zoom level
        if (!center) {
            return { zoomLevel: newZoom };
        }

        // Calculate offset adjustment to maintain center point
        // TODO: This would require current viewOffset and canvas dimensions to be fully implemented
        // For now, return just the zoom level
        return { zoomLevel: newZoom };
    }

    /**
     * Reset viewport to initial state
     */
    resetViewport(): ViewportState {
        return this.getInitialViewportState();
    }

    /**
     * Center image in viewport
     */
    centerImage(
        imageNaturalDimensions: ImageDimensions,
        canvasDisplayDimensions: ImageDimensions,
        currentZoom: number
    ): Point {
        if (!imageNaturalDimensions || !canvasDisplayDimensions) {
            return { ...this.config.DEFAULT_OFFSET };
        }

        // Calculate the scaled image dimensions
        const scaledImageWidth = imageNaturalDimensions.width * currentZoom;
        const scaledImageHeight = imageNaturalDimensions.height * currentZoom;

        // Calculate centering offset
        const offsetX = (canvasDisplayDimensions.width - scaledImageWidth) / 2;
        const offsetY = (canvasDisplayDimensions.height - scaledImageHeight) / 2;

        return this.clampOffset({ x: offsetX, y: offsetY });
    }

    /**
     * Apply pan delta to current offset
     */
    applyPanDelta(currentOffset: Point, deltaX: number, deltaY: number): Point {
        return this.clampOffset({
            x: currentOffset.x + deltaX,
            y: currentOffset.y + deltaY
        });
    }

    /**
     * Convert screen coordinates to image coordinates
     */
    screenToImageCoordinates(
        screenPoint: Point,
        viewOffset: Point,
        zoomLevel: number
    ): Point {
        return {
            x: (screenPoint.x - viewOffset.x) / zoomLevel,
            y: (screenPoint.y - viewOffset.y) / zoomLevel
        };
    }

    /**
     * Convert image coordinates to screen coordinates
     */
    imageToScreenCoordinates(
        imagePoint: Point,
        viewOffset: Point,
        zoomLevel: number
    ): Point {
        return {
            x: imagePoint.x * zoomLevel + viewOffset.x,
            y: imagePoint.y * zoomLevel + viewOffset.y
        };
    }

    /**
     * Check if viewport state is valid
     */
    isValidViewportState(state: Partial<ViewportState>): boolean {
        if (state.zoomLevel !== undefined) {
            if (state.zoomLevel < this.config.MIN_ZOOM || state.zoomLevel > this.config.MAX_ZOOM) {
                return false;
            }
        }
        
        // TODO: ViewOffset is always valid for now
        return true;
    }

    /**
     * Get viewport bounds in image coordinates
     */
    getViewportBounds(
        viewOffset: Point,
        zoomLevel: number,
        canvasDisplayDimensions: ImageDimensions
    ): { topLeft: Point; bottomRight: Point } {
        const topLeft = this.screenToImageCoordinates(
            { x: 0, y: 0 },
            viewOffset,
            zoomLevel
        );
        
        const bottomRight = this.screenToImageCoordinates(
            { x: canvasDisplayDimensions.width, y: canvasDisplayDimensions.height },
            viewOffset,
            zoomLevel
        );

        return { topLeft, bottomRight };
    }
}

// Export singleton instance
export const viewportManager = new ViewportManager();