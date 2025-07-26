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
                    <router-link class="nav-link underline-animation" to="/home">Home</router-link>
                </div>
                <div class="workspace-top-bar-center">
                    <Button 
                        @click="handlePreviousTask"
                        :disabled="!canNavigateToPrevious"
                    >
                        Previous
                    </Button>
                    <span>{{ taskNavigationInfo.current }} / {{ taskNavigationInfo.total }}</span>
                    <Button 
                        @click="handleNextTask"
                        :disabled="!canNavigateToNext"
                    >
                        {{ canNavigateToNext ? 'Next' : 'All Done' }}
                    </Button>
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
                    <AnnotationsPanel />
                </div>
            </div>
        </template>
        <ModalWindow
            :is-open="showCompletionModal"
            title="Workflow Complete"
            @close="handleCompletionModalCancel"
            hide-footer
        >
            <template>
                <p>You have reached the last task in this workflow stage. Would you like to return to the tasks view?</p>
            </template>
            <template #footer>
                <Button @click="handleCompletionModalCancel">Stay</Button>
                <Button @click="handleCompletionModalConfirm" variant="primary">Go to Tasks</Button>
            </template>
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, onUnmounted, ref} from "vue";
import AnnotationCanvas from "@/components/annotationWorkspace/AnnotationCanvas.vue";
import Button from "@/components/common/Button.vue";
import {useWorkspaceStore} from "@/stores/workspaceStore";
import ToolsLeftPanel from "@/components/annotationWorkspace/ToolsLeftPanel.vue";
import AnnotationsPanel from "@/components/annotationWorkspace/AnnotationsPanel.vue";
import {useRoute, useRouter} from "vue-router";
import ModalWindow from "@/components/common/modal/ModalWindow.vue";

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
const route = useRoute();
const router = useRouter();

const isLoading = computed(() => workspaceStore.getLoadingState);
const error = computed(() => workspaceStore.getError);
const imageUrlFromStore = computed(() => workspaceStore.currentImageUrl);
const displayedTime = computed(() => workspaceStore.elapsedTimeDisplay);
const taskNavigationInfo = computed(() => workspaceStore.getTaskNavigationInfo);
const canNavigateToPrevious = computed(() => workspaceStore.canNavigateToPrevious);
const canNavigateToNext = computed(() => workspaceStore.canNavigateToNext);
const zoomPercentageDisplay = computed(() => {
    const zoomLevel = workspaceStore.zoomLevel;
    return `${(zoomLevel * 100).toFixed(0)}%`;
});

const retryLoading = async () => {
    workspaceStore.clearError();
    const taskId = route.query.taskId as string | undefined;
    await workspaceStore.loadAsset(props.projectId, props.assetId, taskId);
};

const handlePreviousTask = async () => {
    const navigationInfo = await workspaceStore.navigateToPreviousTask();
    
    if (navigationInfo) {
        // Directly load the new asset instead of using router.push to avoid same-route issues
        await workspaceStore.loadAsset(navigationInfo.projectId, navigationInfo.assetId, navigationInfo.taskId);
        
        // Update the URL without triggering a route change
        await router.replace({
            name: 'AnnotationWorkspace',
            params: {
                projectId: navigationInfo.projectId,
                assetId: navigationInfo.assetId
            },
            query: {
                taskId: navigationInfo.taskId
            }
        });
    }
};

const showCompletionModal = ref(false);

const handleNextTask = async () => {
    const navigationInfo = await workspaceStore.navigateToNextTask();
    if (navigationInfo) {
        // Directly load the new asset instead of using router.push to avoid same-route issues
        await workspaceStore.loadAsset(navigationInfo.projectId, navigationInfo.assetId, navigationInfo.taskId);
        
        // Update the URL without triggering a route change
        await router.replace({
            name: 'AnnotationWorkspace',
            params: {
                projectId: navigationInfo.projectId,
                assetId: navigationInfo.assetId
            },
            query: {
                taskId: navigationInfo.taskId
            }
        });
    } else {
        // At last task - show completion modal
        showCompletionModal.value = true;
    }
};

const handleCompletionModalConfirm = () => {
    const currentTask = workspaceStore.getCurrentTask;
    if (currentTask) {
        router.push({
            name: 'StageTasks',
            params: {
                projectId: props.projectId,
                workflowId: currentTask.workflowId.toString(),
                stageId: currentTask.currentWorkflowStageId.toString()
            }
        });
    }
    showCompletionModal.value = false;
};

const handleCompletionModalCancel = () => {
    showCompletionModal.value = false;
};

onMounted(async () => {
    const taskId = route.query.taskId as string | undefined;
    console.log(
        "[WorkspaceView] Mounted. Project ID:",
        props.projectId,
        "Asset ID:",
        props.assetId,
        "Task ID:",
        taskId
    );
    await workspaceStore.loadAsset(props.projectId, props.assetId, taskId);
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
.annotation-workspace-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    background-color: var(--color-dark-blue-1);
    color: var(--color-gray-200);
}

.workspace-top-bar {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    grid-gap: 0.5rem;
    align-items: center;
    background-color: var(--color-dark-blue-1);
    padding: 0.5rem 1rem;
    text-align: center;
    border-bottom: 1px solid var(--color-accent-blue);

    .workspace-top-bar-left {
        display: flex;
        justify-content: flex-start;
        align-items: center;
        justify-self: start;

        a {
            color: var(--color-gray-200);
            text-decoration: none;
            padding: 0.5rem 0;
        }
    }
    .workspace-top-bar-center {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 0.5rem;
    }
    .workspace-top-bar-right {
        display: flex;
        justify-content: flex-end;
        align-items: center;
        font-size: 1rem;

        .zoom-display {
            margin-right: 0.5rem;
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
    background-color: var(--color-dark-blue-2);
    padding: 0.5rem;
    border-right: 1px solid var(--color-accent-blue);
    flex-shrink: 0;
    overflow-y: auto;
}

.workspace-canvas-area {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    overflow: hidden;
    position: relative;
    padding: 1rem;
    background-color: var(--color-dark-blue-3);
}

.workspace-annotations-right {
    width: 300px;
    background-color: var(--color-dark-blue-2);
    padding: 0;
    border-left: 1px solid var(--color-accent-blue);
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
    background-color: rgba(var(--color-black), 0.8);
    z-index: 1000;
}

.loading-content,
.error-content {
    display: grid;
    grid-template-columns: 1fr 1fr;
    text-align: center;
    align-items: center;
    justify-content: center;
    color: var(--color-white);
    padding: 1.5rem;
    border-radius: 4px;
    background-color: var(--color-dark-blue-2);
    border: 1px solid var(--color-accent-blue);
}

.loading-spinner {
    width: 40px;
    height: 40px;
    border: 4px solid var(--color-gray-600);
    border-top: 4px solid var(--color-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.error-content {
    h3 {
        color: var(--color-error);
        margin-bottom: 1rem;
        grid-column: span 2;
    }

    p {
        margin-bottom: 1rem;
        color: var(--color-gray-300);
        grid-column: span 2;
    }

    Button {
        margin-inline: 1rem;
    }

    .error-link {
        color: var(--color-primary);
        text-decoration: none;
        
        &:hover {
            color: var(--color-link-hover);
            text-decoration: underline;
        }
    }
}

.asset-name {
    margin-left: 1rem;
    color: var(--color-gray-300);
    font-style: italic;
}
</style>
