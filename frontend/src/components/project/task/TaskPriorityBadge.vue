<template>
    <span class="task-priority-badge" :class="priorityClass">
        <font-awesome-icon :icon="priorityIcon" />
        {{ priorityLabel }}
    </span>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faExclamation, faExclamationTriangle, faFlag, faMinus} from '@fortawesome/free-solid-svg-icons';

interface Props {
    priority: number;
}

const props = defineProps<Props>();

const priorityInfo = computed(() => {
    switch (props.priority) {
        case 1:
            return {
                label: 'High',
                icon: faExclamation,
                class: 'high'
            };
        case 2:
            return {
                label: 'Medium',
                icon: faExclamationTriangle,
                class: 'medium'
            };
        case 3:
            return {
                label: 'Normal',
                icon: faFlag,
                class: 'normal'
            };
        default:
            return {
                label: 'Low',
                icon: faMinus,
                class: 'low'
            };
    }
});

const priorityLabel = computed(() => priorityInfo.value.label);
const priorityIcon = computed(() => priorityInfo.value.icon);
const priorityClass = computed(() => priorityInfo.value.class);
</script>

<style lang="scss" scoped>
.task-priority-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;
    padding: 2px 8px;
    border-radius: 2px;
    font-size: 0.75rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    white-space: nowrap;
    min-width: 60px;
    
    &.high {
        background: var(--color-error-light);
        color: var(--color-error);
        border: 1px solid var(--color-error);
    }
    
    &.medium {
        background: var(--color-warning-light);
        color: var(--color-warning-dark);
        border: 1px solid var(--color-warning);
    }
    
    &.normal {
        background: var(--color-info-light);
        color: var(--color-info);
        border: 1px solid var(--color-info);
    }
    
    &.low {
        background: var(--color-gray-100);
        color: var(--color-gray-600);
        border: 1px solid var(--color-gray-300);
    }
    
    svg {
        font-size: 0.75rem;
    }
}
</style>
