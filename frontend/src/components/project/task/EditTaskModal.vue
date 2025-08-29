<template>
    <div v-if="show" class="modal-overlay" @click="handleOverlayClick">
        <div class="modal-content" @click.stop>
            <div class="modal-header">
                <h3>{{ modalTitle }}</h3>
                <button class="close-button" @click="handleClose" type="button">
                    <font-awesome-icon :icon="faTimes" />
                </button>
            </div>
            
            <div class="modal-body">
                <div class="edit-task-form">
                    <div class="form-section">
                        <h3>Task Details</h3>
                        
                        <div class="form-group">
                            <label for="priority">Priority *</label>
                            <select 
                                id="priority"
                                v-model="formData.priority"
                                class="form-select"
                                :disabled="isSaving"
                            >
                                <option value="1">Low</option>
                                <option value="2">Medium</option>
                                <option value="3">High</option>
                            </select>
                        </div>

                        <div class="form-group">
                            <label for="dueDate">Due Date</label>
                            <input
                                id="dueDate"
                                type="date"
                                v-model="formData.dueDate"
                                class="form-input"
                                :disabled="isSaving"
                            />
                            <small class="form-help">Leave empty for no due date</small>
                        </div>
                    </div>

                    <div class="form-section" v-if="canAssignTasks">
                        <h3>Assignment</h3>
                        
                        <div class="form-group">
                            <label for="assignedUser">Assigned To</label>
                            <select 
                                id="assignedUser"
                                v-model="formData.assignedToEmail"
                                class="form-select"
                                :disabled="isSaving || isLoadingUsers"
                            >
                                <option value="">Unassigned</option>
                                <option 
                                    v-for="user in availableUsers" 
                                    :key="user.email" 
                                    :value="user.email"
                                >
                                    {{ user.email }} ({{ user.role }})
                                </option>
                            </select>
                            <small class="form-help" v-if="isLoadingUsers">Loading team members...</small>
                        </div>
                    </div>

                    <div class="task-info">
                        <h3>Task Information</h3>
                        <div class="info-grid">
                            <div class="info-item asset-name">
                                <strong>Asset:</strong> 
                                <span class="asset-name-text" :title="task?.assetName || 'Unknown'">
                                    {{ task?.assetName || 'Unknown' }}
                                </span>
                            </div>
                            <div class="info-item">
                                <strong>Current Status:</strong> 
                                <TaskStatusBadge :status="task?.status || TaskStatus.NOT_STARTED" />
                            </div>
                            <div class="info-item">
                                <strong>Created:</strong> {{ formatDate(task?.createdAt) }}
                            </div>
                            <div class="info-item" v-if="task?.completedAt">
                                <strong>Completed:</strong> {{ formatDate(task.completedAt) }}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="modal-footer">
                <Button
                    variant="secondary"
                    @click="handleClose"
                    :disabled="isSaving"
                >
                    Cancel
                </Button>
                <Button
                    variant="primary"
                    @click="handleSave"
                    :disabled="isSaving || !isFormValid"
                    :loading="isSaving"
                >
                    Save Changes
                </Button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faTimes } from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import TaskStatusBadge from './TaskStatusBadge.vue';
import type { TaskTableRow, UpdateTaskRequest } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';
import type { ProjectMember } from '@/services/project/projectMember.types';
import { taskService } from '@/services/project';
import { useProjectStore } from '@/stores/projectStore';
import { useErrorHandler } from '@/composables/useErrorHandler';
import { AppLogger } from '@/core/logger/logger';

interface Props {
    show: boolean;
    task: TaskTableRow | null;
    projectId: number;
    canAssignTasks?: boolean;
}

