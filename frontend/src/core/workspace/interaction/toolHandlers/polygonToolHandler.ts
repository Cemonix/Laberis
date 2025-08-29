import { v4 as uuidv4 } from 'uuid';
import type { Annotation, PolygonAnnotationData } from '@/core/workspace/annotation.types';
import { AnnotationType } from '@/core/workspace/annotation.types';
import type { ToolHandler } from './toolHandler';
import type { useWorkspaceStore } from '@/stores/workspaceStore';
import { StoreError, ToolError } from '@/core/errors/errors';
import { drawPoint } from '@/core/annotations/annotationDrawer';
import { calculateRenderSizes } from '@/core/annotations/annotationRenderer';
import { clampPointToImageBounds } from '@/core/workspace/geometry/geometry';
import type { Point } from '@/core/geometry/geometry.types';

type WorkspaceStore = ReturnType<typeof useWorkspaceStore>;

export class PolygonToolHandler implements ToolHandler {
    private drawing = false;
    private points: Point[] = [];
    private currentPoint: Point | null = null;
    private speedMode = false;
    private speedModeInterval: number | null = null;
    private speedModeDistance = 0;
    private imageDimensions: { width: number; height: number } | null = null;

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
            throw new ToolError("Cannot create polygon: No label is selected.");
        }
        
        if (store.currentAssetId === null) {
            throw new StoreError("Cannot create polygon: Asset ID is missing.");
        }
        if (store.currentTaskId === null) {
            throw new StoreError("Cannot create polygon: Task ID is missing.");
        }

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        const clickPoint = this.validatePoint({ x: imageX, y: imageY }, store);

        // Set image dimensions for speed mode calculations
        if (store.getCurrentAsset && store.getCurrentAsset.width && store.getCurrentAsset.height) {
            const asset = store.getCurrentAsset;
            this.imageDimensions = { width: asset.width, height: asset.height };
        }

        // Add the point to the polygon
        this.points.push(clickPoint);
        this.currentPoint = clickPoint;
        this.drawing = true;

        // Check if speed mode should be activated
        if (event.shiftKey && this.points.length >= 1) {
            this.activateSpeedMode();
        }
    }

    onMouseMove(event: MouseEvent, store: WorkspaceStore): void {
        if (!this.drawing) return;

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;
        const imageX = (canvasX - store.viewOffset.x) / store.zoomLevel;
        const imageY = (canvasY - store.viewOffset.y) / store.zoomLevel;

        this.currentPoint = this.validatePoint({ x: imageX, y: imageY }, store);

        // Check for shift key to activate/deactivate speed mode
        if (event.shiftKey && !this.speedMode && this.points.length >= 1) {
            this.activateSpeedMode();
        } else if (!event.shiftKey && this.speedMode) {
            this.deactivateSpeedMode();
        }
    }

    onMouseUp(_event: MouseEvent, _store: WorkspaceStore): void {
        // For polygon, we continue on mouse up (unlike other tools)
    }

    onMouseLeave(_event: MouseEvent, _store: WorkspaceStore): void {
        // Don't auto-finish on mouse leave, user should use Enter key
        // Just reset if not enough points
        if (this.points.length < 3) {
            this.reset();
        }
    }

    onKeyDown(event: KeyboardEvent, store: WorkspaceStore): void {
        if (event.key === 'Enter' && this.drawing && this.points.length >= 3) {
            this.finishPolygon(store);
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

    draw(ctx: CanvasRenderingContext2D, zoomLevel: number = 1): void {
        if (!this.drawing || this.points.length === 0) return;

        const { lineWidth, pointRadius, thinLineWidth } = calculateRenderSizes(zoomLevel);

        // Draw the existing points of the polygon
        if (this.points.length >= 2) {
            // Draw lines between points (not closed yet)
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(this.points[0].x, this.points[0].y);
            for (let i = 1; i < this.points.length; i++) {
                ctx.lineTo(this.points[i].x, this.points[i].y);
            }
            ctx.lineWidth = lineWidth;
            ctx.strokeStyle = '#00FFFF';
            ctx.stroke();
            ctx.restore();
        }

        // Draw points for visual feedback
        this.points.forEach((point, index) => {
            // Make the first point slightly larger to indicate start
            const radius = index === 0 ? pointRadius * 1.5 : pointRadius;
            const color = index === 0 ? '#FF0000' : '#00FFFF';
            drawPoint(ctx, point.x, point.y, color, radius, thinLineWidth, '#FFFFFF');
        });

        // Draw the current line segment being created
        if (this.currentPoint && this.points.length > 0) {
            const lastPoint = this.points[this.points.length - 1];
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(lastPoint.x, lastPoint.y);
            ctx.lineTo(this.currentPoint.x, this.currentPoint.y);
            ctx.lineWidth = lineWidth;
            ctx.strokeStyle = '#00FFFF';
            ctx.stroke();
            ctx.restore();
        }
    }

    private finishPolygon(store: WorkspaceStore): void {
        if (this.points.length < 3) {
            this.reset();
            return;
        }

        const polygonCoordinates: PolygonAnnotationData = {
            type: AnnotationType.POLYGON,
            points: [...this.points], // Create a copy of the points array
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(),
            annotationType: AnnotationType.POLYGON,
            labelId: store.getSelectedLabelId!,
            coordinates: polygonCoordinates,
            assetId: Number(store.currentAssetId),
            taskId: Number(store.currentTaskId),
            isGroundTruth: true
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
        // Calculate distance as 1% of image diagonal
        const diagonal = Math.sqrt(
            this.imageDimensions.width * this.imageDimensions.width +
            this.imageDimensions.height * this.imageDimensions.height
        );
        this.speedModeDistance = diagonal * 0.01;
        
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