<template>
    <span class="task-status-badge" :class="statusClass">
        <font-awesome-icon :icon="statusIcon" />
        {{ statusLabel }}
    </span>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faArchive, faCheckCircle, faHourglass, faPause, faPlay} from '@fortawesome/free-solid-svg-icons';
import type {TaskStatus} from '@/types/task';

interface Props {
    status: TaskStatus;
}

const props = defineProps<Props>();

const statusConfig = {
    NOT_STARTED: {
        label: 'Not Started',
        icon: faHourglass,
        class: 'not-started'
    },
    IN_PROGRESS: {
        label: 'In Progress',
        icon: faPlay,
        class: 'in-progress'
    },
    COMPLETED: {
        label: 'Completed',
        icon: faCheckCircle,
        class: 'completed'
    },
    ARCHIVED: {
        label: 'Archived',
        icon: faArchive,
        class: 'archived'
    },
    SUSPENDED: {
        label: 'Suspended',
        icon: faPause,
        class: 'suspended'
    }
};

const statusInfo = computed(() => statusConfig[props.status] || statusConfig.NOT_STARTED);
const statusLabel = computed(() => statusInfo.value.label);
const statusIcon = computed(() => statusInfo.value.icon);
const statusClass = computed(() => statusInfo.value.class);
</script>

<style lang="scss" scoped>
.task-status-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 2px 8px;
    border-radius: 2px;
    font-size: 0.75rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    white-space: nowrap;
    
    &.not-started {
        background: var(--color-gray-100);
        color: var(--color-gray-600);
        border: 1px solid var(--color-gray-300);
    }
    
    &.in-progress {
        background: var(--color-info-light);
        color: var(--color-info);
        border: 1px solid var(--color-info);
    }
    
    &.completed {
        background: var(--color-success-light);
        color: var(--color-success);
        border: 1px solid var(--color-success);
    }
    
    &.archived {
        background: var(--color-gray-200);
        color: var(--color-gray-700);
        border: 1px solid var(--color-gray-400);
    }
    
    &.suspended {
        background: var(--color-warning-light);
        color: var(--color-warning-dark);
        border: 1px solid var(--color-warning);
    }
    
    svg {
        font-size: 0.75rem;
    }
}
</style>
