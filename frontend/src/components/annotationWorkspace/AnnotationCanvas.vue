<template>
    <div class="annotation-canvas-wrapper" ref="wrapperRef">
        <canvas 
            ref="canvasRef" 
            class="main-canvas"
            @mousedown="handleMouseDown"
            @mousemove="handleMouseMove"
            @mouseup="handleMouseUp"
            @mouseleave="handleMouseLeave"
            @wheel="handleWheel"
            :style="{ cursor: canvasCursorStyle }"
        ></canvas>
        <div v-if="isLoading" class="loading-overlay" role="status" aria-live="polite">Loading Image...</div>
        <div v-if="errorLoadingImage" class="error-overlay">Error loading image.</div>

        <AlertModal
            :is-open="isAlertOpen"
            :title="alertTitle"
            :message="alertMessage"
            @confirm="handleAlertConfirm"
        />
    </div>
</template>

<script setup lang="ts">
import {computed, nextTick, onBeforeUnmount, onMounted, ref, watch} from "vue";
import {useWorkspaceStore} from '@/stores/workspaceStore';
import type {Point} from "@/types/common/point";
import {ToolName} from "@/types/workspace/tools";
import {AnnotationType} from "@/types/workspace/annotation";
import {drawBoundingBox, drawPoint, drawLine, drawPolyline, drawPolygon, drawEditHandle} from '@/core/annotationWorkspace/annotationDrawer';
import {AnnotationManager} from "@/core/annotationWorkspace/annotationManager";
import {StoreError, ToolError} from "@/types/common/errors";
import AlertModal from "../common/modal/AlertModal.vue";
import {useAlert} from "@/composables/useAlert";
import {AppLogger} from "@/utils/logger";

const logger = AppLogger.createComponentLogger('AnnotationCanvas');

const { isAlertOpen, alertTitle, alertMessage, showAlert, handleAlertConfirm } = useAlert();

interface Props {
    imageUrl: string | null;
}

const props = defineProps<Props>();
const workspaceStore = useWorkspaceStore();

const annotationManager = new AnnotationManager(workspaceStore);

const wrapperRef = ref<HTMLDivElement | null>(null);
const canvasRef = ref<HTMLCanvasElement | null>(null);
const imageInstance = ref<HTMLImageElement | null>(null);
const isLoading = ref<boolean>(false);
const errorLoadingImage = ref<boolean>(false);
const isPanning = ref(false);
const lastPanMousePosition = ref<Point | null>(null);

// Annotation editing state
const selectedAnnotationId = ref<number | null>(null);
const isDraggingHandle = ref(false);
const draggedPointIndex = ref<number>(-1);
const hoveredHandleIndex = ref<number>(-1);
const hoveredAnnotationId = ref<number | null>(null);
const originalAnnotationBeforeEdit = ref<any>(null);

let ctx: CanvasRenderingContext2D | null = null;
let resizeObserver: ResizeObserver | null = null;

const viewOffset = computed(() => workspaceStore.viewOffset);
const zoomLevel = computed(() => workspaceStore.zoomLevel);
const activeTool = computed(() => workspaceStore.activeTool);
const annotationsToRender = computed(() => workspaceStore.getAnnotations);
const isAnnotationEditingDisabled = computed(() => workspaceStore.isAnnotationEditingDisabled);

const canvasCursorStyle = computed(() => {
    switch (activeTool.value) {
        case ToolName.CURSOR:
            if (isDraggingHandle.value) return 'grabbing';
            if (isPanning.value) return 'grabbing';
            if (hoveredHandleIndex.value >= 0) return 'grab';
            if (hoveredAnnotationId.value !== null) return 'pointer';
            return 'default';
        case ToolName.POINT:
        case ToolName.LINE:
        case ToolName.POLYLINE:
        case ToolName.BOUNDING_BOX:
        case ToolName.POLYGON:
            return 'crosshair';
        default:
            return 'default';
    }
});

