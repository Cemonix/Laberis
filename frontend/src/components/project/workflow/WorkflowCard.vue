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
import {computed} from 'vue';
import {type Workflow} from '@/types/workflow';

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

<style scoped>
.workflow-card-link {
    display: block;
    text-decoration: none;
    color: inherit;
    height: 100%;
}

.workflow-card {
    background-color: var(--color-white);
    border: 1px solid var(--color-gray-400);
    border-radius: 8px;
    padding: 1.5rem;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
    transition: box-shadow 0.2s ease-in-out, transform 0.2s ease-in-out, border-color 0.2s ease-in-out;
    display: flex;
    flex-direction: column;
    height: 100%;
    
    &:hover {
        transform: translateY(-4px);
        box-shadow: 0 1px 3px rgba(var(--color-black), 0.1);
        border-color: var(--color-primary);
    }
}

.workflow-header {
    margin-bottom: 1rem;
    
    .workflow-name {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-gray-800);
        margin: 0;
    }
}

.workflow-description {
    color: var(--color-gray-600);
    font-size: 1rem;
    line-height: 1.4;
    margin-bottom: 1.5rem;
    flex-grow: 1;
    min-height: 2.4em;
}

.workflow-stats {
    display: flex;
    gap: 1.5rem;
    margin-bottom: 1.5rem;
    
    .stat-item {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        
        .stat-label {
            font-size: 0.875rem;
            color: var(--color-gray-600);
        }
        
        .stat-value {
            font-size: 1rem;
            color: var(--color-gray-800);
            font-weight: 600;
        }
    }
}

.workflow-footer {
    margin-top: auto;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-400);
    text-align: right;
    
    span {
        color: var(--color-primary);
        font-weight: 600;
        font-size: 0.875rem;
        transition: transform 0.2s ease;
    }
}

.workflow-card:hover .workflow-footer span {
    transform: translateX(-4px);
}
</style>