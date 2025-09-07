import { ToolName } from "@/core/workspace/tools.types";

/**
 * CursorManager handles cursor style calculations based on tool state and interaction context.
 * This centralizes cursor logic that was previously scattered across components.
 */
export class CursorManager {
    /**
     * Calculate the appropriate cursor style based on current tool and interaction state
     */
    calculateCanvasCursorStyle(
        activeTool: ToolName,
        isDraggingHandle: boolean = false,
        isPanning: boolean = false,
        hoveredHandleIndex: number = -1,
        hoveredAnnotationId: number | null = null
    ): string {
        switch (activeTool) {
            case ToolName.CURSOR:
                if (isDraggingHandle) return 'grabbing';
                if (isPanning) return 'grabbing';
                if (hoveredHandleIndex >= 0) return 'grab';
                if (hoveredAnnotationId !== null) return 'pointer';
                return 'default';
            
            case ToolName.POINT:
            case ToolName.LINE:
            case ToolName.POLYLINE:
            case ToolName.BOUNDING_BOX:
            case ToolName.POLYGON:
                return 'crosshair';
            
            default:
                return 'default';
        }
    }

    /**
     * Get cursor style for a specific tool (without interaction context)
     */
    getToolCursor(toolName: ToolName): string {
        return this.calculateCanvasCursorStyle(toolName);
    }

    /**
     * Check if cursor should change on hover for given tool
     */
    shouldShowHoverCursor(toolName: ToolName): boolean {
        return toolName === ToolName.CURSOR;
    }

    /**
     * Get cursor for dragging operations
     */
    getDragCursor(): string {
        return 'grabbing';
    }

    /**
     * Get cursor for hover operations  
     */
    getHoverCursor(): string {
        return 'grab';
    }

    /**
     * Get default cursor
     */
    getDefaultCursor(): string {
        return 'default';
    }

    /**
     * Get crosshair cursor for drawing tools
     */
    getCrosshairCursor(): string {
        return 'crosshair';
    }

    /**
     * Get pointer cursor for clickable elements
     */
    getPointerCursor(): string {
        return 'pointer';
    }
}

// Export singleton instance
export const cursorManager = new CursorManager();