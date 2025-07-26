import { v4 as uuidv4 } from 'uuid';
import type { Annotation, PolylineAnnotationData } from '@/types/workspace/annotation';
import { AnnotationType } from '@/types/workspace/annotation';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { StoreError, ToolError } from '@/types/common/errors';
import { drawPolyline, drawPoint } from '@/core/annotationWorkspace/annotationDrawer';
import type { Point } from '@/types/common/point';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class PolylineToolHandler implements ToolHandler {
    private drawing = false;
    private points: Point[] = [];
    private currentPoint: Point | null = null;
    private speedMode = false;
    private speedModeInterval: number | null = null;
    private speedModeDistance = 0;
    private imageDimensions: { width: number; height: number } | null = null;

    onMouseDown(event: MouseEvent, store: WorkspaceStore): void {
        if (store.getSelectedLabelId === null) {
            throw new ToolError("Cannot create polyline: No label is selected.");
        }
        
        if (store.currentAssetId === null) {
            throw new StoreError("Cannot create polyline: Asset ID is missing.");
        }
        if (store.currentTaskId === null) {
            throw new StoreError("Cannot create polyline: Task ID is missing.");
        }

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        const clickPoint = { x: imageX, y: imageY };

        // Set image dimensions for speed mode calculations
        if (store.getCurrentAsset && store.getCurrentAsset) {
            const asset = store.getCurrentAsset;
            this.imageDimensions = { width: asset.width, height: asset.height };
        }

        // Add the point to the polyline
        this.points.push(clickPoint);
        this.currentPoint = clickPoint;
        this.drawing = true;

        // Check if speed mode should be activated
        if (event.shiftKey && this.points.length === 1) {
            this.activateSpeedMode();
        }
    }

    onMouseMove(event: MouseEvent, _store: WorkspaceStore): void {
        if (!this.drawing) return;

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - _store.viewOffset.x) / _store.zoomLevel;
        const imageY = (canvasY - _store.viewOffset.y) / _store.zoomLevel;

        this.currentPoint = { x: imageX, y: imageY };

        // Check for shift key to activate/deactivate speed mode
        if (event.shiftKey && !this.speedMode && this.points.length >= 1) {
            this.activateSpeedMode();
        } else if (!event.shiftKey && this.speedMode) {
            this.deactivateSpeedMode();
        }
    }

    onMouseUp(_event: MouseEvent, _store: WorkspaceStore): void {
        // For polyline, we continue on mouse up (unlike other tools)
        // The polyline is finished when double-clicking or clicking near the first point
    }

    onMouseLeave(_event: MouseEvent, _store: WorkspaceStore): void {
        // Don't auto-finish on mouse leave, user should use Enter key
        // Just reset if not enough points
        if (this.points.length < 2) {
            this.reset();
        }
    }

    onKeyDown(event: KeyboardEvent, store: WorkspaceStore): void {
        if (event.key === 'Enter' && this.drawing && this.points.length >= 2) {
            this.finishPolyline(store);
        } else if (event.key === 'Escape') {
            this.reset();
        } else if (event.key === 'Shift' && this.drawing && this.points.length >= 1) {
            // Toggle speed mode
            if (this.speedMode) {
                this.deactivateSpeedMode();
            } else {
                this.activateSpeedMode();
            }
        }
    }

    draw(ctx: CanvasRenderingContext2D): void {
        if (!this.drawing || this.points.length === 0) return;

        // Draw the existing points of the polyline
        if (this.points.length >= 1) {
            drawPolyline(ctx, this.points, '#00FFFF', 2);
        }

        // Draw points for visual feedback
        this.points.forEach((point, index) => {
            // Make the first point slightly larger to indicate start
            const radius = index === 0 ? 6 : 4;
            const color = index === 0 ? '#FF0000' : '#00FFFF';
            drawPoint(ctx, point.x, point.y, color, radius, 2, '#FFFFFF');
        });

        // Draw the current line segment being created
        if (this.currentPoint && this.points.length > 0) {
            const lastPoint = this.points[this.points.length - 1];
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(lastPoint.x, lastPoint.y);
            ctx.lineTo(this.currentPoint.x, this.currentPoint.y);
            ctx.lineWidth = 2;
            ctx.strokeStyle = '#00FFFF';
            ctx.stroke();
            ctx.restore();
        }
    }

    private finishPolyline(store: WorkspaceStore): void {
        if (this.points.length < 2) {
            this.reset();
            return;
        }

        const polylineCoordinates: PolylineAnnotationData = {
            type: AnnotationType.POLYLINE,
            points: [...this.points], // Create a copy of the points array
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: AnnotationType.POLYLINE,
            labelId: store.getSelectedLabelId!,
            coordinates: polylineCoordinates,
            assetId: Number(store.currentAssetId),
            taskId: Number(store.currentTaskId),
        };

        store.addAnnotation(newAnnotation);
        this.reset();
    }

    private reset(): void {
        this.drawing = false;
        this.points = [];
        this.currentPoint = null;
        this.deactivateSpeedMode();
    }

    private activateSpeedMode(): void {
        if (!this.imageDimensions) return;
        
        this.speedMode = true;
        // Calculate distance as 2% of image diagonal
        const diagonal = Math.sqrt(
            this.imageDimensions.width * this.imageDimensions.width +
            this.imageDimensions.height * this.imageDimensions.height
        );
        this.speedModeDistance = diagonal * 0.02;
        
        // Start adding points automatically every 100ms
        this.speedModeInterval = window.setInterval(() => {
            this.addSpeedModePoint();
        }, 100);
    }

    private deactivateSpeedMode(): void {
        this.speedMode = false;
        if (this.speedModeInterval !== null) {
            clearInterval(this.speedModeInterval);
            this.speedModeInterval = null;
        }
    }

    private addSpeedModePoint(): void {
        if (!this.currentPoint || this.points.length === 0) return;
        
        const lastPoint = this.points[this.points.length - 1];
        const distance = Math.sqrt(
            Math.pow(this.currentPoint.x - lastPoint.x, 2) +
            Math.pow(this.currentPoint.y - lastPoint.y, 2)
        );
        
        // Only add point if we've moved the minimum distance
        if (distance >= this.speedModeDistance) {
            this.points.push({ ...this.currentPoint });
        }
    }

    isDrawing(): boolean {
        return this.drawing;
    }
}