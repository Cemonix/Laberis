<template>
    <div class="annotation-workspace-container">
        <div class="workspace-top-bar">
            <div class="workspace-top-bar-left">
                <router-link to="/home">Home</router-link>
            </div>
            <div class="workspace-top-bar-center">
                <Button>Previous</Button>
                <span>122 / 150</span>
                <Button>Next</Button>
            </div>
            <div class="workspace-top-bar-right">
                <span class="zoom-display"
                    >Zoom: {{ zoomPercentageDisplay }}</span
                >
                <span>{{ displayedTime }}</span>
            </div>
        </div>
        <div class="workspace-main-area">
            <div class="workspace-tools-left">
                <ToolsLeftPanel />
            </div>
            <div class="workspace-canvas-area">
                <AnnotationCanvas :image-url="imageUrlFromStore" />
            </div>
            <div class="workspace-annotations-right">
                Right Annotations/Issues Panel
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, computed } from "vue";
import AnnotationCanvas from "@/components/annotationWorkspace/AnnotationCanvas.vue";
import Button from "@/components/common/Button.vue";
import { useWorkspaceStore } from "@/stores/workspaceStore";
import ToolsLeftPanel from "@/components/annotationWorkspace/ToolsLeftPanel.vue";

const props = defineProps({
    projectId: {
        type: String,
        required: true,
    },
    assetId: {
        type: String,
        required: true,
    },
});

const workspaceStore = useWorkspaceStore();

const imageUrlFromStore = computed(() => workspaceStore.currentImageUrl);
const displayedTime = computed(() => workspaceStore.elapsedTimeDisplay);
const zoomPercentageDisplay = computed(() => {
    const zoomLevel = workspaceStore.zoomLevel;
    return `${(zoomLevel * 100).toFixed(0)}%`;
});

onMounted(async () => {
    console.log(
        "[WorkspaceView] Mounted. Project ID:",
        props.projectId,
        "Asset ID:",
        props.assetId
    );
    await workspaceStore.loadAsset(props.projectId, props.assetId);
    console.log(
        "[WorkspaceView] loadAsset action dispatched. Image URL from store should be updated now."
    );

    // TODO: Later, this will be driven by the Pinia store (Step 3.4.1)
    // Example:
    // import { useWorkspaceStore } from '@/stores/workspaceStore';
    // const workspaceStore = useWorkspaceStore();
    // workspaceStore.loadAsset(props.projectId, props.assetId);
    // currentImageUrl.value = computed(() => workspaceStore.currentImageUrl);
});

onUnmounted(() => {
    workspaceStore.cleanupTimer();
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.annotation-workspace-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    background-color: vars.$color-dark-blue-1;
    color: vars.$color-gray-200;
}

.workspace-top-bar {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    grid-gap: vars.$gap-small;
    align-items: center;
    background-color: vars.$color-dark-blue-1;
    padding: vars.$padding-small vars.$padding-medium;
    text-align: center;
    border-bottom: vars.$border-width solid vars.$color-accent-blue;

    .workspace-top-bar-left {
        display: flex;
        justify-content: flex-start;
        align-items: center;
        justify-self: start;

        a {
            color: vars.$color-gray-200;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color 0.2s ease-in-out;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$color-primary;
                transition: width 0.3s ease, left 0.3s ease;
            }

            &:hover,
            &.router-link-exact-active {
                color: vars.$color-link-hover;
            }

            &:hover::after,
            &.router-link-exact-active::after {
                width: 100%;
                left: 0;
            }
        }
    }
    .workspace-top-bar-center {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: vars.$gap-small;
    }
    .workspace-top-bar-right {
        display: flex;
        justify-content: flex-end;
        align-items: center;
        font-size: vars.$font-size-medium;

        .zoom-display {
            margin-right: vars.$margin-small;
        }
    }
}

.workspace-main-area {
    display: flex;
    flex-grow: 1;
    overflow: hidden;
}

.workspace-tools-left {
    width: auto;
    min-width: 60px;
    background-color: vars.$color-dark-blue-2;
    padding: vars.$padding-small;
    border-right: vars.$border-width solid vars.$color-accent-blue;
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    overflow: hidden;
    position: relative;
    padding: vars.$padding-medium;
    background-color: vars.$color-dark-blue-3;
}

.workspace-annotations-right {
    width: 200px;
    background-color: vars.$color-dark-blue-2;
    padding: vars.$padding-small;
    border-left: vars.$border-width solid vars.$color-accent-blue;
    flex-shrink: 0;
    overflow-y: auto;
}
</style>
