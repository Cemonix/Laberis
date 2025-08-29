import { ToolName } from "@/core/workspace/tools.types";

/**
 * Calculate the appropriate cursor style based on current tool and interaction state
 */
export const calculateCanvasCursorStyle = (
    activeTool: ToolName,
    isDraggingHandle: boolean = false,
    isPanning: boolean = false,
    hoveredHandleIndex: number = -1,
    hoveredAnnotationId: number | null = null
): string => {
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
};