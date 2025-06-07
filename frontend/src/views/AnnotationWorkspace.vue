<template>
    <div class="annotation-workspace-container">
        <div class="workspace-top-bar">
            <div class="workspace-top-bar-left">
                <a href="/home">Home</a>
            </div>
            <div class="workspace-top-bar-center">
                <button class="btn btn-primary">Previous</button>
                <span>122 / 150</span>
                <button class="btn btn-primary">Next</button>
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
@use "@/styles/mixins" as mixins;

.annotation-workspace-container {
    @include mixins.flex-column();
    height: 100%;
    width: 100%;
    background-color: vars.$ws-layout-bg;
    color: vars.$ws-layout-text;
}

.workspace-top-bar {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    grid-gap: vars.$padding-small;
    align-items: center;
    background-color: vars.$ws-layout-bg;
    padding: vars.$padding-small vars.$padding-medium;
    text-align: center;
    border-bottom: vars.$border-width solid vars.$ws-border;

    .workspace-top-bar-left {
        @include mixins.flex-start-center();
        justify-self: start;

        a {
            color: vars.$ws-layout-text;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color vars.$transition-normal-ease-in-out;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$color-primary;
                transition: width vars.$transition-slow-ease, left vars.$transition-slow-ease;
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
        @include mixins.flex-center($gap: vars.$padding-small);
    }
    .workspace-top-bar-right {
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
    background-color: vars.$ws-panel-bg;
    padding: vars.$padding-small;
    border-right: vars.$border-width solid vars.$ws-border;
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
    @include mixins.flex-column();
    flex-grow: 1;
    overflow: hidden;
    position: relative;
    padding: vars.$padding-medium;
    background-color: vars.$ws-canvas-bg;
}

.workspace-annotations-right {
    width: 200px;
    background-color: vars.$ws-panel-bg;
    padding: vars.$padding-small;
    border-left: vars.$border-width solid vars.$ws-border;
    flex-shrink: 0;
    overflow-y: auto;
}
</style>
