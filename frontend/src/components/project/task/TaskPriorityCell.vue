<template>
    <div class="priority-cell" :class="getPriorityClass(priority)">
        <font-awesome-icon :icon="getPriorityIcon(priority)" />
        {{ getPriorityLabel(priority) }}
    </div>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faExclamationTriangle, faArrowUp, faMinus } from '@fortawesome/free-solid-svg-icons';

interface Props {
    priority: number;
}

defineProps<Props>();

const getPriorityClass = (priority: number): string => {
    if (priority >= 3) return 'priority-high';
    if (priority === 2) return 'priority-medium';
    return 'priority-low';
};

const getPriorityIcon = (priority: number) => {
    if (priority >= 3) return faExclamationTriangle;
    if (priority === 2) return faArrowUp;
    return faMinus;
};

const getPriorityLabel = (priority: number): string => {
    if (priority >= 3) return 'High';
    if (priority === 2) return 'Medium';
    return 'Low';
};
</script>

<style lang="scss" scoped>
.priority-cell {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-weight: 500;
    
    &.priority-high {
        color: var(--color-error);
    }
    
    &.priority-medium {
        color: var(--color-warning);
    }
    
    &.priority-low {
        color: var(--color-gray-600);
    }
}
</style>