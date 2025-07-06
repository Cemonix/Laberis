<template>
    <span class="task-priority-badge" :class="priorityClass">
        <font-awesome-icon :icon="priorityIcon" />
        {{ priorityLabel }}
    </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { 
    faExclamation, 
    faExclamationTriangle, 
    faFlag,
    faMinus
} from '@fortawesome/free-solid-svg-icons';

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
@use "@/styles/variables" as vars;

.task-priority-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: vars.$gap-xsmall;
    padding: 2px 8px;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font-size-xsmall;
    font-weight: vars.$font-weight-medium;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    white-space: nowrap;
    min-width: 60px;
    
    &.high {
        background: vars.$color-error-light;
        color: vars.$color-error;
        border: 1px solid vars.$color-error;
    }
    
    &.medium {
        background: vars.$color-warning-light;
        color: vars.$color-warning-dark;
        border: 1px solid vars.$color-warning;
    }
    
    &.normal {
        background: vars.$color-info-light;
        color: vars.$color-info;
        border: 1px solid vars.$color-info;
    }
    
    &.low {
        background: vars.$color-gray-100;
        color: vars.$color-gray-600;
        border: 1px solid vars.$color-gray-300;
    }
    
    svg {
        font-size: vars.$font-size-xsmall;
    }
}
</style>
