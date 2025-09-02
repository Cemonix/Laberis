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
                        
                        <!-- Active task actions -->
                        <template v-if="!isTaskCompleted">
                            <Button 
                                v-if="canVetoTask"
                                @click="handleVetoTask"
                                :disabled="!currentTask"
                                variant="danger"
                                class="task-action-btn veto-btn"
                                title="Return task for rework"
                            >
                                <font-awesome-icon :icon="faUndo" />
                                Veto
                            </Button>
                            <Button 
                                @click="handleSuspendTask"
                                :disabled="!currentTask"
                                variant="warning"
                                class="task-action-btn"
                                title="Suspend task (pause work)"
                            >
                                <font-awesome-icon :icon="faPause" />
                            </Button>
                            <Button 
                                @click="handleDeferTask"
                                :disabled="!currentTask"
                                variant="info"
                                class="task-action-btn"
                                title="Defer task (skip for now)"
                            >
                                <font-awesome-icon :icon="faForward" />
                            </Button>
                            <Button 
                                @click="handleCompleteTaskAndNext"
                                :disabled="!canCompleteTask"
                                variant="success"
                                class="task-action-btn complete-btn"
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
import {faPause, faForward, faUndo} from "@fortawesome/free-solid-svg-icons";
import {usePermissions} from "@/composables/usePermissions";
import {PERMISSIONS} from "@/services/auth/permissions.types";
import {useToast} from "@/composables/useToast";

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
const { hasPermissionInProject } = usePermissions();
const { showToast } = useToast();

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
const canVetoTask = computed(() => {
    if (!workspaceStore.canVetoCurrentTask) return false;
    
    // Get project ID from props
    const projectId = parseInt(props.projectId, 10);
    if (isNaN(projectId)) return false;

    // Check if user has the return for rework permission
    return hasPermissionInProject(PERMISSIONS.TASK.RETURN_FOR_REWORK, projectId);
});
const zoomPercentageDisplay = computed(() => {
    const zoomLevel = workspaceStore.zoomLevel;
    return `${(zoomLevel * 100).toFixed(0)}%`;
});

const retryLoading = async () => {
    workspaceStore.clearError();
    const taskId = route.query.taskId as string | undefined;
    await workspaceStore.loadAsset(props.projectId, props.assetId, taskId);
};

/**
 * Auto-assign a task to current user and load it in the workspace
 */
const assignAndLoadTask = async (taskInfo: { projectId: string; assetId: string; taskId: string }, context: string = 'navigation') => {
    const taskId = parseInt(taskInfo.taskId, 10);
    const projectIdNum = parseInt(taskInfo.projectId, 10);
    
    try {
        await workspaceStore.assignAndStartNextTask(projectIdNum, taskId);
    } catch (error) {
        console.error(`Failed to assign task during ${context}:`, error);
        
        showToast(
            'Task Assignment Failed', 
            `Failed to auto-assign the next task. You may not have permission to work on this task. Please go back to Tasks view and manually select a task.`,
            'error',
            { duration: 10000 } // Show for 10 seconds since this is critical
        );
    }
    
    // Load the task in workspace
    await workspaceStore.loadAsset(taskInfo.projectId, taskInfo.assetId, taskInfo.taskId);
    
    // Update URL without triggering route change
    await router.replace({
        name: 'AnnotationWorkspace',
        params: {
            projectId: taskInfo.projectId,
            assetId: taskInfo.assetId
        },
        query: {
            taskId: taskInfo.taskId
        }
    });
};

const handlePreviousTask = async () => {
    // Save current working time before navigating to another task
    await workspaceStore.saveWorkingTimeBeforeUnload();
    
    const navigationInfo = await workspaceStore.navigateToPreviousTask();
    
    if (navigationInfo) {
        await assignAndLoadTask(navigationInfo, 'previous navigation');
    }
};

const showCompletionModal = ref(false);

const handleNextTask = async () => {
    // Save current working time before navigating to another task
    await workspaceStore.saveWorkingTimeBeforeUnload();
    
    const navigationInfo = await workspaceStore.navigateToNextTask();
    if (navigationInfo) {
        await assignAndLoadTask(navigationInfo, 'next navigation');
    } else {
        // At last task - show completion modal
        showCompletionModal.value = true;
    }
};

const handleCompletionModalConfirm = async () => {
    // Save working time before navigating away
    await workspaceStore.saveWorkingTimeBeforeUnload();
    
    const currentTask = workspaceStore.getCurrentTask;
    if (currentTask) {
        router.push({
            name: 'StageTasks',
            params: {
                projectId: props.projectId,
                workflowId: currentTask.workflowId.toString(),
                stageId: currentTask.workflowStageId.toString()
            }
        });
    }
    showCompletionModal.value = false;
};

const handleCompletionModalCancel = () => {
    showCompletionModal.value = false;
};

const handleBackToTasks = async () => {
    // Save working time before navigating away
    await workspaceStore.saveWorkingTimeBeforeUnload();
    
    const task = currentTask.value;
    if (task) {
        router.push({
            name: 'StageTasks',
            params: {
                projectId: props.projectId,
                workflowId: task.workflowId.toString(),
                stageId: task.workflowStageId.toString()
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
            await assignAndLoadTask(nextTaskInfo, 'task completion');
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
                await assignAndLoadTask(nextTaskInfo, 'suspend');
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
                await assignAndLoadTask(nextTaskInfo, 'defer');
            } else {
                showCompletionModal.value = true;
            }
        }
    } catch (error) {
        console.error('Error in handleDeferTask:', error);
    }
};

const handleVetoTask = async () => {
    if (!currentTask.value) return;
    
    try {
        // Optionally prompt for a reason
        const reason = prompt('Please provide a reason for returning this task for rework (optional):');
        
        const success = await workspaceStore.returnCurrentTaskForRework(reason || undefined);
        if (success) {
            // Move to next task after veto
            const nextTaskInfo = await workspaceStore.getNextAvailableTask();
            if (nextTaskInfo) {
                await assignAndLoadTask(nextTaskInfo, 'veto');
            } else {
                showCompletionModal.value = true;
            }
        }
    } catch (error) {
        console.error('Error in handleVetoTask:', error);
    }
};


// Handler to save working time before page unload (refresh, close, navigation)
const handleBeforeUnload = async () => {
    // Save working time before the page is unloaded
    await workspaceStore.saveWorkingTimeBeforeUnload();
};

onMounted(async () => {
    const taskId = route.query.taskId as string | undefined;
    await workspaceStore.loadAsset(props.projectId, props.assetId, taskId);
    
    // Add beforeunload event listener to save working time on page refresh/close
    window.addEventListener('beforeunload', handleBeforeUnload);
});

onUnmounted(() => {
    workspaceStore.cleanupTimer();
    
    // Remove beforeunload event listener
    window.removeEventListener('beforeunload', handleBeforeUnload);
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
        border-radius: 6px;
        font-size: 1rem;
        transition: all 0.2s ease-in-out;
        
        &:hover:not(:disabled) {
            transform: translateY(-1px);
            box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
        }
        
        &.veto-btn {
            width: auto;
            padding: 0.5rem 1rem;
            gap: 0.5rem;
        }

        &.complete-btn {
            width: auto;
            background-color: var(--color-green-500);
            color: var(--color-white);
            font-size: 0.875rem;
            font-weight: 600;
        }
        
        &.complete-btn:hover:not(:disabled) {
            background-color: var(--color-green-600);
            transform: translateY(-1px);
            box-shadow: 0 2px 8px rgba(40, 167, 69, 0.3);
        }
        
        &.complete-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
            transform: none;
            box-shadow: none;
        }
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
