<template>
    <div class="selection-column">
        <!-- Header checkbox for selecting all on page -->
        <div class="selection-header" v-if="showHeader" @click.stop>
            <input 
                type="checkbox" 
                class="selection-checkbox header-checkbox"
                :checked="isAllSelected"
                @change="handleHeaderToggle"
                @click.stop
                :disabled="tasks.length === 0"
                :title="getHeaderTooltip()"
                ref="headerCheckbox"
            />
            <span class="sr-only">Select all tasks on page</span>
        </div>
        
        <!-- Row checkbox for individual task -->
        <div class="selection-row" v-if="!showHeader && task" @click.stop>
            <input 
                type="checkbox" 
                class="selection-checkbox row-checkbox"
                :checked="isTaskSelected?.(task.id)"
                @change="handleRowToggle"
                @click.stop
                :disabled="isTaskDisabled(task)"
                :title="getRowTooltip(task)"
            />
            <span class="sr-only">Select task {{ task.assetName }}</span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { TaskTableRow } from '@/services/project/task/task.types';
import { TaskStatus } from '@/services/project/task/task.types';

interface Props {
    /** Show header checkbox (for table header) */
    showHeader?: boolean;
    /** Individual task (for table row) */
    task?: TaskTableRow;
    /** All tasks on current page (needed for header logic) */
    tasks?: TaskTableRow[];
    /** Function to check if task is selected */
    isTaskSelected?: (taskId: number) => boolean;
}

interface Emits {
    (e: 'toggle-task', taskId: number): void;
    (e: 'toggle-page', tasks: TaskTableRow[]): void;
}

const props = withDefaults(defineProps<Props>(), {
    showHeader: false,
    tasks: () => [],
    isTaskSelected: () => false,
});

const emit = defineEmits<Emits>();

// Refs
const headerCheckbox = ref<HTMLInputElement>();

// Computed properties for header checkbox state
const selectableTasks = computed(() => {
    return props.tasks?.filter(task => !isTaskDisabled(task)) || [];
});

const selectedTasksOnPage = computed(() => {
    return selectableTasks.value.filter(task => props.isTaskSelected?.(task.id) === true);
});

const isAllSelected = computed(() => {
    const selectable = selectableTasks.value;
    return selectable.length > 0 && selectedTasksOnPage.value.length === selectable.length;
});

const isSomeSelected = computed(() => {
    const selectedCount = selectedTasksOnPage.value.length;
    const selectableCount = selectableTasks.value.length;
    return selectedCount > 0 && selectedCount < selectableCount;
});

// Event handlers
const handleHeaderToggle = () => {
    if (props.tasks && props.tasks.length > 0) {
        emit('toggle-page', props.tasks);
    }
};

const handleRowToggle = () => {
    if (props.task) {
        emit('toggle-task', props.task.id);
    }
};

// Utility functions
const isTaskDisabled = (task: TaskTableRow): boolean => {
    // Archived tasks cannot be selected for bulk operations
    return task.status === TaskStatus.ARCHIVED;
};

const getHeaderTooltip = (): string => {
    const selectableCount = selectableTasks.value.length;
    const selectedCount = selectedTasksOnPage.value.length;
    
    if (selectableCount === 0) {
        return 'No selectable tasks on this page';
    }
    
    if (isAllSelected.value) {
        return `Deselect all ${selectableCount} tasks on this page`;
    } else if (isSomeSelected.value) {
        return `Select remaining ${selectableCount - selectedCount} tasks on this page`;
    } else {
        return `Select all ${selectableCount} tasks on this page`;
    }
};

const getRowTooltip = (task: TaskTableRow): string => {
    if (isTaskDisabled(task)) {
        return 'Archived tasks cannot be selected';
    }
    
    const isSelected = props.isTaskSelected?.(task.id);
    return isSelected 
        ? `Deselect ${task.assetName}` 
        : `Select ${task.assetName}`;
};
</script>

<style lang="scss" scoped>
.selection-column {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    min-height: 1.5rem;
}

.selection-header,
.selection-row {
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    padding: 0.5rem;
    border-radius: 2px;
}

.selection-checkbox {
    width: 1.25rem;
    height: 1.25rem;
    cursor: pointer;
    margin: 0;
    
    &:disabled {
        cursor: not-allowed;
        opacity: 0.5;
    }
    
    &:focus {
        outline: 2px solid var(--color-primary);
        outline-offset: 2px;
    }
    
    // Indeterminate state styling (for header checkbox)
    &:indeterminate {
        opacity: 0.8;
        
        &::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            width: 8px;
            height: 2px;
            background: var(--color-primary);
            border-radius: 1px;
        }
    }
}

.header-checkbox {
    // Larger for better visibility in header
    width: 1.4rem;
    height: 1.4rem;
}

.row-checkbox {
    // Larger size for easier clicking in table rows
    width: 1.25rem;
    height: 1.25rem;
}

// Screen reader only content
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}

// Hover effects
.selection-header:hover .selection-checkbox:not(:disabled),
.selection-row:hover .selection-checkbox:not(:disabled) {
    transform: scale(1.05);
    transition: transform 0.1s ease;
}

// Focus states for keyboard navigation
.selection-checkbox:focus-visible {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
    border-radius: 2px;
}
</style>