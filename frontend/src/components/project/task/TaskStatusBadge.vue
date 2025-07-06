<template>
    <span class="task-status-badge" :class="statusClass">
        <font-awesome-icon :icon="statusIcon" />
        {{ statusLabel }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { 
    faHourglass, 
    faPlay, 
    faCheckCircle, 
    faArchive, 
    faPause 
} from '@fortawesome/free-solid-svg-icons';
import type { TaskStatus } from '@/types/task';

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
@use "@/styles/variables" as vars;

.task-status-badge {
    display: inline-flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    padding: 2px 8px;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font-size-xsmall;
    font-weight: vars.$font-weight-medium;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    white-space: nowrap;
    
    &.not-started {
        background: vars.$color-gray-100;
        color: vars.$color-gray-600;
        border: 1px solid vars.$color-gray-300;
    }
    
    &.in-progress {
        background: vars.$color-info-light;
        color: vars.$color-info;
        border: 1px solid vars.$color-info;
    }
    
    &.completed {
        background: vars.$color-success-light;
        color: vars.$color-success;
        border: 1px solid vars.$color-success;
    }
    
    &.archived {
        background: vars.$color-gray-200;
        color: vars.$color-gray-700;
        border: 1px solid vars.$color-gray-400;
    }
    
    &.suspended {
        background: vars.$color-warning-light;
        color: vars.$color-warning-dark;
        border: 1px solid vars.$color-warning;
    }
    
    svg {
        font-size: vars.$font-size-xsmall;
    }
}
</style>