const setCanvasDimensions = () => {
    if (!canvasRef.value || !wrapperRef.value || !ctx) {
        return;
    }

    const canvas = canvasRef.value;
    const availableWidth = wrapperRef.value.offsetWidth;
    const availableHeight = wrapperRef.value.offsetHeight;

    if (availableWidth === 0 || availableHeight === 0) {
        canvas.width = 0;
        canvas.height = 0;
        workspaceStore.setCanvasDisplayDimensions({ width: 0, height: 0 });
        return;
    }

    let targetCanvasWidth = availableWidth;
    let targetCanvasHeight = availableHeight;

    if (
        imageInstance.value &&
        imageInstance.value.naturalWidth > 0 &&
        imageInstance.value.naturalHeight > 0
    ) {
        const img = imageInstance.value;
        const imgWidth = img.naturalWidth;
        const imgHeight = img.naturalHeight;

        // Calculate scale to fit image within available space
        const scaleX = availableWidth / imgWidth;
        const scaleY = availableHeight / imgHeight;
        let scale = Math.min(scaleX, scaleY);

        targetCanvasWidth = Math.round(imgWidth * scale);
        targetCanvasHeight = Math.round(imgHeight * scale);
    } else {
        targetCanvasWidth = availableWidth;
        targetCanvasHeight = availableHeight;
    }

    canvas.width = targetCanvasWidth;
    canvas.height = targetCanvasHeight;

    workspaceStore.setCanvasDisplayDimensions({ width: canvas.width, height: canvas.height });
};

const loadImage = async (url: string | null) => {
    // Clear previous image/error state
    imageInstance.value = null;
    errorLoadingImage.value = false;

    workspaceStore.setViewOffset({ x: 0, y: 0 });
    workspaceStore.setZoomLevel(1);

    if (!url) {
        isLoading.value = false;
        workspaceStore.setCanvasDisplayDimensions({ width: 0, height: 0 });
        nextTick(() => {
            setCanvasDimensions();
            draw();
        });
        return;
    }

    if (!canvasRef.value) {
        logger.error("Canvas element not available for drawing.");
        isLoading.value = false;
        return;
    }

    if (!ctx) {
        ctx = canvasRef.value.getContext("2d");
        if (!ctx) {
            logger.error("Failed to get 2D rendering context.");
            isLoading.value = false;
            return;
        }
    }

    isLoading.value = true;

    const img = new Image();
    img.crossOrigin = "Anonymous";

    img.onload = () => {
        imageInstance.value = img;
        isLoading.value = false;
        workspaceStore.setImageNaturalDimensions({
            width: img.naturalWidth,
            height: img.naturalHeight
        });
        centerAndFitImage();
    };

    img.onerror = () => {
        logger.error("Error loading image:", url);
        imageInstance.value = null;
        isLoading.value = false;
        errorLoadingImage.value = true;
        workspaceStore.setImageNaturalDimensions({ width: 0, height: 0 });
        nextTick(() => {
            setCanvasDimensions();
        });
    };

    img.src = url;
};

const draw = () => {
    if (!canvasRef.value || !ctx) {
        return;
    }

    const canvas = canvasRef.value;
    ctx.save();

    // Clear the canvas before drawing
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.translate(viewOffset.value.x, viewOffset.value.y);
    ctx.scale(zoomLevel.value, zoomLevel.value);
    
    // TODO: Refactor to use a more robust image loading and drawing mechanism
    if (
        imageInstance.value &&
        imageInstance.value.naturalWidth > 0 &&
        imageInstance.value.naturalHeight > 0
    ) {
        const img = imageInstance.value;
        ctx.drawImage(img, 0, 0, img.naturalWidth, img.naturalHeight);
    } else if (errorLoadingImage.value) {
        ctx.restore();
        ctx.save();
        if (canvas.width > 0 && canvas.height > 0) {
            ctx.fillStyle = "#ff6b6b";
            ctx.font = "16px Arial";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(
                "Error loading image",
                canvas.width / 2,
                canvas.height / 2
            );
        }
    } else if (!isLoading.value && !props.imageUrl) {
        ctx.restore();
        ctx.save();
        if (canvas.width > 0 && canvas.height > 0) {
            ctx.fillStyle = "#555";
            ctx.font = "16px Arial";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(
                "No image loaded",
                canvas.width / 2,
                canvas.height / 2
            );
        }
    }
    
    drawSavedAnnotations(ctx);
    annotationManager.draw(ctx);

    ctx.restore();
};

