import type { useWorkspaceStore } from '@/stores/workspaceStore';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export interface ToolHandler {
    /**
     * Handles the mouse down event on the canvas.
     * @param event The native MouseEvent.
     * @param store The Pinia workspace store instance.
     */
    onMouseDown(event: MouseEvent, store: WorkspaceStore): void;

    /**
     * Handles the mouse move event on the canvas.
     * @param event The native MouseEvent.
     * @param store The Pinia workspace store instance.
     */
    onMouseMove(event: MouseEvent, store: WorkspaceStore): void;

    /**
     * Handles the mouse up event on the canvas.
     * @param event The native MouseEvent.
     * @param store The Pinia workspace store instance.
     */
    onMouseUp(event: MouseEvent, store: WorkspaceStore): void;

    /**
     * Handles the mouse leave event on the canvas.
     * @param event The native MouseEvent.
     * @param store The Pinia workspace store instance.
     */
    onMouseLeave?(event: MouseEvent, store: WorkspaceStore): void; // Optional

    /**
     * Handles keyboard events (like Enter to finish annotation).
     * @param event The native KeyboardEvent.
     * @param store The Pinia workspace store instance.
     */
    onKeyDown?(event: KeyboardEvent, store: WorkspaceStore): void; // Optional

    /**
     * Draws any temporary, in-progress visuals for the tool.
     * For example, a bounding box being dragged.
     * @param ctx The 2D rendering context of the canvas.
     * @param zoomLevel The current zoom level for proper scaling.
     */
    draw(ctx: CanvasRenderingContext2D, zoomLevel?: number): void;

    /**
     * Returns whether the tool is currently in drawing mode.
     * @returns true if the tool is actively drawing/creating an annotation
     */
    isDrawing(): boolean;
}