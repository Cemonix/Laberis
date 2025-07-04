<template>
    <router-link 
        :to="workflowUrl" 
        class="workflow-card-link"
        aria-label="View workflow pipeline"
    >
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
            
            <div class="workflow-footer">
                <span>View Pipeline &rarr;</span>
            </div>
        </div>
    </router-link>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { type Workflow } from '@/types/workflow';

interface Props {
    workflow: Workflow;
}

const props = defineProps<Props>();

const workflowUrl = computed(() => 
    `/projects/${props.workflow.projectId}/workflows/${props.workflow.id}/pipeline`
);

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

.workflow-card-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;
}

.workflow-card {
    background-color: vars.$theme-surface;
    border: 1px solid vars.$theme-border;
    border-radius: vars.$border-radius-lg;
    padding: vars.$padding-large;
    box-shadow: vars.$shadow-sm;
    transition: box-shadow 0.2s ease-in-out, transform 0.2s ease-in-out, border-color 0.2s ease-in-out;
    display: flex;
    flex-direction: column;
    height: 100%;
    
    &:hover {
        transform: translateY(-4px);
        box-shadow: vars.$shadow-md;
        border-color: vars.$color-primary;
    }
}

.workflow-header {
    margin-bottom: vars.$margin-medium;
    
    .workflow-name {
        font-size: vars.$font-size-large;
        font-weight: vars.$font-weight-xlarge;
        color: vars.$theme-text;
        margin: 0;
    }
}

.workflow-description {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-medium;
    line-height: vars.$line-height-medium;
    margin-bottom: vars.$margin-large;
    flex-grow: 1;
    min-height: 2.4em;
}

.workflow-stats {
    display: flex;
    gap: vars.$gap-large;
    margin-bottom: vars.$margin-large;
    
    .stat-item {
        display: flex;
        flex-direction: column;
        gap: vars.$gap-xsmall;
        
        .stat-label {
            font-size: vars.$font-size-small;
            color: vars.$theme-text-light;
        }
        
        .stat-value {
            font-size: vars.$font-size-medium;
            color: vars.$theme-text;
            font-weight: vars.$font-weight-large;
        }
    }
}

.workflow-footer {
    margin-top: auto;
    padding-top: vars.$padding-medium;
    border-top: 1px solid vars.$theme-border;
    text-align: right;
    
    span {
        color: vars.$color-primary;
        font-weight: vars.$font-weight-large;
        font-size: vars.$font-size-small;
        transition: transform 0.2s ease;
    }
}

.workflow-card:hover .workflow-footer span {
    transform: translateX(-4px);
}
</style>