const drawSavedAnnotations = (context: CanvasRenderingContext2D) => {
    if (!annotationsToRender.value) return;

    // Separate selected annotation from others to render it on top
    const selectedAnnotation = annotationsToRender.value.find(a => a.annotationId === selectedAnnotationId.value);
    const otherAnnotations = annotationsToRender.value.filter(a => a.annotationId !== selectedAnnotationId.value);
    
    // Draw non-selected annotations first
    const annotationsToDrawFirst = otherAnnotations;
    
    annotationsToDrawFirst.forEach(annotation => {
        const label = workspaceStore.getLabelById(annotation.labelId);
        const baseColor = label?.color || 'magenta';
        const isSelected = selectedAnnotationId.value === annotation.annotationId;
        const isHovered = hoveredAnnotationId.value === annotation.annotationId;
        
        // Brighten color if hovered
        const color = isHovered && !isSelected ? '#FFFFFF' : baseColor;

        if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
            const imageCoordX = annotation.coordinates.point.x;
            const imageCoordY = annotation.coordinates.point.y;
            const pointRadius = 4 / zoomLevel.value;
            const lineWidth = 1 / zoomLevel.value;

            drawPoint(context, imageCoordX, imageCoordY, color, pointRadius, lineWidth);
            
            // Draw edit handle for selected point
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, imageCoordX, imageCoordY, handleSize, baseColor, hoveredHandleIndex.value === 0);
            }
        }
        else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
            const { topLeft, bottomRight } = annotation.coordinates;
            const width = bottomRight.x - topLeft.x;
            const height = bottomRight.y - topLeft.y;
            drawBoundingBox(context, topLeft.x, topLeft.y, width, height, color);
            
            // Draw edit handles for selected bounding box corners
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, topLeft.x, topLeft.y, handleSize, baseColor, hoveredHandleIndex.value === 0);
                drawEditHandle(context, bottomRight.x, topLeft.y, handleSize, baseColor, hoveredHandleIndex.value === 1);
                drawEditHandle(context, bottomRight.x, bottomRight.y, handleSize, baseColor, hoveredHandleIndex.value === 2);
                drawEditHandle(context, topLeft.x, bottomRight.y, handleSize, baseColor, hoveredHandleIndex.value === 3);
            }
        }
        else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
            const { pointFrom, pointTo } = annotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawLine(context, pointFrom, pointTo, color, lineWidth);
            
            // Draw edit handles for selected line endpoints
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, pointFrom.x, pointFrom.y, handleSize, baseColor, hoveredHandleIndex.value === 0);
                drawEditHandle(context, pointTo.x, pointTo.y, handleSize, baseColor, hoveredHandleIndex.value === 1);
            }
        }
        else if (annotation.annotationType === AnnotationType.POLYLINE && annotation.coordinates?.type === AnnotationType.POLYLINE) {
            const { points } = annotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawPolyline(context, points, color, lineWidth);
            
            // Draw edit handles for selected polyline points
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                points.forEach((point, index) => {
                    drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex.value === index);
                });
            }
        }
        else if (annotation.annotationType === AnnotationType.POLYGON && annotation.coordinates?.type === AnnotationType.POLYGON) {
            const { points } = annotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawPolygon(context, points, color, lineWidth);
            
            // Draw edit handles for selected polygon points
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                points.forEach((point, index) => {
                    drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex.value === index);
                });
            }
        }
    });
    
    // Draw selected annotation on top for better accessibility
    if (selectedAnnotation) {
        const label = workspaceStore.getLabelById(selectedAnnotation.labelId);
        const baseColor = label?.color || 'magenta';
        const isSelected = true;
        const isHovered = hoveredAnnotationId.value === selectedAnnotation.annotationId;
        
        // Brighten color if hovered
        const color = isHovered && !isSelected ? '#FFFFFF' : baseColor;
        
        if (selectedAnnotation.annotationType === AnnotationType.POINT && selectedAnnotation.coordinates?.type === AnnotationType.POINT) {
            const imageCoordX = selectedAnnotation.coordinates.point.x;
            const imageCoordY = selectedAnnotation.coordinates.point.y;
            const pointRadius = 4 / zoomLevel.value;
            const lineWidth = 1 / zoomLevel.value;
            drawPoint(context, imageCoordX, imageCoordY, color, pointRadius, lineWidth);
            
            // Draw edit handle for selected point
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, imageCoordX, imageCoordY, handleSize, baseColor, hoveredHandleIndex.value === 0);
            }
        }
        else if (selectedAnnotation.annotationType === AnnotationType.BOUNDING_BOX && selectedAnnotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
            const { topLeft, bottomRight } = selectedAnnotation.coordinates;
            const width = bottomRight.x - topLeft.x;
            const height = bottomRight.y - topLeft.y;
            drawBoundingBox(context, topLeft.x, topLeft.y, width, height, color);
            
            // Draw edit handles for selected bounding box corners
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, topLeft.x, topLeft.y, handleSize, baseColor, hoveredHandleIndex.value === 0);
                drawEditHandle(context, bottomRight.x, topLeft.y, handleSize, baseColor, hoveredHandleIndex.value === 1);
                drawEditHandle(context, bottomRight.x, bottomRight.y, handleSize, baseColor, hoveredHandleIndex.value === 2);
                drawEditHandle(context, topLeft.x, bottomRight.y, handleSize, baseColor, hoveredHandleIndex.value === 3);
            }
        }
        else if (selectedAnnotation.annotationType === AnnotationType.LINE && selectedAnnotation.coordinates?.type === AnnotationType.LINE) {
            const { pointFrom, pointTo } = selectedAnnotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawLine(context, pointFrom, pointTo, color, lineWidth);
            
            // Draw edit handles for selected line endpoints
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                drawEditHandle(context, pointFrom.x, pointFrom.y, handleSize, baseColor, hoveredHandleIndex.value === 0);
                drawEditHandle(context, pointTo.x, pointTo.y, handleSize, baseColor, hoveredHandleIndex.value === 1);
            }
        }
        else if (selectedAnnotation.annotationType === AnnotationType.POLYLINE && selectedAnnotation.coordinates?.type === AnnotationType.POLYLINE) {
            const { points } = selectedAnnotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawPolyline(context, points, color, lineWidth);
            
            // Draw edit handles for selected polyline points
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                points.forEach((point, index) => {
                    drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex.value === index);
                });
            }
        }
        else if (selectedAnnotation.annotationType === AnnotationType.POLYGON && selectedAnnotation.coordinates?.type === AnnotationType.POLYGON) {
            const { points } = selectedAnnotation.coordinates;
            const lineWidth = Math.max(2 / zoomLevel.value, 2);
            drawPolygon(context, points, color, lineWidth);
            
            // Draw edit handles for selected polygon points
            if (isSelected) {
                const handleSize = Math.max(8 / zoomLevel.value, 6);
                points.forEach((point, index) => {
                    drawEditHandle(context, point.x, point.y, handleSize, baseColor, hoveredHandleIndex.value === index);
                });
            }
        }
    }
};

