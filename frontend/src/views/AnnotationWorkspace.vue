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
                <span>{{ displayedTime }}</span>
            </div>
        </div>
        <div class="workspace-main-area">
            <div class="workspace-tools-left">Left Tools Panel</div>
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
import { onMounted, computed, ref } from "vue";
import AnnotationCanvas from "@/components/workspace/AnnotationCanvas.vue";
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { Timer } from '@/utils/timer';
import { onUnmounted } from "vue";

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

const labelingTimer = new Timer();
const displayedTime = ref<string>("00:00:00");
let timerInterval: number | null = null;

onMounted(async () => {
    console.log("[WorkspaceView] Mounted. Project ID:", props.projectId, "Asset ID:", props.assetId);
    await workspaceStore.loadAsset(props.projectId, props.assetId);
    console.log("[WorkspaceView] loadAsset action dispatched. Image URL from store should be updated now.");

    // Start the timer
    labelingTimer.start();
    timerInterval = window.setInterval(() => {
        displayedTime.value = labelingTimer.getFormattedElapsedTime();
    }, 1000);

    // TODO: Later, this will be driven by the Pinia store (Step 3.4.1)
    // Example:
    // import { useWorkspaceStore } from '@/stores/workspaceStore';
    // const workspaceStore = useWorkspaceStore();
    // workspaceStore.loadAsset(props.projectId, props.assetId);
    // currentImageUrl.value = computed(() => workspaceStore.currentImageUrl);
});

onUnmounted(() => {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
    labelingTimer.stop();
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
    grid-template-columns: 1fr 1fr 1fr;
    grid-template-areas: "home main timer"; // TODO: Areas are not decided yet
    grid-gap: vars.$padding-small;
    background-color: vars.$workspace-top-bar-bg;
    padding: vars.$padding-small;
    text-align: center;
    border-bottom: 1px solid vars.$workspace-border-color;

    .workspace-top-bar-left {
        grid-area: home;
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
        grid-area: main;
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
        grid-area: timer;
        @include mixins.flex-end-center();
        font-size: vars.$font-size-medium;
    }
}

.workspace-main-area {
    display: flex;
    flex-grow: 1;
    overflow: hidden;
}

.workspace-tools-left {
    width: 200px;
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
