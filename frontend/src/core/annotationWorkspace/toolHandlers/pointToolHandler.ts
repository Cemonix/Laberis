import { v4 as uuidv4 } from 'uuid';
import type { Annotation, PointAnnotationData } from '@/types/workspace/annotation';
import { AnnotationType } from '@/types/workspace/annotation';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { StoreError, ToolError } from '@/types/common/errors';
import { clampPointToImageBounds } from '@/core/annotationWorkspace/geometry';
import type { Point } from '@/types/common/point';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class PointToolHandler implements ToolHandler {
    private getImageDimensions(store: WorkspaceStore): { width: number; height: number } | null {
        const asset = store.getCurrentAsset;
        if (!asset || !asset.width || !asset.height) return null;
        return { width: asset.width, height: asset.height };
    }

    private validatePoint(point: Point, store: WorkspaceStore): Point {
        const imageDims = this.getImageDimensions(store);
        if (!imageDims) return point;
        return clampPointToImageBounds(point, imageDims.width, imageDims.height);
    }

    onMouseDown(event: MouseEvent, store: WorkspaceStore): void {
        if (store.getSelectedLabelId === null) {
            throw new ToolError("Cannot create point: No label is selected.");
        }
        
        if (store.currentAssetId === null) {
            throw new StoreError("Cannot create point: Asset ID is missing.");
        }
        if (store.currentTaskId === null) {
            throw new StoreError("Cannot create point: Task ID is missing.");
        }


        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        const validatedPoint = this.validatePoint({ x: imageX, y: imageY }, store);
        const pointCoordinates: PointAnnotationData = {
            type: AnnotationType.POINT,
            point: validatedPoint,
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: AnnotationType.POINT,
            labelId: store.getSelectedLabelId,
            coordinates: pointCoordinates,
            assetId: Number(store.currentAssetId),
            taskId: Number(store.currentTaskId),
        };

        store.addAnnotation(newAnnotation);
    }

    onMouseMove(_event: MouseEvent, _store: WorkspaceStore): void {}

    onMouseUp(_event: MouseEvent, _store: WorkspaceStore): void {}

    draw(_ctx: CanvasRenderingContext2D, _zoomLevel?: number): void {}

    isDrawing(): boolean {
        return false; // Point tool doesn't have a drawing state
    }
}