// Helper function to check if a point is near another point (for handle detection)
const isPointNearHandle = (clickPoint: Point, handlePoint: Point, threshold: number = 10): boolean => {
    const distance = Math.sqrt(
        Math.pow(clickPoint.x - handlePoint.x, 2) + 
        Math.pow(clickPoint.y - handlePoint.y, 2)
    );
    return distance <= threshold / zoomLevel.value;
};

// Helper function to check if a point is near a line
const isPointNearLine = (point: Point, lineStart: Point, lineEnd: Point, threshold: number = 10): boolean => {
    const adjustedThreshold = threshold / zoomLevel.value;
    
    // Calculate distance from point to line segment
    const A = point.x - lineStart.x;
    const B = point.y - lineStart.y;
    const C = lineEnd.x - lineStart.x;
    const D = lineEnd.y - lineStart.y;

    const dot = A * C + B * D;
    const lenSq = C * C + D * D;
    
    if (lenSq === 0) {
        // Line start and end are the same point
        return isPointNearHandle(point, lineStart, threshold);
    }
    
    let param = dot / lenSq;
    param = Math.max(0, Math.min(1, param)); // Clamp to line segment
    
    const xx = lineStart.x + param * C;
    const yy = lineStart.y + param * D;
    
    const dx = point.x - xx;
    const dy = point.y - yy;
    const distance = Math.sqrt(dx * dx + dy * dy);
    
    return distance <= adjustedThreshold;
};

// Helper function to check if a point is inside a bounding box
const isPointInBoundingBox = (point: Point, topLeft: Point, bottomRight: Point): boolean => {
    return point.x >= topLeft.x && point.x <= bottomRight.x &&
           point.y >= topLeft.y && point.y <= bottomRight.y;
};

// Helper function to check if a point is near a polyline
const isPointNearPolyline = (point: Point, points: Point[], threshold: number = 10): boolean => {
    if (points.length < 2) return false;
    
    for (let i = 0; i < points.length - 1; i++) {
        if (isPointNearLine(point, points[i], points[i + 1], threshold)) {
            return true;
        }
    }
    return false;
};

