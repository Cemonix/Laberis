<template>
    <ModalWindow
        :is-open="show"
        title="Change Priority"
        @close="handleClose"
    >
        <div class="priority-modal-content">
            <div v-if="task" class="task-info">
                <p class="task-name">
                    <strong>{{ task.assetName }}</strong>
                </p>
                <p class="current-priority">
                    Current Priority: 
                    <TaskPriorityCell :priority="task.priority" />
                </p>
            </div>

            <div class="priority-section">
                <label class="form-label">Select new priority:</label>
                <div class="priority-options">
                    <div 
                        v-for="option in priorityOptions" 
                        :key="option.value"
                        class="priority-option"
                        :class="{ active: selectedPriority === option.value }"
                        @click="selectedPriority = option.value"
                    >
                        <div class="priority-preview">
                            <font-awesome-icon :icon="option.icon" :class="option.class" />
                            <span :class="option.class">{{ option.label }}</span>
                        </div>
                        <input 
                            type="radio" 
                            :value="option.value" 
                            v-model="selectedPriority"
                            :id="`priority-${option.value}`"
                        />
                    </div>
                </div>
            </div>

            <div v-if="error" class="error-message">
                {{ error }}
            </div>
        </div>

        <template #footer>
            <div class="modal-actions">
                <Button variant="secondary" @click="handleClose" :disabled="isChanging">
                    Cancel
                </Button>
                <Button 
                    variant="primary" 
                    @click="handleChangePriority" 
                    :disabled="selectedPriority === task?.priority || isChanging"
                    :loading="isChanging"
                >
                    Change Priority
                </Button>
            </div>
        </template>
    </ModalWindow>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import Button from '@/components/common/Button.vue';
import TaskPriorityCell from './TaskPriorityCell.vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faExclamationTriangle, faArrowUp, faMinus } from '@fortawesome/free-solid-svg-icons';
import type { TaskTableRow } from '@/types/task';
import { taskService } from '@/services/api/projects';
import { AppLogger } from '@/utils/logger';

interface Props {
    show: boolean;
    task: TaskTableRow | null;
    projectId: number;
}

interface Emits {
    (e: 'close'): void;
    (e: 'changed', updatedTask: TaskTableRow): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const logger = AppLogger.createComponentLogger('TaskPriorityModal');

const selectedPriority = ref<number>(1);
const isChanging = ref<boolean>(false);
const error = ref<string>('');

// Priority options configuration
const priorityOptions = computed(() => [
    {
        value: 1,
        label: 'Low',
        icon: faMinus,
        class: 'priority-low'
    },
    {
        value: 2,
        label: 'Medium',
        icon: faArrowUp,
        class: 'priority-medium'
    },
    {
        value: 3,
        label: 'High',
        icon: faExclamationTriangle,
        class: 'priority-high'
    }
]);

// Reset form when modal opens/closes
watch(() => props.show, (newShow) => {
    if (newShow && props.task) {
        selectedPriority.value = props.task.priority;
        error.value = '';
    } else {
        selectedPriority.value = 1;
        error.value = '';
        isChanging.value = false;
    }
});

const handleClose = () => {
    emit('close');
};

const handleChangePriority = async () => {
    if (!props.task) return;

    isChanging.value = true;
    error.value = '';

    try {
        logger.info('Changing task priority', { 
            taskId: props.task.id, 
            currentPriority: props.task.priority,
            newPriority: selectedPriority.value 
        });

        // Update the task priority using the API
        await taskService.updateTask(props.projectId, props.task.id, {
            priority: selectedPriority.value
        });

        // Create updated task object for the parent component
        const updatedTask: TaskTableRow = {
            ...props.task,
            priority: selectedPriority.value
        };
        
        emit('changed', updatedTask);
        logger.info('Task priority changed successfully', { 
            taskId: props.task.id, 
            newPriority: selectedPriority.value 
        });
        
    } catch (err) {
        logger.error('Failed to change task priority', { taskId: props.task?.id, error: err });
        error.value = 'Failed to change task priority. Please try again.';
    } finally {
        isChanging.value = false;
    }
};
</script>

<style lang="scss" scoped>
.priority-modal-content {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
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
    
    .current-priority {
        margin: 0;
        font-size: 0.75rem;
        color: var(--color-gray-600);
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }
}

.priority-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.form-label {
    font-weight: 500;
    color: var(--color-gray-800);
    font-size: 0.875rem;
}

.priority-options {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.priority-option {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem;
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.2s ease;
    
    &:hover {
        background: var(--color-gray-50);
        border-color: var(--color-primary);
    }
    
    &.active {
        background: var(--color-primary-50);
        border-color: var(--color-primary);
    }
    
    .priority-preview {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 500;
        
        .priority-high {
            color: var(--color-error);
        }
        
        .priority-medium {
            color: var(--color-warning);
        }
        
        .priority-low {
            color: var(--color-gray-600);
        }
    }
    
    input[type="radio"] {
        margin: 0;
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