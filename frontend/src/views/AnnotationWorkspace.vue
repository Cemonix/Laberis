<template>
    <div class="annotation-workspace-container">
        <div class="workspace-top-bar">
            <div class="workspace-top-bar-left">
                <a href="/home">Home</a>
            </div>
            <div class="workspace-top-bar-center">
                <button>Previous</button>
                <span>122 / 150</span>
                <button>Next</button>
            </div>
            <div class="workspace-top-bar-right">
                <span class="zoom-display">Zoom: {{ zoomPercentageDisplay }}</span>
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
import AnnotationCanvas from "@/components/workspace/AnnotationCanvas.vue";
import { useWorkspaceStore } from '@/stores/workspaceStore';
import ToolsLeftPanel from "@/components/workspace/ToolsLeftPanel.vue";

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
    console.log("[WorkspaceView] Mounted. Project ID:", props.projectId, "Asset ID:", props.assetId);
    await workspaceStore.loadAsset(props.projectId, props.assetId);
    console.log("[WorkspaceView] loadAsset action dispatched. Image URL from store should be updated now.");

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
@use "@/styles/variables.scss" as vars;
@use '@/styles/mixins' as mixins;

.annotation-workspace-container {
    @include mixins.flex-column();
    height: 100%;
    width: 100%;
    background-color: vars.$workspace-container-bg;
    color: vars.$workspace-container-text;
}

.workspace-top-bar {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    grid-gap: vars.$padding-small;
    align-items: center;
    background-color: vars.$workspace-top-bar-bg;
    padding: vars.$padding-small vars.$padding-medium;
    text-align: center;
    border-bottom: 1px solid vars.$workspace-border-color;

    .workspace-top-bar-left {
        justify-self: start;
        @include mixins.flex-start-center();

        a {
            color: vars.$laberis-brand-text;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color vars.$transition-normal;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$primary-blue;
                transition: width vars.$transition-long, left vars.$transition-long;
            }

            &:hover,
            &.router-link-exact-active {
                color: vars.$primary-link-hover-color;
            }

            &:hover::after,
            &.router-link-exact-active::after {
                width: 100%;
                left: 0;
            }
        }
    }
    .workspace-top-bar-center {
        justify-self: center;
        @include mixins.flex-center();
        gap: vars.$padding-small;

        button {
            border: none;
            border-radius: vars.$border-radius-standard;
            color: vars.$button-primary-text;
            background-color: vars.$button-primary-bg;
            padding: vars.$padding-small;
            cursor: pointer;
            &:hover {
                background-color: vars.$button-primary-hover-bg;
            }
        }
    }
    .workspace-top-bar-right {
        justify-self: end;
        @include mixins.flex-end-center();
        font-size: vars.$font-size-medium;

        .zoom-display {
            margin-right: vars.$padding-small;
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
    background-color: vars.$workspace-panel-bg;
    padding: vars.$padding-small;
    border-right: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
  flex-grow: 1;
  display: flex;
  overflow: hidden;
  position: relative;
  padding: vars.$padding-medium;
  background-color: vars.$workspace-layout-bg;
}

.workspace-annotations-right {
    width: 200px;
    background-color: vars.$workspace-panel-bg;
    padding: vars.$padding-small;
    border-left: 1px solid vars.$workspace-border-color;
    flex-shrink: 0;
    overflow-y: auto;
}
</style>
