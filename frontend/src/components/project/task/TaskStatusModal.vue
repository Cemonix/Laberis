<template>
    <div v-if="show" class="modal-overlay" @click="handleOverlayClick">
        <div class="modal-content" @click.stop>
            <div class="modal-header">
                <h3>Change Task Status</h3>
                <button class="close-button" @click="$emit('close')" type="button">
                    <font-awesome-icon :icon="faTimes" />
                </button>
            </div>
            
            <div class="modal-body">
                <div class="task-info">
                    <div class="task-detail">
                        <label>Asset:</label>
                        <span>{{ task?.assetName }}</span>
                    </div>
                    <div class="task-detail">
                        <label>Current Status:</label>
                        <TaskStatusBadge :status="task?.status || TaskStatus.NOT_STARTED"/>
                    </div>
                    <div class="task-detail" v-if="task?.assignedTo">
                        <label>Assigned To:</label>
                        <span>{{ task.assignedTo }}</span>
                    </div>
                </div>
                
                <div class="status-actions">
                    <h4>Available Actions</h4>
                    <div class="action-buttons">
                        <Button
                            v-if="canSuspend"
                            variant="secondary"
                            @click="handleAction(task?.status === TaskStatus.SUSPENDED ? 'unsuspend' : 'suspend')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faPause" />
                            {{ task?.status === TaskStatus.SUSPENDED ? 'Unsuspend' : 'Suspend' }}
                        </Button>
                        
                        <Button
                            v-if="canDefer"
                            variant="secondary"
                            @click="handleAction(task?.status === TaskStatus.DEFERRED ? 'undefer' : 'defer')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faForward" />
                            {{ task?.status === TaskStatus.DEFERRED ? 'Undefer' : 'Defer' }}
                        </Button>
                        
                        <Button
                            v-if="canComplete"
                            variant="primary"
                            @click="handleAction('complete')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faCheck" />
                            Complete Task
                        </Button>
                        
                        <Button
                            v-if="canUncomplete"
                            variant="secondary"
                            @click="handleAction('uncomplete')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faUndo" />
                            Mark as Incomplete
                        </Button>
                        
                        <Button
                            v-if="canReturnForRework"
                            variant="danger"
                            @click="handleAction('return_for_rework')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faArrowLeft" />
                            Return for Rework
                        </Button>
                        
                        <Button
                            v-if="canArchive"
                            variant="danger"
                            @click="handleAction('archive')"
                            :disabled="actionInProgress"
                            class="status-action-button"
                        >
                            <font-awesome-icon :icon="faArchive" />
                            Archive Task
                        </Button>
                        
                    </div>
                    
                    <div v-if="!hasAvailableActions" class="no-actions">
                        <p>No status changes are available for this task in its current state.</p>
                    </div>
                </div>
            </div>
            
            <div class="modal-footer">
                <Button variant="secondary" @click="$emit('close')" :disabled="actionInProgress">
                    Cancel
                </Button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faArchive,
    faArrowLeft,
    faCheck,
    faForward,
    faPause,
    faTimes,
    faUndo
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import TaskStatusBadge from '@/components/project/task/TaskStatusBadge.vue';
import type {TaskTableRow} from '@/services/project/task/task.types';
import {TaskStatus} from '@/services/project/task/task.types';

interface Props {
    show: boolean;
    task: TaskTableRow | null;
    canReview?: boolean;
    canManage?: boolean;
    stageType?: string;  // Current workflow stage type
}

