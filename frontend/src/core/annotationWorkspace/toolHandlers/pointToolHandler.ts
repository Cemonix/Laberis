import { v4 as uuidv4 } from 'uuid';
import type { Annotation, PointAnnotationData } from '@/types/workspace/annotation';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class PointToolHandler implements ToolHandler {
    onMouseDown(event: MouseEvent, store: WorkspaceStore): void {
        if (store.getSelectedLabelId === null) {
            alert("Please select a label first.");
            return;
        }
        if (store.currentAssetId === null || store.currentTaskId === null) {
            console.error("Cannot create point: Asset ID or Task ID is missing.");
            alert("Error: Asset or Task information is missing.");
            return;
        }

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        const pointCoordinates: PointAnnotationData = {
            type: 'point',
            point: {
                x: imageX,
                y: imageY,
            },
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: 'point',
            labelId: store.getSelectedLabelId,
            coordinates: pointCoordinates,
            assetId: Number(store.currentAssetId),
            taskId: Number(store.currentTaskId),
        };

        store.addAnnotation(newAnnotation);
    }

    onMouseMove(_event: MouseEvent, _store: WorkspaceStore): void {}

    onMouseUp(_event: MouseEvent, _store: WorkspaceStore): void {}

    draw(_ctx: CanvasRenderingContext2D): void {}
}