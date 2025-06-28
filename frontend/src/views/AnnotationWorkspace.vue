<template>
    <div class="annotation-workspace-container">
        <!-- Loading overlay -->
        <div v-if="isLoading" class="loading-overlay">
            <div class="loading-content">
                <div class="loading-spinner"></div>
                <p>Loading asset...</p>
            </div>
        </div>

        <!-- Error overlay -->
        <div v-else-if="error" class="error-overlay">
            <div class="error-content">
                <h3>Failed to load asset</h3>
                <p>{{ error }}</p>
                <Button @click="retryLoading">Retry</Button>
                <router-link to="/home" class="error-link">Go back to Home</router-link>
            </div>
        </div>

        <!-- Main workspace content -->
        <template v-else>
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
                    <LabelsPanel />
                </div>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, computed } from "vue";
import AnnotationCanvas from "@/components/annotationWorkspace/AnnotationCanvas.vue";
import Button from "@/components/common/Button.vue";
import { useWorkspaceStore } from "@/stores/workspaceStore";
import ToolsLeftPanel from "@/components/annotationWorkspace/ToolsLeftPanel.vue";
import LabelsPanel from "@/components/annotationWorkspace/LabelsPanel.vue";

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

const isLoading = computed(() => workspaceStore.getLoadingState);
const error = computed(() => workspaceStore.getError);
const imageUrlFromStore = computed(() => workspaceStore.currentImageUrl);
const displayedTime = computed(() => workspaceStore.elapsedTimeDisplay);
const zoomPercentageDisplay = computed(() => {
    const zoomLevel = workspaceStore.zoomLevel;
    return `${(zoomLevel * 100).toFixed(0)}%`;
});

const retryLoading = async () => {
    workspaceStore.clearError();
    await workspaceStore.loadAsset(props.projectId, props.assetId);
};

onMounted(async () => {
    console.log(
        "[WorkspaceView] Mounted. Project ID:",
        props.projectId,
        "Asset ID:",
        props.assetId
    );
    await workspaceStore.loadAsset(props.projectId, props.assetId);
    console.log(
        "[WorkspaceView] loadAsset action dispatched. Image URL from store should be updated now.",
        "Image URL:",
        workspaceStore.currentImageUrl
    );
});

onUnmounted(() => {
    workspaceStore.cleanupTimer();
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;
@use "@/styles/mixins/underline-animation" as mixins;

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

            @include mixins.underline-animation();
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
    padding: 0;
    border-left: vars.$border-width solid vars.$color-accent-blue;
    flex-shrink: 0;
    overflow-y: auto;
}

.loading-overlay,
.error-overlay {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    background-color: rgba(vars.$color-black, 0.8);
    z-index: 1000;
}

.loading-content,
.error-content {
    display: grid;
    grid-template-columns: 1fr 1fr;
    text-align: center;
    align-items: center;
    justify-content: center;
    color: vars.$color-white;
    padding: vars.$padding-large;
    border-radius: vars.$border-radius-md;
    background-color: vars.$color-dark-blue-2;
    border: 1px solid vars.$color-accent-blue;
}

.loading-spinner {
    width: 40px;
    height: 40px;
    border: 4px solid vars.$color-gray-600;
    border-top: 4px solid vars.$color-primary;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto vars.$margin-medium;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.error-content {
    h3 {
        color: vars.$color-error;
        margin-bottom: vars.$margin-medium;
        grid-column: span 2;
    }

    p {
        margin-bottom: vars.$margin-medium;
        color: vars.$color-gray-300;
        grid-column: span 2;
    }

    Button {
        margin-inline: vars.$margin-medium;
    }

    .error-link {
        color: vars.$color-primary;
        text-decoration: none;
        
        &:hover {
            color: vars.$color-link-hover;
            text-decoration: underline;
        }
    }
}

.asset-name {
    margin-left: vars.$margin-medium;
    color: vars.$color-gray-300;
    font-style: italic;
}
</style>