interface Emits {
    close: [];
    action: [actionKey: string, task: TaskTableRow];
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const actionInProgress = ref<boolean>(false);

const canSuspend = computed(() => {
    if (!props.task) return false;
    
    // Can suspend if not completed or archived, or can unsuspend if currently suspended
    if (props.task.status === TaskStatus.SUSPENDED) {
        return true; // Can unsuspend
    }
    
    // Can suspend if not already deferred, completed or archived
    return ![TaskStatus.DEFERRED, TaskStatus.COMPLETED, TaskStatus.ARCHIVED].includes(props.task.status);
});

const canDefer = computed(() => {
    if (!props.task) return false;
    
    // Can defer if not already suspended, completed, or archived
    // Can also undefer if currently deferred
    if (props.task.status === TaskStatus.DEFERRED) {
        return true; // Can undefer
    }
    
    return ![
        TaskStatus.SUSPENDED,
        TaskStatus.COMPLETED,
        TaskStatus.ARCHIVED
    ].includes(props.task.status);
});

const canComplete = computed(() => {
    if (!props.task) return false;
    
    // Can complete tasks that are in progress or ready for work
    const completableStatuses = [
        TaskStatus.IN_PROGRESS,
        TaskStatus.READY_FOR_ANNOTATION,
        TaskStatus.READY_FOR_REVIEW,
        TaskStatus.READY_FOR_COMPLETION
    ];
    
    return completableStatuses.includes(props.task.status);
});

const canUncomplete = computed(() => {
    if (!props.task) return false;
    
    // Can only uncomplete completed tasks, but NOT in completion stage
    // For completion stage, use "Return for Rework" instead
    if (props.stageType === 'COMPLETION') return false;
    
    return props.task.status === TaskStatus.COMPLETED;
});

const canReturnForRework = computed(() => {
    if (!props.task) return false;
    
    // Only reviewers and managers can return tasks for rework
    if (!props.canReview && !props.canManage) return false;
    
    // Return for rework is only available in review and completion stages, not annotation
    if (props.stageType === 'ANNOTATION') return false;
    
    // Can return completed tasks in review and completion stages
    return props.task.status === TaskStatus.COMPLETED;
});

const canArchive = computed(() => {
    if (!props.task) return false;
    
    // Only managers can archive tasks
    if (!props.canManage) return false;
    
    // Can only archive completed tasks in completion stage
    if (props.stageType !== 'COMPLETION') return false;
    
    return props.task.status === TaskStatus.COMPLETED;
});

const hasAvailableActions = computed(() => {
    return canSuspend.value || canDefer.value || canComplete.value || canUncomplete.value || canReturnForRework.value || canArchive.value;
});

const handleAction = async (actionKey: string) => {
    if (!props.task || actionInProgress.value) return;
    
    actionInProgress.value = true;
    
    try {
        emit('action', actionKey, props.task);
    } finally {
        actionInProgress.value = false;
    }
};

const handleOverlayClick = () => {
    if (!actionInProgress.value) {
        emit('close');
    }
};
</script>

<style scoped>
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1rem;
}

.modal-content {
    background: var(--color-white);
    border-radius: 8px;
    max-width: 500px;
    width: 100%;
    max-height: 90vh;
    overflow-y: auto;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem 1.5rem 1rem;
    border-bottom: 1px solid var(--color-gray-300);
}

.modal-header h3 {
    margin: 0;
    color: var(--color-gray-800);
    font-size: 1.25rem;
    font-weight: 600;
}

.close-button {
    background: none;
    border: none;
    color: var(--color-gray-600);
    cursor: pointer;
    padding: 0.5rem;
    border-radius: 4px;
    transition: all 0.2s;
}

.close-button:hover {
    background-color: var(--color-gray-200);
    color: var(--color-gray-800);
}

.modal-body {
    padding: 1.5rem;
}

.task-info {
    margin-bottom: 2rem;
    padding: 1rem;
    background-color: var(--color-gray-100);
    border-radius: 6px;
}

.task-detail {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
}

.task-detail:last-child {
    margin-bottom: 0;
}

.task-detail label {
    font-weight: 500;
    color: var(--color-gray-700);
    font-size: 0.875rem;
}

.task-detail span {
    color: var(--color-gray-800);
    font-size: 0.875rem;
}

.status-actions h4 {
    margin: 0 0 1rem;
    color: var(--color-gray-800);
    font-size: 1rem;
    font-weight: 600;
}

.action-buttons {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.status-action-button {
    justify-content: flex-start;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    text-align: left;
}


.no-actions {
    text-align: center;
    padding: 2rem 1rem;
    color: var(--color-gray-600);
    font-style: italic;
}

.modal-footer {
    padding: 1rem 1.5rem 1.5rem;
    border-top: 1px solid var(--color-gray-300);
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
}

@media (max-width: 480px) {
    .modal-overlay {
        padding: 0.5rem;
    }
    
    .modal-content {
        max-height: 95vh;
    }
    
    .modal-header,
    .modal-body,
    .modal-footer {
        padding-left: 1rem;
        padding-right: 1rem;
    }
}
</style>