// Helper function to check if a point is inside a polygon (ray casting algorithm)
const isPointInPolygon = (point: Point, points: Point[]): boolean => {
    if (points.length < 3) return false;
    
    let inside = false;
    for (let i = 0, j = points.length - 1; i < points.length; j = i++) {
        if (((points[i].y > point.y) !== (points[j].y > point.y)) &&
            (point.x < (points[j].x - points[i].x) * (point.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)) {
            inside = !inside;
        }
    }
    return inside;
};

// Helper function to check if a point hits an annotation
const checkAnnotationHit = (annotation: any, imagePoint: Point, isSelected: boolean = false): { annotation: any, handleIndex: number } | null => {
    // Check if clicking on a handle (only for selected annotation)
    if (isSelected) {
        if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
            if (isPointNearHandle(imagePoint, annotation.coordinates.point)) {
                return { annotation, handleIndex: 0 };
            }
        }
        else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
            if (isPointNearHandle(imagePoint, annotation.coordinates.pointFrom)) {
                return { annotation, handleIndex: 0 };
            }
            if (isPointNearHandle(imagePoint, annotation.coordinates.pointTo)) {
                return { annotation, handleIndex: 1 };
            }
        }
        else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
            const { topLeft, bottomRight } = annotation.coordinates;
            const corners = [
                topLeft,
                { x: bottomRight.x, y: topLeft.y },
                bottomRight,
                { x: topLeft.x, y: bottomRight.y }
            ];
            
            for (let j = 0; j < corners.length; j++) {
                if (isPointNearHandle(imagePoint, corners[j])) {
                    return { annotation, handleIndex: j };
                }
            }
        }
        else if ((annotation.annotationType === AnnotationType.POLYLINE || annotation.annotationType === AnnotationType.POLYGON) 
                 && annotation.coordinates?.type === annotation.annotationType) {
            const points = annotation.coordinates.points;
            for (let j = 0; j < points.length; j++) {
                if (isPointNearHandle(imagePoint, points[j])) {
                    return { annotation, handleIndex: j };
                }
            }
        }
    }
    
    // Check if clicking on the annotation itself (for selection)
    if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
        if (isPointNearHandle(imagePoint, annotation.coordinates.point, 15)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
        const { pointFrom, pointTo } = annotation.coordinates;
        if (isPointNearLine(imagePoint, pointFrom, pointTo, 10)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
        const { topLeft, bottomRight } = annotation.coordinates;
        if (isPointInBoundingBox(imagePoint, topLeft, bottomRight)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.POLYLINE && annotation.coordinates?.type === AnnotationType.POLYLINE) {
        const { points } = annotation.coordinates;
        if (isPointNearPolyline(imagePoint, points, 10)) {
            return { annotation, handleIndex: -1 };
        }
    }
    else if (annotation.annotationType === AnnotationType.POLYGON && annotation.coordinates?.type === AnnotationType.POLYGON) {
        const { points } = annotation.coordinates;
        if (isPointInPolygon(imagePoint, points) || isPointNearPolyline(imagePoint, points, 10)) {
            return { annotation, handleIndex: -1 };
        }
    }
    
    return null;
};

// Helper function to find annotation at mouse position
const findAnnotationAtPoint = (imagePoint: Point): { annotation: any, handleIndex: number } | null => {
    if (!annotationsToRender.value) return null;

    // First check if we're clicking on the selected annotation (highest priority)
    const selectedAnnotation = annotationsToRender.value.find(a => a.annotationId === selectedAnnotationId.value);
    if (selectedAnnotation) {
        const result = checkAnnotationHit(selectedAnnotation, imagePoint, true);
        if (result) return result;
    }

    // Then check other annotations in reverse order to prioritize top annotations
    for (let i = annotationsToRender.value.length - 1; i >= 0; i--) {
        const annotation = annotationsToRender.value[i];
        
        // Skip selected annotation as we already checked it
        if (annotation.annotationId === selectedAnnotationId.value) continue;
        
        // Check this annotation for hits
        const result = checkAnnotationHit(annotation, imagePoint, false);
        if (result) return result;
    }
    
    return null;
};

// Function to update annotation point during dragging (local only, no backend save)
const updateAnnotationPointLocally = (annotationId: number, handleIndex: number, newPoint: Point) => {
    const annotation = annotationsToRender.value?.find(a => a.annotationId === annotationId);
    if (!annotation) return;

    let updatedCoordinates = null;

    if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
        updatedCoordinates = {
            ...annotation.coordinates,
            point: newPoint
        };
    }
    else if (annotation.annotationType === AnnotationType.LINE && annotation.coordinates?.type === AnnotationType.LINE) {
        updatedCoordinates = {
            ...annotation.coordinates,
            ...(handleIndex === 0 ? { pointFrom: newPoint } : { pointTo: newPoint })
        };
    }
    else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
        const { topLeft, bottomRight } = annotation.coordinates;
        const corners = [
            { key: 'topLeft', point: topLeft },
            { key: 'topRight', point: { x: bottomRight.x, y: topLeft.y } },
            { key: 'bottomRight', point: bottomRight },
            { key: 'bottomLeft', point: { x: topLeft.x, y: bottomRight.y } }
        ];

        if (handleIndex >= 0 && handleIndex < corners.length) {
            // Update the appropriate corner
            let newTopLeft = topLeft;
            let newBottomRight = bottomRight;

            switch (handleIndex) {
                case 0: // topLeft
                    newTopLeft = newPoint;
                    break;
                case 1: // topRight
                    newTopLeft = { x: topLeft.x, y: newPoint.y };
                    newBottomRight = { x: newPoint.x, y: bottomRight.y };
                    break;
                case 2: // bottomRight
                    newBottomRight = newPoint;
                    break;
                case 3: // bottomLeft
                    newTopLeft = { x: newPoint.x, y: topLeft.y };
                    newBottomRight = { x: bottomRight.x, y: newPoint.y };
                    break;
            }

            updatedCoordinates = {
                ...annotation.coordinates,
                topLeft: newTopLeft,
                bottomRight: newBottomRight
            };
        }
    }
    else if ((annotation.annotationType === AnnotationType.POLYLINE || annotation.annotationType === AnnotationType.POLYGON) 
             && annotation.coordinates?.type === annotation.annotationType) {
        const points = [...annotation.coordinates.points];
        if (handleIndex >= 0 && handleIndex < points.length) {
            points[handleIndex] = newPoint;
            updatedCoordinates = {
                ...annotation.coordinates,
                points
            };
        }
    }

    if (updatedCoordinates) {
        // Update the annotation locally only (no backend call)
        annotation.coordinates = updatedCoordinates;
    }
};

