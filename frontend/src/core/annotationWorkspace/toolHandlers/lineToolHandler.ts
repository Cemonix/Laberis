import { v4 as uuidv4 } from 'uuid';
import type { Annotation, LineAnnotationData } from '@/types/workspace/annotation';
import { AnnotationType } from '@/types/workspace/annotation';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { StoreError, ToolError } from '@/types/common/errors';
import { drawLine } from '@/core/annotationWorkspace/annotationDrawer';
import type { Point } from '@/types/common/point';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class LineToolHandler implements ToolHandler {
    private drawing = false;
    private startPoint: Point | null = null;
    private currentPoint: Point | null = null;

    onMouseDown(event: MouseEvent, store: WorkspaceStore): void {
        if (store.getSelectedLabelId === null) {
            throw new ToolError("Cannot create line: No label is selected.");
        }
        
        if (store.currentAssetId === null) {
            throw new StoreError("Cannot create line: Asset ID is missing.");
        }
        if (store.currentTaskId === null) {
            throw new StoreError("Cannot create line: Task ID is missing.");
        }

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        this.startPoint = { x: imageX, y: imageY };
        this.currentPoint = { x: imageX, y: imageY };
        this.drawing = true;
    }

    onMouseMove(event: MouseEvent, store: WorkspaceStore): void {
        if (!this.drawing || !this.startPoint) return;

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        this.currentPoint = { x: imageX, y: imageY };
    }

    onMouseUp(event: MouseEvent, store: WorkspaceStore): void {
        if (!this.drawing || !this.startPoint || !this.currentPoint) return;

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        const endPoint = { x: imageX, y: imageY };

        // Only create annotation if there's meaningful distance
        const distance = Math.sqrt(
            Math.pow(endPoint.x - this.startPoint.x, 2) + 
            Math.pow(endPoint.y - this.startPoint.y, 2)
        );
        
        if (distance < 5) { // Minimum distance threshold
            this.reset();
            return;
        }

        const lineCoordinates: LineAnnotationData = {
            type: AnnotationType.LINE,
            pointFrom: this.startPoint,
            pointTo: endPoint,
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: AnnotationType.LINE,
            labelId: store.getSelectedLabelId!,
            coordinates: lineCoordinates,
            assetId: Number(store.currentAssetId),
            taskId: Number(store.currentTaskId),
        };

        store.addAnnotation(newAnnotation);
        this.reset();
    }

    onMouseLeave(_event: MouseEvent, _store: WorkspaceStore): void {
        this.reset();
    }

    draw(ctx: CanvasRenderingContext2D): void {
        if (!this.drawing || !this.startPoint || !this.currentPoint) return;

        drawLine(ctx, this.startPoint, this.currentPoint, '#00FFFF', 2);
    }

    private reset(): void {
        this.drawing = false;
        this.startPoint = null;
        this.currentPoint = null;
    }

    isDrawing(): boolean {
        return this.drawing;
    }
}