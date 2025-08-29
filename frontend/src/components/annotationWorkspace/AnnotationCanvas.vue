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
import type {Point} from "@/core/geometry/geometry.types";
import {ToolName} from "@/core/workspace/tools.types";
import {
    AnnotationManager,
    findAnnotationAtPoint,
    updateAnnotationPointLocally,
    calculateCanvasDimensions,
    calculateCenterAndFitView,
    mouseToImageCoordinates,
    calculateZoomFromWheel,
    calculateZoomViewOffset,
    calculateCanvasCursorStyle,
    renderAnnotation,
    type AnnotationRenderContext,
    getAnnotationDisplayColor,
    separateAnnotationsBySelection
} from "@/core/workspace";
import {StoreError, ToolError} from "@/core/errors/errors";
import AlertModal from "../common/modal/AlertModal.vue";
import {useAlert} from "@/composables/useAlert";
import {AppLogger} from "@/core/logger/logger";
import type { Annotation } from "@/core/workspace/annotation.types";

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
    return calculateCanvasCursorStyle(
        activeTool.value,
        isDraggingHandle.value,
        isPanning.value,
        hoveredHandleIndex.value,
        hoveredAnnotationId.value
    );
});

const setCanvasDimensions = () => {
    if (!canvasRef.value || !wrapperRef.value || !ctx) {
        return;
    }

    const canvas = canvasRef.value;
    const availableWidth = wrapperRef.value.offsetWidth;
    const availableHeight = wrapperRef.value.offsetHeight;

    const imageNaturalDimensions = imageInstance.value ? 
        { width: imageInstance.value.naturalWidth, height: imageInstance.value.naturalHeight } : 
        undefined;

    const dimensions = calculateCanvasDimensions(
        availableWidth,
        availableHeight,
        imageNaturalDimensions
    );

    canvas.width = dimensions.width;
    canvas.height = dimensions.height;

    workspaceStore.setCanvasDisplayDimensions(dimensions);
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
    annotationManager.draw(ctx, zoomLevel.value);

    ctx.restore();
};

const drawSavedAnnotations = (context: CanvasRenderingContext2D) => {
    if (!annotationsToRender.value) return;

    // Separate annotations for layered rendering (selected on top)
    const { selectedAnnotation, otherAnnotations } = separateAnnotationsBySelection(
        annotationsToRender.value, 
        selectedAnnotationId.value
    );
    
    // Helper function to create render context for an annotation
    const createRenderContext = (annotation: any, isSelected: boolean): AnnotationRenderContext => {
        const label = workspaceStore.getLabelById(annotation.labelId);
        const baseColor = label?.color || 'magenta';
        const isHovered = hoveredAnnotationId.value === annotation.annotationId;
        const color = getAnnotationDisplayColor(baseColor, isSelected, isHovered);

        return {
            context,
            annotation,
            color,
            baseColor,
            isSelected,
            zoomLevel: zoomLevel.value,
            hoveredHandleIndex: hoveredHandleIndex.value
        };
    };
    
    // Draw non-selected annotations first
    otherAnnotations.forEach(annotation => {
        const renderContext = createRenderContext(annotation, false);
        renderAnnotation(renderContext);
    });
    
    // Draw selected annotation on top for better accessibility
    if (selectedAnnotation) {
        const renderContext = createRenderContext(selectedAnnotation, true);
        renderAnnotation(renderContext);
    }
};


// Function to save annotation changes to backend
const saveAnnotationChanges = async (annotationId: number) => {
    const annotation = annotationsToRender.value?.find((a: Annotation) => a.annotationId === annotationId);
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
    const viewState = calculateCenterAndFitView(
        canvas.width,
        canvas.height,
        workspaceStore.imageNaturalDimensions
    );

    workspaceStore.setZoomLevel(viewState.zoomLevel);
    workspaceStore.setViewOffset(viewState.offset);
};

const handleMouseDown = (event: MouseEvent) => {
    const canvasX = event.offsetX;
    const canvasY = event.offsetY;
    const imagePoint = mouseToImageCoordinates(canvasX, canvasY, viewOffset.value, zoomLevel.value);

    // --- Only allow panning if CURSOR tool is active ---
    if (activeTool.value === ToolName.CURSOR) {
        event.preventDefault();
        if (event.button !== 0) return;
        
        // Check for annotation interaction first
        const hit = findAnnotationAtPoint(imagePoint, annotationsToRender.value || [], selectedAnnotationId.value, zoomLevel.value);
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
    const imagePoint = mouseToImageCoordinates(canvasX, canvasY, viewOffset.value, zoomLevel.value);

    if (activeTool.value === ToolName.CURSOR) {
        if (isDraggingHandle.value && selectedAnnotationId.value !== null) {
            // Handle dragging - update annotation coordinates locally only
            const annotation = annotationsToRender.value?.find((a: Annotation) => a.annotationId === selectedAnnotationId.value);
            if (annotation) {
                const imageDims = workspaceStore.getCurrentAsset && workspaceStore.getCurrentAsset.width && workspaceStore.getCurrentAsset.height
                    ? { width: workspaceStore.getCurrentAsset.width, height: workspaceStore.getCurrentAsset.height }
                    : undefined;
                updateAnnotationPointLocally(annotation, draggedPointIndex.value, imagePoint, imageDims);
                nextTick(draw);
            }
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
            const hit = findAnnotationAtPoint(imagePoint, annotationsToRender.value || [], selectedAnnotationId.value, zoomLevel.value);
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
    const newZoomLevel = calculateZoomFromWheel(zoomLevel.value, event.deltaY, zoomSensitivity);

    // Calculate mouse position relative to the canvas
    const mouseX = event.offsetX;
    const mouseY = event.offsetY;

    // Calculate image point under mouse before zoom
    const imagePoint = mouseToImageCoordinates(mouseX, mouseY, viewOffset.value, zoomLevel.value);

    // Update zoom level first (this will be clamped in the store action)
    workspaceStore.setZoomLevel(newZoomLevel);
    const actualNewZoomLevel = workspaceStore.zoomLevel; // Get potentially clamped value

    // Calculate new viewOffset to keep imagePoint under the mouse
    const newViewOffset = calculateZoomViewOffset(mouseX, mouseY, imagePoint.x, imagePoint.y, actualNewZoomLevel);
    
    workspaceStore.setViewOffset(newViewOffset);
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