// Function to save annotation changes to backend
const saveAnnotationChanges = async (annotationId: number) => {
    const annotation = annotationsToRender.value?.find(a => a.annotationId === annotationId);
    if (!annotation || !annotation.coordinates) return;

    try {
        await workspaceStore.updateAnnotation(annotationId, { coordinates: annotation.coordinates });
        logger.info('Annotation saved successfully');
        originalAnnotationBeforeEdit.value = null; // Clear backup after successful save
    } catch (error) {
        logger.error('Failed to save annotation changes, reverting:', error);
        // Revert to original on error
        if (originalAnnotationBeforeEdit.value) {
            annotation.coordinates = originalAnnotationBeforeEdit.value.coordinates;
            originalAnnotationBeforeEdit.value = null;
            nextTick(draw);
        }
    }
};

const centerAndFitImage = () => {
    if (!canvasRef.value || !imageInstance.value || !workspaceStore.imageNaturalDimensions) {
        workspaceStore.setViewOffset({ x: 0, y: 0 });
        workspaceStore.setZoomLevel(1.0);
        nextTick(draw);
        return;
    }

    const canvas = canvasRef.value;
    const { width: imgWidth, height: imgHeight } = workspaceStore.imageNaturalDimensions;

    if (imgWidth === 0 || imgHeight === 0 || canvas.width === 0 || canvas.height === 0) {
        workspaceStore.setViewOffset({ x: 0, y: 0 });
        workspaceStore.setZoomLevel(1.0);
        nextTick(draw);
        return;
    }
    
    // Calculate scale to fit image within canvas
    const scaleX = canvas.width / imgWidth;
    const scaleY = canvas.height / imgHeight;
    const newZoom = Math.min(scaleX, scaleY);

    // Calculate offset to center the image
    const newOffsetX = (canvas.width - imgWidth * newZoom) / 2;
    const newOffsetY = (canvas.height - imgHeight * newZoom) / 2;

    workspaceStore.setZoomLevel(newZoom);
    workspaceStore.setViewOffset({ x: newOffsetX, y: newOffsetY });
};

const handleMouseDown = (event: MouseEvent) => {
    const canvasX = event.offsetX;
    const canvasY = event.offsetY;
    const imageX = (canvasX - viewOffset.value.x) / zoomLevel.value;
    const imageY = (canvasY - viewOffset.value.y) / zoomLevel.value;
    const imagePoint = { x: imageX, y: imageY };

    // --- Only allow panning if CURSOR tool is active ---
    if (activeTool.value === ToolName.CURSOR) {
        event.preventDefault();
        if (event.button !== 0) return;
        
        // Check for annotation interaction first
        const hit = findAnnotationAtPoint(imagePoint);
        if (hit) {
            if (hit.handleIndex >= 0 && !isAnnotationEditingDisabled.value) {
                // Start dragging a handle (only if editing is enabled)
                // Store original annotation for potential rollback
                originalAnnotationBeforeEdit.value = JSON.parse(JSON.stringify(hit.annotation));
                isDraggingHandle.value = true;
                draggedPointIndex.value = hit.handleIndex;
                selectedAnnotationId.value = hit.annotation.annotationId;
            } else {
                // Select the annotation (always allowed for viewing)
                selectedAnnotationId.value = hit.annotation.annotationId;
            }
            nextTick(draw);
            return;
        }
        
        // Clear selection if clicking on empty area
        selectedAnnotationId.value = null;
        
        // Start panning
        isPanning.value = true;
        lastPanMousePosition.value = { x: event.offsetX, y: event.offsetY };
    } else if (!isAnnotationEditingDisabled.value) {
        // Only allow annotation creation if editing is enabled
        try {
            annotationManager.onMouseDown(event);
        }
        catch (error) {
            if (error instanceof StoreError || error instanceof ToolError) {
                showAlert(error.alertTitle, error.message);
            }
            else {
                showAlert("Unexpected Error", "An unexpected error occurred while processing your action. Please try again.");
            }
        }

        // Redraw canvas after mouse down for tools
        nextTick(draw);
    }
};