interface Emits {
    (e: 'close'): void;
    (e: 'saved', task: TaskTableRow): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const projectStore = useProjectStore();
const { handleError } = useErrorHandler();
const logger = AppLogger.createComponentLogger('EditTaskModal');

// Form state
const formData = ref<{
    priority: number;
    dueDate: string;
    assignedToEmail: string;
}>({
    priority: 1,
    dueDate: '',
    assignedToEmail: ''
});

const isSaving = ref(false);
const isLoadingUsers = ref(false);
const availableUsers = ref<ProjectMember[]>([]);

// Computed properties
const modalTitle = computed(() => {
    if (!props.task) return 'Edit Task';
    return `Edit Task - ${props.task.assetName}`;
});

const isFormValid = computed(() => {
    return formData.value.priority >= 1 && formData.value.priority <= 3;
});

// Reset form when task changes
watch(() => props.task, (newTask) => {
    if (newTask) {
        formData.value = {
            priority: newTask.priority || 1,
            dueDate: newTask.dueDate || '',
            assignedToEmail: newTask.assignedTo || ''
        };
    }
}, { immediate: true });

// Load available users when modal opens
watch(() => props.show, (isVisible) => {
    if (isVisible && props.canAssignTasks) {
        loadAvailableUsers();
    }
});

const loadAvailableUsers = async () => {
    if (!props.projectId) return;
    
    isLoadingUsers.value = true;
    try {
        // Get project members from store
        await projectStore.setCurrentProject(props.projectId);
        
        // Get team members from store
        availableUsers.value = projectStore.activeMembers;
        
        // Try to find and set the currently assigned user
        if (props.task?.assignedTo) {
            const assignedMember = projectStore.activeMembers.find((member: ProjectMember) => 
                member.email === props.task?.assignedTo
            );
            if (assignedMember) {
                formData.value.assignedToEmail = assignedMember.email;
            }
        }
        
        logger.info(`Loaded ${availableUsers.value.length} available users`);
    } catch (error) {
        logger.error('Failed to load available users:', error);
        handleError(error, 'Failed to load team members');
    } finally {
        isLoadingUsers.value = false;
    }
};

const handleSave = async () => {
    if (!props.task || !isFormValid.value) {
        return;
    }

    isSaving.value = true;
    try {
        logger.info('Saving task changes', { 
            taskId: props.task.id, 
            changes: formData.value
        });

        const updates: UpdateTaskRequest = {
            priority: formData.value.priority,
            assignedToEmail: formData.value.assignedToEmail || null
        };

        // Only include dueDate if it has a value
        if (formData.value.dueDate) {
            updates.dueDate = formData.value.dueDate;
        }

        const updatedTask = await taskService.updateTask(
            props.projectId, 
            props.task.id, 
            updates
        );

        logger.info('Task updated successfully', { taskId: props.task.id });

        // Create updated TaskTableRow for the parent component
        const updatedTaskRow: TaskTableRow = {
            ...props.task,
            priority: updatedTask.priority,
            dueDate: updatedTask.dueDate,
            assignedTo: updatedTask.assignedToEmail || undefined
        };

        emit('saved', updatedTaskRow);
    } catch (error) {
        logger.error('Failed to update task:', error);
        handleError(error, 'Failed to save task changes');
    } finally {
        isSaving.value = false;
    }
};

const handleClose = () => {
    emit('close');
};

const handleOverlayClick = () => {
    if (!isSaving.value) {
        handleClose();
    }
};

const formatDate = (dateString?: string): string => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleString();
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
    border-radius: 0.5rem;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1), 0 1px 3px rgba(0, 0, 0, 0.08);
    max-width: 600px;
    width: 100%;
    max-height: 85vh;
    overflow-y: auto;
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
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
    font-size: 1.25rem;
    color: var(--color-gray-500);
    cursor: pointer;
    padding: 0.25rem;
    border-radius: 0.25rem;
    transition: color 0.2s, background-color 0.2s;
}

.close-button:hover {
    color: var(--color-gray-700);
    background-color: var(--color-gray-100);
}

.modal-body {
    padding: 1.5rem;
}

.modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 0.75rem;
    padding: 1.5rem;
    border-top: 1px solid var(--color-gray-300);
    background-color: var(--color-gray-50);
    border-radius: 0 0 0.5rem 0.5rem;
}

.edit-task-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.form-section {
    padding: 1rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 0.5rem;
    background-color: var(--color-gray-50);
}

.form-section h3 {
    margin: 0 0 1rem 0;
    color: var(--color-gray-800);
    font-size: 1rem;
    font-weight: 600;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.form-group:last-child {
    margin-bottom: 0;
}

.form-group label {
    font-weight: 500;
    color: var(--color-gray-700);
    font-size: 0.875rem;
}

.form-input,
.form-select {
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 0.375rem;
    font-size: 0.875rem;
    transition: border-color 0.2s;
}

.form-input:focus,
.form-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 2px var(--color-primary-light);
}

.form-input:disabled,
.form-select:disabled {
    background-color: var(--color-gray-100);
    color: var(--color-gray-500);
    cursor: not-allowed;
}

.form-help {
    color: var(--color-gray-600);
    font-size: 0.75rem;
    font-style: italic;
}

.task-info {
    padding: 1rem;
    background-color: var(--color-gray-100);
    border-radius: 0.5rem;
    border: 1px solid var(--color-gray-300);
}

.task-info h3 {
    margin: 0 0 1rem 0;
    color: var(--color-gray-800);
    font-size: 1rem;
    font-weight: 600;
}

.info-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 0.75rem;
}

.info-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
}

.info-item strong {
    color: var(--color-gray-700);
    font-weight: 500;
}

.asset-name {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
}

.asset-name-text {
    word-break: break-all;
    overflow-wrap: break-word;
    max-width: 100%;
    line-height: 1.3;
}

@media (max-width: 600px) {
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
    
    .info-grid {
        grid-template-columns: 1fr;
    }
    
    .modal-footer {
        flex-direction: column;
    }
}
</style>