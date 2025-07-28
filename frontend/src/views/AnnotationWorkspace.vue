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
                    <button 
                        @click="handleBackToTasks"
                        class="nav-link back-to-tasks-btn"
                        title="Return to tasks view"
                    >
                        ← Back to Tasks
                    </button>
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
                    <div class="task-actions">
                        <div v-if="isTaskCompleted" class="task-preview-indicator">
                            <span class="preview-badge">📋 Preview Only</span>
                            <small>This task is completed</small>
                        </div>
                        <template v-else>
                            <button 
                                @click="handleSuspendTask"
                                :disabled="!currentTask"
                                class="task-action-btn suspend-btn"
                                title="Suspend task (pause work)"
                            >
                                <font-awesome-icon :icon="faPause" />
                            </button>
                            <button 
                                @click="handleDeferTask"
                                :disabled="!currentTask"
                                class="task-action-btn defer-btn"
                                title="Defer task (skip for now)"
                            >
                                <font-awesome-icon :icon="faForward" />
                            </button>
                            <Button 
                                @click="handleCompleteTaskAndNext"
                                :disabled="!canCompleteTask"
                                variant="success"
                                class="complete-btn"
                                title="Complete annotation and move to next task"
                            >
                                Complete & Next
                            </Button>
                        </template>
                    </div>
                </div>
                <div class="workspace-top-bar-right">
                    <span class="zoom-display">Zoom: {{ zoomPercentageDisplay }}</span>
                    <span>{{ displayedTime }}</span>
                </div>
            </div>
            <div class="workspace-main-area">
                <div class="workspace-tools-left">
                    <WorkspaceSidebar />
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
            title="All Tasks Complete"
            @close="handleCompletionModalCancel"
        >
            <div class="completion-modal-content">
                <p>🎉 Great work! You have completed all available tasks in this workflow stage.</p>
                <p>You can either stay in the workspace or return to the tasks view to see your progress.</p>
            </div>
            <template #footer>
                <Button @click="handleCompletionModalCancel">Stay in Workspace</Button>
                <Button @click="handleCompletionModalConfirm" variant="primary">Back to Tasks</Button>
            </template>
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, onUnmounted, ref} from "vue";
import AnnotationCanvas from "@/components/annotationWorkspace/AnnotationCanvas.vue";
import Button from "@/components/common/Button.vue";
import {useWorkspaceStore} from "@/stores/workspaceStore";
import WorkspaceSidebar from "@/components/annotationWorkspace/WorkspaceSidebar.vue";
import AnnotationsPanel from "@/components/annotationWorkspace/AnnotationsPanel.vue";
import {useRoute, useRouter} from "vue-router";
import ModalWindow from "@/components/common/modal/ModalWindow.vue";
import {FontAwesomeIcon} from "@fortawesome/vue-fontawesome";
import {faPause, faForward} from "@fortawesome/free-solid-svg-icons";

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
const canCompleteTask = computed(() => workspaceStore.canCompleteCurrentTask);
const currentTask = computed(() => workspaceStore.getCurrentTask);
const isTaskCompleted = computed(() => workspaceStore.isTaskCompleted);
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

const handleBackToTasks = () => {
    const task = currentTask.value;
    if (task) {
        router.push({
            name: 'StageTasks',
            params: {
                projectId: props.projectId,
                workflowId: task.workflowId.toString(),
                stageId: task.currentWorkflowStageId.toString()
            }
        });
    } else {
        // Fallback to project view if no current task
        router.push({
            name: 'ProjectDashboard',
            params: {
                projectId: props.projectId
            }
        });
    }
};

const handleCompleteTaskAndNext = async () => {
    try {
        const success = await workspaceStore.completeAndMoveToNextTask();
        if (!success) {
            // Error handling is already done in the store
            return;
        }
        
        // Try to get next available task automatically
        const nextTaskInfo = await workspaceStore.getNextAvailableTask();
        if (nextTaskInfo) {
            // Load the next task seamlessly without leaving the workspace
            await workspaceStore.loadAsset(nextTaskInfo.projectId, nextTaskInfo.assetId, nextTaskInfo.taskId);
            
            // Update URL without triggering route change
            await router.replace({
                name: 'AnnotationWorkspace',
                params: {
                    projectId: nextTaskInfo.projectId,
                    assetId: nextTaskInfo.assetId
                },
                query: {
                    taskId: nextTaskInfo.taskId
                }
            });
        } else {
            // No more tasks available - show completion modal or go back to tasks view
            showCompletionModal.value = true;
        }
    } catch (error) {
        console.error('Error in handleCompleteTaskAndNext:', error);
    }
};