const handleMouseMove = (event: MouseEvent) => {
    const canvasX = event.offsetX;
    const canvasY = event.offsetY;
    const imageX = (canvasX - viewOffset.value.x) / zoomLevel.value;
    const imageY = (canvasY - viewOffset.value.y) / zoomLevel.value;
    const imagePoint = { x: imageX, y: imageY };

    if (activeTool.value === ToolName.CURSOR) {
        if (isDraggingHandle.value && selectedAnnotationId.value !== null) {
            // Handle dragging - update annotation coordinates locally only
            updateAnnotationPointLocally(selectedAnnotationId.value, draggedPointIndex.value, imagePoint);
            nextTick(draw);
        } else if (isPanning.value) {
            // Panning
            if (!lastPanMousePosition.value) return;
            const dx = event.offsetX - lastPanMousePosition.value.x;
            const dy = event.offsetY - lastPanMousePosition.value.y;

            workspaceStore.setViewOffset({
                x: viewOffset.value.x + dx,
                y: viewOffset.value.y + dy,
            });
            lastPanMousePosition.value = { x: event.offsetX, y: event.offsetY };
        } else {
            // Update hover state for handles and annotations
            const hit = findAnnotationAtPoint(imagePoint);
            hoveredHandleIndex.value = hit?.handleIndex ?? -1;
            hoveredAnnotationId.value = hit?.annotation?.annotationId ?? null;
            nextTick(draw);
        }
    } else if (!isAnnotationEditingDisabled.value) {
        // Only allow annotation tools if editing is enabled
        annotationManager.onMouseMove(event);
        // Redraw canvas during mouse move for tool preview
        nextTick(draw);
    }
};

const handleMouseUp = (event: MouseEvent) => {
    if (activeTool.value === ToolName.CURSOR) {
        if (isDraggingHandle.value && selectedAnnotationId.value !== null) {
            // Finish dragging handle - save changes to backend
            saveAnnotationChanges(selectedAnnotationId.value);
            isDraggingHandle.value = false;
            draggedPointIndex.value = -1;
            nextTick(draw);
        } else if (isPanning.value) {
            isPanning.value = false;
        }
    } else if (!isAnnotationEditingDisabled.value) {
        // Only allow annotation tools if editing is enabled
        annotationManager.onMouseUp(event);
        // Redraw canvas after mouse up
        nextTick(draw);
    }
};

const handleMouseLeave = (event: MouseEvent) => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
       isPanning.value = false;
    } else {
       annotationManager.onMouseLeave(event);
       // Redraw canvas after mouse leave
       nextTick(draw);
    }
};

const handleWheel = (event: WheelEvent) => {
    event.preventDefault();

    if (!canvasRef.value) return;

    const { zoomSensitivity } = workspaceStore.getZoomConfig;
    const delta = event.deltaY * -1; // Invert for natural zoom
    const zoomFactor = Math.exp(delta * zoomSensitivity);
    
    const newZoomLevel = zoomLevel.value * zoomFactor;

    // Calculate mouse position relative to the canvas
    const mouseX = event.offsetX;
    const mouseY = event.offsetY;

    // Calculate image point under mouse before zoom
    const imagePointX = (mouseX - viewOffset.value.x) / zoomLevel.value;
    const imagePointY = (mouseY - viewOffset.value.y) / zoomLevel.value;

    // Update zoom level first (this will be clamped in the store action)
    workspaceStore.setZoomLevel(newZoomLevel);
    const actualNewZoomLevel = workspaceStore.zoomLevel; // Get potentially clamped value

    // Calculate new viewOffset to keep imagePoint under the mouse
    const newViewOffsetX = mouseX - imagePointX * actualNewZoomLevel;
    const newViewOffsetY = mouseY - imagePointY * actualNewZoomLevel;
    
    workspaceStore.setViewOffset({ x: newViewOffsetX, y: newViewOffsetY });
};

