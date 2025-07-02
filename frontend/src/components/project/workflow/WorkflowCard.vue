<template>
    <div class="workflow-card">
        <div class="workflow-header">
            <h3 class="workflow-name">{{ workflow.name }}</h3>
        </div>
        
        <p class="workflow-description">
            {{ workflow.description || 'No description provided' }}
        </p>
        
        <div class="workflow-stats">
            <div class="stat-item">
                <span class="stat-label">Stages:</span>
                <span class="stat-value">{{ workflow.stageCount || 0 }}</span>
            </div>
            <div class="stat-item">
                <span class="stat-label">Created:</span>
                <span class="stat-value">{{ formatDate(workflow.createdAt) }}</span>
            </div>
        </div>
        
        <div class="workflow-actions">
            <Button 
                variant="secondary" 
                size="small" 
                @click="$emit('edit', workflow)"
                aria-label="Edit workflow"
            >
                Edit
            </Button>
            <Button 
                variant="primary" 
                size="small" 
                @click="$emit('manage-stages', workflow)"
                aria-label="View workflow pipeline"
            >
                View Pipeline
            </Button>
            <Button 
                variant="secondary" 
                size="small" 
                @click="$emit('delete', workflow)"
                aria-label="Delete workflow"
                class="delete-btn"
            >
                Delete
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { type Workflow } from '@/types/workflow';
import Button from '@/components/common/Button.vue';

interface Props {
    workflow: Workflow;
}

interface Emits {
    (e: 'edit', workflow: Workflow): void;
    (e: 'delete', workflow: Workflow): void;
    (e: 'manage-stages', workflow: Workflow): void;
}

defineProps<Props>();
defineEmits<Emits>();

const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
};
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.workflow-card {
    background-color: vars.$theme-surface;
    border: 1px solid vars.$theme-border;
    border-radius: vars.$border-radius-md;
    padding: vars.$padding-large;
    box-shadow: vars.$shadow-sm;
    transition: box-shadow 0.2s ease-in-out, transform 0.2s ease-in-out;
    
    &:hover {
        box-shadow: vars.$shadow-md;
    }
}

.workflow-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: vars.$margin-medium;
    
    .workflow-name {
        font-size: vars.$font-size-large;
        font-weight: vars.$font-weight-xlarge;
        color: vars.$theme-text;
        margin: 0;
        flex: 1;
        margin-right: vars.$margin-small;
    }
    
    .workflow-badges {
        display: flex;
        gap: vars.$gap-small;
        flex-shrink: 0;
    }
}

.badge {
    padding: vars.$padding-xsmall vars.$padding-small;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font-size-small;
    font-weight: vars.$font-weight-medium;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    
    &.default-badge {
        background-color: vars.$color-primary;
        color: vars.$color-white;
    }
}

.workflow-description {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-medium;
    line-height: vars.$line-height-medium;
    margin-bottom: vars.$margin-large;
    min-height: 2.4em;
}

.workflow-stats {
    display: flex;
    gap: vars.$gap-large;
    margin-bottom: vars.$margin-large;
    padding: vars.$padding-medium;
    background-color: vars.$theme-surface-variant;
    border-radius: vars.$border-radius-sm;
    
    .stat-item {
        display: flex;
        flex-direction: column;
        gap: vars.$gap-xsmall;
        
        .stat-label {
            font-size: vars.$font-size-small;
            color: vars.$theme-text-light;
            font-weight: vars.$font-weight-medium;
        }
        
        .stat-value {
            font-size: vars.$font-size-medium;
            color: vars.$theme-text;
            font-weight: vars.$font-weight-xlarge;
        }
    }
}

.workflow-actions {
    display: flex;
    gap: vars.$gap-small;
    flex-wrap: wrap;
    
    :deep(.delete-btn) {
        background-color: vars.$color-error;
        border-color: vars.$color-error;
        color: vars.$color-white;
        
        &:hover:not(:disabled) {
            background-color: color.adjust(vars.$color-error, $alpha: 0.1); 
            border-color: color.adjust(vars.$color-error, $alpha: 0.1);
        }
        
        &:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
    }
    
    // Ensure buttons take equal space on mobile
    @media (max-width: 480px) {
        :deep(.btn) {
            flex: 1;
            min-width: 0;
        }
    }
}
</style>
