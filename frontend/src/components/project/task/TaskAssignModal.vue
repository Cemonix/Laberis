<template>
    <ModalWindow
        :is-open="show"
        title="Assign Task"
        @close="handleClose"
    >
        <div class="assign-modal-content">
            <div v-if="task" class="task-info">
                <p class="task-name">
                    <strong>{{ task.assetName }}</strong>
                </p>
                <p class="current-assignee">
                    Currently assigned to: 
                    <span v-if="task.assignedTo" class="assigned">{{ task.assignedTo }}</span>
                    <span v-else class="unassigned">Unassigned</span>
                </p>
            </div>

            <div class="assignment-section">
                <label class="form-label">
                    Select team member:
                    <span v-if="isAssignmentRestricted" class="restriction-note">
                        (Reviewers can only assign tasks to themselves)
                    </span>
                </label>
                <select 
                    v-model="selectedMember" 
                    class="form-select"
                    :disabled="isAssigning"
                >
                    <option value="">-- Select a team member --</option>
                    <option 
                        v-for="member in availableMembers" 
                        :key="member.email"
                        :value="member.email"
                    >
                        {{ member.email }}
                    </option>
                    <option v-if="canUnassign" value="unassign">🚫 Unassign task</option>
                </select>
            </div>

            <div v-if="error" class="error-message">
                {{ error }}
            </div>
        </div>

        <template #footer>
            <div class="modal-actions">
                <Button variant="secondary" @click="handleClose" :disabled="isAssigning">
                    Cancel
                </Button>
                <Button 
                    variant="primary" 
                    @click="handleAssign" 
                    :disabled="!selectedMember || isAssigning"
                    :loading="isAssigning"
                >
                    Assign
                </Button>
            </div>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import Button from '@/components/common/Button.vue';
import type { TaskTableRow } from '@/types/task';
import type { ProjectMember } from '@/types/projectMember/projectMember';
import { useProjectStore } from '@/stores/projectStore';
import { useAuthStore } from '@/stores/authStore';
import { taskService } from '@/services/api/projects';
import { AppLogger } from '@/utils/logger';
import type { ProjectRole } from '@/types/project/project';

interface Props {
    show: boolean;
    task: TaskTableRow | null;
    projectId: number;
}

interface Emits {
    (e: 'close'): void;
    (e: 'assigned', updatedTask: TaskTableRow): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const projectStore = useProjectStore();
const authStore = useAuthStore();
const logger = AppLogger.createComponentLogger('TaskAssignModal');

const selectedMember = ref<string>('');
const isAssigning = ref<boolean>(false);
const error = ref<string>('');

// Get current user's role in the project
const currentUserRole = computed((): ProjectRole | null => {
    const currentUserEmail = authStore.currentUser?.email;
    if (!currentUserEmail) return null;
    
    const currentUserMember = projectStore.teamMembers?.find(
        member => member.email === currentUserEmail
    );
    return currentUserMember?.role || null;
});

// Get available members based on current user's role
const availableMembers = computed((): ProjectMember[] => {
    const allMembers = projectStore.teamMembers || [];
    const userRole = currentUserRole.value;
    const currentUserEmail = authStore.currentUser?.email;
    
    // MANAGER can assign to any project member
    if (userRole === 'MANAGER') {
        return allMembers;
    }
    
    // REVIEWER can only assign to themselves
    if (userRole === 'REVIEWER') {
        return allMembers.filter(member => member.email === currentUserEmail);
    }
    
    // Other roles (ANNOTATOR, VIEWER) should not have task:assign permission anyway
    // but return empty list to be safe
    return [];
});

// Check if unassign option should be available
const canUnassign = computed((): boolean => {
    const userRole = currentUserRole.value;
    // MANAGER and REVIEWER can unassign tasks
    return userRole === 'MANAGER' || userRole === 'REVIEWER';
});

// Check if assignment is restricted (for UI feedback)
const isAssignmentRestricted = computed((): boolean => {
    return currentUserRole.value === 'REVIEWER';
});

// Reset form when modal opens/closes
watch(() => props.show, (newShow) => {
    if (newShow) {
        // Set the current assignee email if task is assigned
        selectedMember.value = props.task?.assignedTo || '';
        error.value = '';
    } else {
        selectedMember.value = '';
        error.value = '';
        isAssigning.value = false;
    }
});

const handleClose = () => {
    emit('close');
};

const handleAssign = async () => {
    if (!props.task) return;

    isAssigning.value = true;
    error.value = '';

    try {
        logger.info('Assigning task', { 
            taskId: props.task.id, 
            currentAssignee: props.task.assignedTo,
            newAssignee: selectedMember.value 
        });

        let updatedTask: TaskTableRow;

        if (selectedMember.value === 'unassign') {
            // Unassign task by setting email to null
            await taskService.updateTask(props.projectId, props.task.id, {
                assignedToEmail: null
            });
            updatedTask = {
                ...props.task,
                assignedTo: undefined
            };
        } else if (selectedMember.value) {
            // Assign to selected member by email
            await taskService.updateTask(props.projectId, props.task.id, {
                assignedToEmail: selectedMember.value
            });
            updatedTask = {
                ...props.task,
                assignedTo: selectedMember.value
            };
        } else {
            error.value = 'Please select a team member to assign the task to.';
            return;
        }
        
        emit('assigned', updatedTask);
        logger.info('Task assigned successfully', { taskId: props.task.id, newAssignee: selectedMember.value });
        
    } catch (err) {
        logger.error('Failed to assign task', { taskId: props.task?.id, error: err });
        error.value = 'Failed to assign task. Please try again.';
    } finally {
        isAssigning.value = false;
    }
};
</script>

<style lang="scss" scoped>
.assign-modal-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.task-info {
    padding: 1rem;
    background: var(--color-gray-50);
    border-radius: 6px;
    border: 1px solid var(--color-gray-200);
    
    .task-name {
        margin: 0 0 0.5rem;
        font-size: 0.875rem;
        color: var(--color-gray-800);
    }
    
    .current-assignee {
        margin: 0;
        font-size: 0.75rem;
        color: var(--color-gray-600);
        
        .assigned {
            color: var(--color-success);
            font-weight: 500;
        }
        
        .unassigned {
            color: var(--color-warning);
            font-style: italic;
        }
    }
}

.assignment-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.form-label {
    font-weight: 500;
    color: var(--color-gray-800);
    font-size: 0.875rem;
    
    .restriction-note {
        font-weight: 400;
        color: var(--color-warning-600);
        font-size: 0.75rem;
        font-style: italic;
    }
}

.form-select {
    padding: 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    font-size: 0.875rem;
    background: var(--color-white);
    color: var(--color-gray-800);
    
    &:focus {
        outline: none;
        border-color: var(--color-primary);
        box-shadow: 0 0 0 2px var(--color-primary-50);
    }
    
    &:disabled {
        background: var(--color-gray-100);
        color: var(--color-gray-500);
    }
}

.error-message {
    padding: 0.75rem;
    background: var(--color-error-50);
    border: 1px solid var(--color-error-200);
    border-radius: 6px;
    color: var(--color-error-700);
    font-size: 0.875rem;
}

.modal-actions {
    display: flex;
    gap: 0.75rem;
    justify-content: flex-end;
    padding: 0.5rem 0 0;
}
</style>