const handleKeyDown = (event: KeyboardEvent) => {
    // Don't handle shortcuts when typing in inputs
    if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
    }

    if (event.key === 'Escape') {
        // First try to let tool handler handle escape (for aborting annotations)
        annotationManager.onKeyDown?.(event);
        // If no annotation is being drawn, reset canvas view
        if (!annotationManager.isDrawing()) {
            setCanvasDimensions();
            centerAndFitImage();
        }
        nextTick(draw);
    } else if (event.key === 'Enter') {
        // Finish current annotation if applicable
        annotationManager.onKeyDown?.(event);
        nextTick(draw);
    } else if (event.key === 'Shift') {
        // Handle shift key for speed mode
        annotationManager.onKeyDown?.(event);
        nextTick(draw);
    } else if ((event.key === 'Delete' || event.key === 'Backspace') && !isAnnotationEditingDisabled.value) {
        // Delete selected annotation (only if editing is enabled)
        if (selectedAnnotationId.value !== null) {
            event.preventDefault();
            handleDeleteSelectedAnnotation();
        }
    }
};

// Function to handle deletion of selected annotation
const handleDeleteSelectedAnnotation = async () => {
    if (selectedAnnotationId.value === null) return;
    
    try {
        // Remove from workspace store
        await workspaceStore.deleteAnnotation(selectedAnnotationId.value);
        // Clear selection after successful deletion
        selectedAnnotationId.value = null;
        nextTick(draw);
    } catch (error) {
        console.error('Failed to delete annotation:', error);
    }
};

onMounted(() => {
    if (canvasRef.value && wrapperRef.value) {
        ctx = canvasRef.value.getContext("2d");
        if (!ctx) {
            logger.error("Failed to get 2D rendering context on mount.");
            return;
        }

        setCanvasDimensions();

        resizeObserver = new ResizeObserver(() => {
            setCanvasDimensions();
            centerAndFitImage();
        });
        resizeObserver.observe(wrapperRef.value);

        if (props.imageUrl) {
            loadImage(props.imageUrl);
        }
        else {
            centerAndFitImage();
        }
        window.addEventListener('keydown', handleKeyDown);
    } else {
        logger.error("Canvas ref or wrapper ref not available on mount.");
    }
});

onBeforeUnmount(() => {
    if (resizeObserver && wrapperRef.value) {
        resizeObserver.unobserve(wrapperRef.value);
    }
    resizeObserver = null;
    window.removeEventListener('keydown', handleKeyDown);
});

watch(
    () => props.imageUrl,
    (newUrl) => {
        logger.info("imageUrl prop changed to:", newUrl);
        loadImage(newUrl);
    }
);

watch([viewOffset, zoomLevel], () => {
    nextTick(draw);
});

watch(() => workspaceStore.imageNaturalDimensions, (newDim) => {
    if(newDim && newDim.width > 0 && newDim.height > 0 && imageInstance.value) {
        // This implies an image has just loaded or reloaded its dimensions
        // If canvas is already sized, we might want to ensure it's centered.
        // loadImage -> centerAndFitImage should handle this.
        // This watcher might be redundant if loadImage flow is robust.
        // For safety, or if dimensions could change independently:
        centerAndFitImage();
    }
}, { deep: true });

watch(() => workspaceStore.canvasDisplayDimensions, () => {
    // If canvas display dimensions change (e.g. from store, though usually driven by wrapperRef resize)
    // it might be necessary to redraw or even re-fit.
    // ResizeObserver handles wrapper changes. This is more for programmatic changes.
    nextTick(draw);
}, {deep: true});

watch(() => annotationsToRender.value, () => {
    // Redraw canvas when annotations change (add, update, delete)
    nextTick(draw);
}, { deep: true });
</script>

<style lang="scss" scoped>
.annotation-canvas-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    position: relative;
    background-color: var(--color-dark-blue-3);
    overflow: hidden;
}

.main-canvas {
    display: block;
}

%overlay-base {
    display: flex;
    align-items: center;
    justify-content: center;
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    font-size: 1.25rem;
    z-index: 10;
    pointer-events: none; // Allow interaction with canvas below
}

.loading-overlay,
.error-overlay {
    @extend %overlay-base;
    background-color: rgba(var(--color-black), 0.5);
    color: var(--color-white);

    &.error-overlay {
        color: var(--color-error);
    }
}
</style>
