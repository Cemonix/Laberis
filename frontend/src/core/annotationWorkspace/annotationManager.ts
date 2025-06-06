import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { ToolName } from '@/types/workspace/tools';
import type { ToolHandler } from '@/core/annotationWorkspace/toolHandlers/toolHandler';
import { PointToolHandler } from '@/core/annotationWorkspace/toolHandlers/pointToolHandler';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class AnnotationManager {
    private store: WorkspaceStore;
    private toolHandlers: Map<ToolName, ToolHandler>;

    constructor(store: WorkspaceStore) {
        this.store = store;
        this.toolHandlers = new Map();

        this.toolHandlers.set(ToolName.POINT, new PointToolHandler());
        // this.toolHandlers.set(ToolName.BOUNDING_BOX, new BoundingBoxToolHandler()); // Example for later
        // Add other handlers here...
    }

    private getActiveHandler(): ToolHandler | undefined {
        const activeTool = this.store.activeTool;
        return this.toolHandlers.get(activeTool);
    }

    public onMouseDown(event: MouseEvent): void {
        this.getActiveHandler()?.onMouseDown(event, this.store);
    }

    public onMouseMove(event: MouseEvent): void {
        this.getActiveHandler()?.onMouseMove(event, this.store);
    }

    public onMouseUp(event: MouseEvent): void {
        this.getActiveHandler()?.onMouseUp(event, this.store);
    }

    public onMouseLeave(event: MouseEvent): void {
        this.getActiveHandler()?.onMouseLeave?.(event, this.store);
    }

    public draw(ctx: CanvasRenderingContext2D): void {
        this.getActiveHandler()?.draw(ctx);
    }
}