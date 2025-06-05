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
        <div v-if="isLoading" class="loading-overlay">Loading Image...</div>
        <div v-if="errorLoadingImage" class="error-overlay">Error loading image.</div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useWorkspaceStore } from '@/stores/workspaceStore';
import type { Point } from "@/types/common/point";
import { ToolName } from "@/types/workspace/tools";
import { v4 as uuidv4 } from 'uuid';
import type { Annotation, PointAnnotationData } from '@/types/workspace/annotation';
import { drawPoint, drawBoundingBox } from '@/utils/canvasDrawer';

interface Props {
    imageUrl: string | null;
}
// FIXME: There is a bug when scaling window - escape key does not scale image to fit
const props = defineProps<Props>();
const workspaceStore = useWorkspaceStore();


const wrapperRef = ref<HTMLDivElement | null>(null);
const canvasRef = ref<HTMLCanvasElement | null>(null);
const imageInstance = ref<HTMLImageElement | null>(null);
const isLoading = ref<boolean>(false);
const errorLoadingImage = ref<boolean>(false);
const isPanning = ref(false);
const lastPanMousePosition = ref<Point | null>(null);

const viewOffset = computed(() => workspaceStore.viewOffset);
const zoomLevel = computed(() => workspaceStore.zoomLevel);
const activeTool = computed(() => workspaceStore.activeTool);
const selectedLabelId = computed(() => workspaceStore.getSelectedLabelId);
const annotationsToRender = computed(() => workspaceStore.getAnnotations);
const currentAssetId = computed(() => workspaceStore.currentAssetId);
const currentTaskId = computed(() => workspaceStore.currentTaskId);   

let ctx: CanvasRenderingContext2D | null = null;
let resizeObserver: ResizeObserver | null = null;

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
        console.error("Canvas element not available for drawing.");
        isLoading.value = false;
        return;
    }

    if (!ctx) {
        ctx = canvasRef.value.getContext("2d");
        if (!ctx) {
            console.error("Failed to get 2D rendering context.");
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
        console.error("Error loading image:", url);
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
    
    drawAnnotations(ctx);

    ctx.restore();
};

const drawAnnotations = (context: CanvasRenderingContext2D) => {
    if (annotationsToRender.value && annotationsToRender.value.length > 0) {
        annotationsToRender.value.forEach(annotation => {
            if (annotation.annotationType === 'point' && annotation.coordinates.type === 'point') {
                const imageCoordX = annotation.coordinates.x;
                const imageCoordY = annotation.coordinates.y;

                const label = workspaceStore.getLabelById(annotation.labelId);
                const pointColor = label?.color || 'magenta';
                const pointRadius = 4 / zoomLevel.value;
                const lineWidth = 1 / zoomLevel.value;

                drawPoint(context, imageCoordX, imageCoordY, pointColor, pointRadius, lineWidth);
            }
            else if (annotation.annotationType === 'bounding_box' && annotation.coordinates.type === 'bounding_box') {
                const { x, y, width, height } = annotation.coordinates;
                const label = workspaceStore.getLabelById(annotation.labelId);
                drawBoundingBox(context, x, y, width, height, label?.color || 'blue');
            }
        });
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
    // --- Only allow panning if CURSOR tool is active ---
    if (activeTool.value === ToolName.CURSOR) {
        if (event.button !== 0) return;
        isPanning.value = true;
        lastPanMousePosition.value = { x: event.offsetX, y: event.offsetY };
        event.preventDefault();
    } 
    if (activeTool.value === ToolName.POINT) {
        event.preventDefault();

        if (selectedLabelId.value === null) {
            // TODO: Implement a more user-friendly notification system
            alert("Please select a label first to create a point annotation.");
            return;
        }
        if (currentAssetId.value === null) {
            console.error("Cannot create point: Asset ID is missing.");
            alert("Error: Asset information is missing.");
            return;
        }
        // TODO: Uncomment when task management is implemented
        // if (currentTaskId.value === null) {
        //     console.error("Cannot create point: Task ID is missing.");
        //     alert("Error: Task information is missing.");
        //     return;
        // }

        const canvasX = event.offsetX;
        const canvasY = event.offsetY;


        const imageX = (canvasX - viewOffset.value.x) / zoomLevel.value;
        const imageY = (canvasY - viewOffset.value.y) / zoomLevel.value;

        const pointCoordinates: PointAnnotationData = {
            type: 'point',
            x: imageX,
            y: imageY
        };

        const newAnnotation: Annotation = {
            clientId: uuidv4(), // TODO: Generate a temporary client-side ID
            annotationType: 'point',
            labelId: selectedLabelId.value,
            coordinates: pointCoordinates,
            assetId: Number(currentAssetId.value),
            taskId: Number(currentTaskId.value)
        };

        workspaceStore.addAnnotation(newAnnotation);
        draw(); // Redraw canvas to show new annotation
    }
    else if (activeTool.value === ToolName.LINE) {
        // Logic for line tool mousedown
    }
    else if (activeTool.value === ToolName.BOUNDING_BOX) {
        // Logic for bounding box tool mousedown
    }
    else if (activeTool.value === ToolName.POLYGON) {
        // Logic for polygon tool mousedown
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
    }
};

const handleMouseUp = () => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
        isPanning.value = false;
    }
};

const handleMouseLeave = () => {
    if (isPanning.value && activeTool.value === ToolName.CURSOR) {
       handleMouseUp(); // Treat leaving the canvas as mouse up for panning
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
        centerAndFitImage();
    }
};

onMounted(() => {
    if (canvasRef.value && wrapperRef.value) {
        ctx = canvasRef.value.getContext("2d");
        if (!ctx) {
            console.error("Failed to get 2D rendering context on mount.");
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
        console.error("Canvas ref or wrapper ref not available on mount.");
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
@use "@/styles/variables.scss" as vars;

.annotation-canvas-wrapper {
    width: 100%;
    height: 100%;
    position: relative;
    background-color: vars.$canvas-wrapper-bg;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
}

.main-canvas {
    display: block;
}

%overlay-base {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.2em;
    z-index: 10;
    pointer-events: none; // Allow interaction with canvas below
}

.loading-overlay,
.error-overlay {
    @extend %overlay-base;
    background-color: vars.$overlay-bg;
    color: vars.$overlay-text-color;

    &.error-overlay {
        color: vars.$error-overlay-text-color;
    }
}
</style>
