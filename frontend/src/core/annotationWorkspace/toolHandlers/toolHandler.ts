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
     * Draws any temporary, in-progress visuals for the tool.
     * For example, a bounding box being dragged.
     * @param ctx The 2D rendering context of the canvas.
     */
    draw(ctx: CanvasRenderingContext2D): void;
}