const handleSuspendTask = async () => {
    if (!currentTask.value) return;
    
    try {
        const success = await workspaceStore.suspendCurrentTask();
        if (success) {
            // Move to next task after suspending
            const nextTaskInfo = await workspaceStore.getNextAvailableTask();
            if (nextTaskInfo) {
                await workspaceStore.loadAsset(nextTaskInfo.projectId, nextTaskInfo.assetId, nextTaskInfo.taskId);
                await router.replace({
                    name: 'AnnotationWorkspace',
                    params: {
                        projectId: nextTaskInfo.projectId,
                        assetId: nextTaskInfo.assetId
                    },
                    query: {
                        taskId: nextTaskInfo.taskId
                    }
                });
            } else {
                showCompletionModal.value = true;
            }
        }
    } catch (error) {
        console.error('Error in handleSuspendTask:', error);
    }
};

const handleDeferTask = async () => {
    if (!currentTask.value) return;
    
    try {
        const success = await workspaceStore.deferCurrentTask();
        if (success) {
            // Move to next task after deferring
            const nextTaskInfo = await workspaceStore.getNextAvailableTask();
            if (nextTaskInfo) {
                await workspaceStore.loadAsset(nextTaskInfo.projectId, nextTaskInfo.assetId, nextTaskInfo.taskId);
                await router.replace({
                    name: 'AnnotationWorkspace',
                    params: {
                        projectId: nextTaskInfo.projectId,
                        assetId: nextTaskInfo.assetId
                    },
                    query: {
                        taskId: nextTaskInfo.taskId
                    }
                });
            } else {
                showCompletionModal.value = true;
            }
        }
    } catch (error) {
        console.error('Error in handleDeferTask:', error);
    }
};


onMounted(async () => {
    const taskId = route.query.taskId as string | undefined;
    await workspaceStore.loadAsset(props.projectId, props.assetId, taskId);
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
        
        .back-to-tasks-btn {
            background: none;
            border: none;
            color: var(--color-gray-200);
            text-decoration: none;
            padding: 0.5rem 0;
            cursor: pointer;
            font-size: 1rem;
            transition: color 0.2s ease-in-out;
            
            &:hover {
                color: var(--color-blue-400);
            }
        }
    }
    .workspace-top-bar-center {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
    }
    
    .task-actions {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-left: 1rem;
        padding-left: 1rem;
        border-left: 1px solid var(--color-accent-blue);
    }
    
    .task-action-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border: none;
        border-radius: 6px;
        cursor: pointer;
        font-size: 1rem;
        transition: all 0.2s ease-in-out;
        
        &:disabled {
            opacity: 0.4;
            cursor: not-allowed;
        }
        
        &:hover:not(:disabled) {
            transform: translateY(-1px);
            box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
        }
    }
    
    .suspend-btn {
        background-color: var(--color-yellow-500);
        color: var(--color-white);
        
        &:hover:not(:disabled) {
            background-color: var(--color-yellow-600);
        }
    }
    
    .defer-btn {
        background-color: var(--color-turquoise-500);
        color: var(--color-white);
        
        &:hover:not(:disabled) {
            background-color: var(--color-turquoise-600);
        }
    }
    
    .complete-btn {
        background-color: var(--color-green-500);
        color: var(--color-white);
        font-size: 0.875rem;
        font-weight: 600;
        margin-left: 0.5rem;
    }
    
    .complete-btn:hover:not(:disabled) {
        background-color: var(--color-green-600);
        transform: translateY(-1px);
        box-shadow: 0 2px 8px rgba(40, 167, 69, 0.3);
    }
    
    .complete-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
        transform: none;
        box-shadow: none;
    }
    
    .task-preview-indicator {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.25rem;
        padding: 0.5rem 1rem;
        background-color: var(--color-blue-100);
        border: 1px solid var(--color-blue-300);
        border-radius: 6px;
        margin-left: 0.5rem;
    }
    
    .preview-badge {
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--color-blue-700);
    }
    
    .task-preview-indicator small {
        font-size: 0.75rem;
        color: var(--color-blue-600);
        margin: 0;
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

.completion-modal-content {
    padding: 1rem 0;
    text-align: center;
}

.completion-modal-content p {
    margin-bottom: 1rem;
    line-height: 1.5;
}

.completion-modal-content p:first-child {
    font-size: 1.1rem;
    font-weight: 600;
    color: var(--color-green-600);
}
</style>
