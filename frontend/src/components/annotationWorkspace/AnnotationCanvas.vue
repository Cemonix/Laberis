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
import {drawBoundingBox, drawPoint} from '@/core/annotationWorkspace/annotationDrawer';
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

let ctx: CanvasRenderingContext2D | null = null;
let resizeObserver: ResizeObserver | null = null;

const viewOffset = computed(() => workspaceStore.viewOffset);
const zoomLevel = computed(() => workspaceStore.zoomLevel);
const activeTool = computed(() => workspaceStore.activeTool);
const annotationsToRender = computed(() => workspaceStore.getAnnotations);

const canvasCursorStyle = computed(() => {
    switch (activeTool.value) {
        case ToolName.CURSOR:
            return isPanning.value ? 'grabbing' : 'grab';
        case ToolName.POINT:
        case ToolName.LINE:
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

    annotationsToRender.value.forEach(annotation => {
        if (annotation.annotationType === AnnotationType.POINT && annotation.coordinates?.type === AnnotationType.POINT) {
            const imageCoordX = annotation.coordinates.point.x;
            const imageCoordY = annotation.coordinates.point.y;

            const label = workspaceStore.getLabelById(annotation.labelId);
            const pointColor = label?.color || 'magenta';
            const pointRadius = 4 / zoomLevel.value;
            const lineWidth = 1 / zoomLevel.value;

            drawPoint(context, imageCoordX, imageCoordY, pointColor, pointRadius, lineWidth);
        }
        else if (annotation.annotationType === AnnotationType.BOUNDING_BOX && annotation.coordinates?.type === AnnotationType.BOUNDING_BOX) {
            const label = workspaceStore.getLabelById(annotation.labelId);
            const { topLeft, bottomRight } = annotation.coordinates;
            const width = bottomRight.x - topLeft.x;
            const height = bottomRight.y - topLeft.y;
            drawBoundingBox(context, topLeft.x, topLeft.y, width, height, label?.color || 'blue');
        }
    });
    
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
    // --- Only allow panning if CURSOR tool is active ---
    if (activeTool.value === ToolName.CURSOR) {
        event.preventDefault();
        if (event.button !== 0) return;
        isPanning.value = true;
        lastPanMousePosition.value = { x: event.offsetX, y: event.offsetY };
    } else {
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
    }
};

const handleMouseMove = (event: MouseEvent) => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
        if (!lastPanMousePosition.value) return;
        const dx = event.offsetX - lastPanMousePosition.value.x;
        const dy = event.offsetY - lastPanMousePosition.value.y;

        workspaceStore.setViewOffset({
            x: viewOffset.value.x + dx,
            y: viewOffset.value.y + dy,
        });
        lastPanMousePosition.value = { x: event.offsetX, y: event.offsetY };
    } else {
        annotationManager.onMouseMove(event);
    }
};

const handleMouseUp = (event: MouseEvent) => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
        isPanning.value = false;
    } else {
        annotationManager.onMouseUp(event);
    }
};

const handleMouseLeave = (event: MouseEvent) => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
       isPanning.value = false;
    } else {
       annotationManager.onMouseLeave(event);
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

const handleEscapeKey = (event: KeyboardEvent) => {
    if (event.key === 'Escape') {
        setCanvasDimensions();
        centerAndFitImage();
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
        window.addEventListener('keydown', handleEscapeKey);
    } else {
        logger.error("Canvas ref or wrapper ref not available on mount.");
    }
});

onBeforeUnmount(() => {
    if (resizeObserver && wrapperRef.value) {
        resizeObserver.unobserve(wrapperRef.value);
    }
    resizeObserver = null;
    window.removeEventListener('keydown', handleEscapeKey);
});

watch(
    () => props.imageUrl,
    (newUrl) => {
        console.log("imageUrl prop changed to:", newUrl);
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
