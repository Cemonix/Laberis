import { faArrowPointer, faDotCircle, faMinus, faWaveSquare, faSquare, faDrawPolygon } from '@fortawesome/free-solid-svg-icons';
import { ToolName, type Tool } from "@/core/workspace/tools.types";
import { cursorManager } from './cursorManager';

/**
 * ToolManager handles tool selection and validation.
 * This is a utility class that doesn't require reactivity.
 */
export class ToolManager {
    private readonly availableTools: Tool[] = [
        { id: ToolName.CURSOR, name: 'Cursor', iconDefinition: faArrowPointer },
        { id: ToolName.POINT, name: 'Point', iconDefinition: faDotCircle },
        { id: ToolName.LINE, name: 'Line', iconDefinition: faMinus },
        { id: ToolName.BOUNDING_BOX, name: 'Bounding Box', iconDefinition: faSquare },
        { id: ToolName.POLYLINE, name: 'Polyline', iconDefinition: faWaveSquare },
        { id: ToolName.POLYGON, name: 'Polygon', iconDefinition: faDrawPolygon },
    ];

    /**
     * Get all available tools
     */
    getAvailableTools(): Tool[] {
        return [...this.availableTools];
    }

    /**
     * Get tool details by ID
     */
    getToolById(toolId: ToolName): Tool | undefined {
        return this.availableTools.find((tool: Tool) => tool.id === toolId);
    }

    /**
     * Validate if a tool exists
     */
    isValidTool(toolId: ToolName): boolean {
        return this.availableTools.some((tool: Tool) => tool.id === toolId);
    }

    /**
     * Get default tool
     */
    getDefaultTool(): ToolName {
        return ToolName.CURSOR;
    }

    /**
     * Get tool name by ID
     */
    getToolName(toolId: ToolName): string {
        const tool = this.getToolById(toolId);
        return tool?.name || 'Unknown Tool';
    }

    /**
     * Check if tool is annotation tool (not cursor)
     */
    isAnnotationTool(toolId: ToolName): boolean {
        return toolId !== ToolName.CURSOR;
    }

    /**
     * Get annotation tools only
     */
    getAnnotationTools(): Tool[] {
        return this.availableTools.filter(tool => this.isAnnotationTool(tool.id));
    }

    /**
     * Get UI tools only (cursor)
     */
    getUITools(): Tool[] {
        return this.availableTools.filter(tool => !this.isAnnotationTool(tool.id));
    }

    /**
     * Validate and set active tool (returns valid tool or default)
     */
    validateAndSetTool(requestedTool: ToolName): ToolName {
        if (this.isValidTool(requestedTool)) {
            return requestedTool;
        }
        return this.getDefaultTool();
    }

    /**
     * Get cursor style for the given tool
     */
    getToolCursor(toolName: ToolName): string {
        return cursorManager.getToolCursor(toolName);
    }

    /**
     * Calculate cursor style based on tool and interaction state
     */
    calculateCursorStyle(
        activeTool: ToolName,
        isDraggingHandle?: boolean,
        isPanning?: boolean,
        hoveredHandleIndex?: number,
        hoveredAnnotationId?: number | null
    ): string {
        return cursorManager.calculateCanvasCursorStyle(
            activeTool,
            isDraggingHandle,
            isPanning,
            hoveredHandleIndex,
            hoveredAnnotationId
        );
    }

    /**
     * Check if tool should show hover cursor
     */
    shouldShowHoverCursor(toolName: ToolName): boolean {
        return cursorManager.shouldShowHoverCursor(toolName);
    }
}

// Export singleton instance
export const toolManager = new ToolManager();