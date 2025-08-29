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
            <!-- Role-based primary action button -->
            <router-link 
                v-if="navigationInfo"
                :to="navigationInfo.url" 
                class="action-button primary"
                :class="{ 'direct-access': navigationInfo.hasDirectAccess }"
            >
                <font-awesome-icon :icon="navigationInfo.buttonIcon" />
                {{ navigationInfo.buttonText }}
            </router-link>
            
            <!-- Fallback to pipeline view -->
            <router-link 
                v-else
                :to="pipelineUrl" 
                class="action-button secondary"
            >
                <font-awesome-icon :icon="faDiagramProject" />
                View Pipeline
            </router-link>
            
            <!-- Secondary pipeline link (for when user has direct task access) -->
            <router-link 
                v-if="navigationInfo?.hasDirectAccess"
                :to="pipelineUrl" 
                class="action-button secondary small"
                title="View workflow pipeline"
            >
                <font-awesome-icon :icon="faDiagramProject" />
                Pipeline
            </router-link>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { type Workflow } from '@/services/project/workflow/workflow.types';
import { type WorkflowStage } from '@/services/project/workflow/workflowStage.types';
import { type ProjectRole } from '@/services/project/project.types';
import { WorkflowNavigationHelper } from '@/core/workflow';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faDiagramProject } from '@fortawesome/free-solid-svg-icons';

interface Props {
    workflow: Workflow;
    stages?: WorkflowStage[];
    userRole?: ProjectRole;
    userEmail?: string;
}

const props = defineProps<Props>();

const pipelineUrl = computed(() => 
    `/projects/${props.workflow.projectId}/workflows/${props.workflow.id}/pipeline`
);

const navigationInfo = computed(() => {
    if (!props.stages || !props.userRole) {
        return null;
    }

    return WorkflowNavigationHelper.getWorkflowCardNavigation(
        props.workflow.projectId,
        props.workflow,
        props.stages,
        props.userRole,
        props.userEmail
    );
});

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
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(var(--color-black), 0.1);
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

.workflow-actions {
    margin-top: auto;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-400);
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.action-button {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    border-radius: 6px;
    text-decoration: none;
    font-weight: 600;
    font-size: 0.875rem;
    transition: all 0.2s ease;
    border: 1px solid transparent;
    
    &.primary {
        background-color: var(--color-primary);
        color: var(--color-white);
        
        &:hover {
            background-color: var(--color-primary-dark);
            transform: translateY(-1px);
        }
        
        &.direct-access {
            background-color: var(--color-success);
            
            &:hover {
                background-color: var(--color-success-dark);
            }
        }
    }
    
    &.secondary {
        background-color: var(--color-gray-100);
        color: var(--color-gray-700);
        border-color: var(--color-gray-300);
        
        &:hover {
            background-color: var(--color-gray-200);
            border-color: var(--color-gray-400);
        }
        
        &.small {
            padding: 0.5rem 0.75rem;
            font-size: 0.8125rem;
        }
    }
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .workflow-actions {
        flex-direction: column;
    }
    
    .action-button {
        justify-content: center;
        
        &.small {
            align-self: flex-start;
        }
    }
}
</style>