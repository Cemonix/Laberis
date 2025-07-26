import { v4 as uuidv4 } from 'uuid';
import type { Annotation, BoundingBoxAnnotationData } from '@/types/workspace/annotation';
import { AnnotationType } from '@/types/workspace/annotation';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { StoreError, ToolError } from '@/types/common/errors';
import { drawBoundingBox } from '@/core/annotationWorkspace/annotationDrawer';
import type { Point } from '@/types/common/point';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class BoundingBoxToolHandler implements ToolHandler {
    private drawing = false;
    private startPoint: Point | null = null;
    private currentPoint: Point | null = null;

    onMouseDown(event: MouseEvent, store: WorkspaceStore): void {
        if (store.getSelectedLabelId === null) {
            throw new ToolError("Cannot create bounding box: No label is selected.");
        }
        
        if (store.currentAssetId === null) {
            throw new StoreError("Cannot create bounding box: Asset ID is missing.");
        }
        if (store.currentTaskId === null) {
            throw new StoreError("Cannot create bounding box: Task ID is missing.");
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

        // Calculate bounding box dimensions
        const width = Math.abs(endPoint.x - this.startPoint.x);
        const height = Math.abs(endPoint.y - this.startPoint.y);

        // Only create annotation if there's meaningful area
        if (width < 5 || height < 5) { // Minimum size threshold
            this.reset();
            return;
        }

        // Determine top-left and bottom-right points
        const topLeft: Point = {
            x: Math.min(this.startPoint.x, endPoint.x),
            y: Math.min(this.startPoint.y, endPoint.y)
        };
        const bottomRight: Point = {
            x: Math.max(this.startPoint.x, endPoint.x),
            y: Math.max(this.startPoint.y, endPoint.y)
        };

        const boundingBoxCoordinates: BoundingBoxAnnotationData = {
            type: AnnotationType.BOUNDING_BOX,
            topLeft,
            bottomRight,
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: AnnotationType.BOUNDING_BOX,
            labelId: store.getSelectedLabelId!,
            coordinates: boundingBoxCoordinates,
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

        // Calculate the current bounding box for preview
        const x = Math.min(this.startPoint.x, this.currentPoint.x);
        const y = Math.min(this.startPoint.y, this.currentPoint.y);
        const width = Math.abs(this.currentPoint.x - this.startPoint.x);
        const height = Math.abs(this.currentPoint.y - this.startPoint.y);

        drawBoundingBox(ctx, x, y, width, height, '#00FFFF', 2);
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