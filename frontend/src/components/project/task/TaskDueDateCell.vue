<template>
    <div v-if="dueDate" class="due-date" :class="getDueDateClass(dueDate)">
        <font-awesome-icon :icon="faCalendar" />
        {{ formatDate(dueDate) }}
    </div>
    <span v-else class="no-due-date">No due date</span>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faCalendar } from '@fortawesome/free-solid-svg-icons';

interface Props {
    dueDate?: string;
}

defineProps<Props>();

const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString();
};

const getDueDateClass = (dueDate: string): string => {
    const due = new Date(dueDate);
    const now = new Date();
    const diffDays = Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return 'overdue';
    if (diffDays <= 1) return 'urgent';
    if (diffDays <= 3) return 'soon';
    return 'normal';
};
</script>

<style lang="scss" scoped>
.due-date {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    
    &.normal {
        color: var(--color-gray-800);
    }
    
    &.soon {
        color: var(--color-warning);
        font-weight: 500;
    }
    
    &.urgent {
        color: var(--color-warning);
        font-weight: 600;
    }
    
    &.overdue {
        color: var(--color-error);
        font-weight: 600;
    }
}

.no-due-date {
    color: var(--color-gray-600);
    font-style: italic;
}
</style>