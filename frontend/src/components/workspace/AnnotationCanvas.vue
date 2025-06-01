<template>
    <div class="annotation-canvas-wrapper" ref="wrapperRef">
        <canvas ref="canvasRef" class="main-canvas"></canvas>
        <div v-if="isLoading" class="loading-overlay">Loading Image...</div>
        <div v-if="errorLoadingImage" class="error-overlay">Error loading image.</div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useWorkspaceStore } from '@/stores/workspaceStore';

interface Props {
    imageUrl: string | null;
}

const props = defineProps<Props>();
const workspaceStore = useWorkspaceStore();

const wrapperRef = ref<HTMLDivElement | null>(null);
const canvasRef = ref<HTMLCanvasElement | null>(null);
const imageInstance = ref<HTMLImageElement | null>(null);
const isLoading = ref<boolean>(false);
const errorLoadingImage = ref<boolean>(false);

let ctx: CanvasRenderingContext2D | null = null;
let resizeObserver: ResizeObserver | null = null;

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

    if (!url) {
        isLoading.value = false;
        workspaceStore.setCanvasDisplayDimensions({ width: 0, height: 0 });
        nextTick(() => setCanvasDimensions());
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
        nextTick(() => {
            setCanvasDimensions();
            draw();
        });
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
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (
        imageInstance.value &&
        imageInstance.value.naturalWidth > 0 &&
        imageInstance.value.naturalHeight > 0
    ) {
        const img = imageInstance.value;
        ctx.drawImage(
            img, 0, 0, img.naturalWidth, img.naturalHeight, 0, 0, canvas.width, canvas.height
        );
    } else if (errorLoadingImage.value) {
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
            draw();
        });
        resizeObserver.observe(wrapperRef.value);

        if (props.imageUrl) {
            loadImage(props.imageUrl);
            draw();
        }
    } else {
        console.error("Canvas ref or wrapper ref not available on mount.");
    }
});

onBeforeUnmount(() => {
    if (resizeObserver && wrapperRef.value) {
        resizeObserver.unobserve(wrapperRef.value);
    }
    resizeObserver = null;
});

watch(
    () => props.imageUrl,
    (newUrl) => {
        console.log("imageUrl prop changed to:", newUrl);
        loadImage(newUrl);
    }
);
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
    max-width: 100%;
    max-height: 100%;
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
