<template>
    <span class="task-status-badge" :class="statusClass">
        <font-awesome-icon :icon="statusIcon" />
        {{ statusLabel }}
    </span>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faArchive, faCheckCircle, faHourglass, faPause, faPlay, faForward, faPen, faEye, faFlag, faBan, faExclamationTriangle} from '@fortawesome/free-solid-svg-icons';
import type {TaskStatus} from '@/services/project/task/task.types';

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
    },
    DEFERRED: {
        label: 'Deferred',
        icon: faForward,
        class: 'deferred'
    },
    READY_FOR_ANNOTATION: {
        label: 'Ready for Annotation',
        icon: faPen,
        class: 'ready-annotation'
    },
    READY_FOR_REVIEW: {
        label: 'Ready for Review',
        icon: faEye,
        class: 'ready-review'
    },
    READY_FOR_COMPLETION: {
        label: 'Ready for Completion',
        icon: faFlag,
        class: 'ready-completion'
    },
    CHANGES_REQUIRED: {
        label: 'Changes Required',
        icon: faExclamationTriangle,
        class: 'changes-required'
    },
    VETOED: {
        label: 'Vetoed',
        icon: faBan,
        class: 'vetoed'
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
    
    &.deferred {
        background: var(--color-purple-100);
        color: var(--color-purple-700);
        border: 1px solid var(--color-purple-500);
    }
    
    &.ready-annotation {
        background: var(--color-blue-100);
        color: var(--color-blue-700);
        border: 1px solid var(--color-blue-600);
    }
    
    &.ready-review {
        background: var(--color-amber-100);
        color: var(--color-amber-600);
        border: 1px solid var(--color-amber-500);
    }
    
    &.ready-completion {
        background: var(--color-emerald-100);
        color: var(--color-emerald-600);
        border: 1px solid var(--color-emerald-500);
    }
    
    &.changes-required {
        background: var(--color-orange-100);
        color: var(--color-orange-700);
        border: 1px solid var(--color-orange-600);
    }
    
    &.vetoed {
        background: var(--color-red-100);
        color: var(--color-red-700);
        border: 1px solid var(--color-red-600);
    }
    
    svg {
        font-size: 0.75rem;
    }
}
</style>
