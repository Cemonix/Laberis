<template>
    <div class="workflow-pipeline-viewer">
        <div class="pipeline-header">
            <div class="header-content">
                <h3 class="pipeline-title">Workflow: {{ workflowName }}</h3>
                <p class="pipeline-subtitle">{{ stages.length }} stages configured</p>
            </div>
        </div>
        
        <div class="pipeline-content">
            <div v-if="isLoading" class="pipeline-state loading-state">
                <p>Loading pipeline...</p>
            </div>
            <div v-else-if="error" class="pipeline-state error-state">
                <p>Error: {{ error }}</p>
                <Button variant="secondary" @click="refreshPipeline">Try Again</Button>
            </div>
            <div v-else-if="stages.length === 0" class="pipeline-state empty-state">
                <h3>Empty Workflow</h3>
                <p>This workflow has no stages yet. Add a stage to begin.</p>
            </div>
            <div v-else class="linear-pipeline">
                <div class="pipeline-flow">
                    <template v-for="(stage, index) in orderedStages" :key="stage.id">
                        <div 
                            class="stage-container"
                            :style="{ animationDelay: `${index * 150}ms` }"
                        >
                            <WorkflowStageCard
                                :stage="stage"
                                :show-actions="canEdit"
                                @stage-click="handleStageClick"
                                @edit-stage="handleEditStage"
                                @manage-assignments="handleManageAssignments"
                            />
                        </div>
                        
                        <!-- Arrow between stages -->
                        <div 
                            v-if="index < orderedStages.length - 1" 
                            class="pipeline-arrow"
                            :style="{ animationDelay: `${index * 150 + 75}ms` }"
                        >
                            <svg viewBox="0 0 100 20" class="arrow-svg">
                                <defs>
                                    <marker 
                                        id="arrow-marker" 
                                        viewBox="0 0 10 10" 
                                        refX="9" 
                                        refY="5" 
                                        markerWidth="6" 
                                        markerHeight="6" 
                                        orient="auto"
                                    >
                                        <path d="M 0 0 L 10 5 L 0 10 z" fill="currentColor"></path>
                                    </marker>
                                </defs>
                                <line 
                                    x1="0" 
                                    y1="10" 
                                    x2="100" 
                                    y2="10" 
                                    stroke="currentColor" 
                                    stroke-width="2" 
                                    marker-end="url(#arrow-marker)"
                                />
                            </svg>
                        </div>
                    </template>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import Button from '@/components/common/Button.vue';
import WorkflowStageCard from './WorkflowStageCard.vue';
import type {WorkflowStagePipeline} from '@/types/workflow';

interface Props {
    workflowId: number;
    workflowName: string;
    stages: WorkflowStagePipeline[];
    canEdit?: boolean;
    isLoading?: boolean;
    error?: string | null;
}

const props = defineProps<Props>();

const emit = defineEmits<{
    'edit-pipeline': [];
    'refresh': [];
    'stage-click': [stage: WorkflowStagePipeline];
    'edit-stage': [stage: WorkflowStagePipeline];
    'manage-assignments': [stage: WorkflowStagePipeline];
}>();

// Sort stages by order for linear display
const orderedStages = computed(() => {
    return [...props.stages].sort((a, b) => a.stageOrder - b.stageOrder);
});

const handleStageClick = (stage: WorkflowStagePipeline) => emit('stage-click', stage);
const handleEditStage = (stage: WorkflowStagePipeline) => emit('edit-stage', stage);
const handleManageAssignments = (stage: WorkflowStagePipeline) => emit('manage-assignments', stage);
const refreshPipeline = () => emit('refresh');
</script>

<style scoped>
.workflow-pipeline-viewer {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    background: var(--color-gray-50);
    border-radius: 8px;
    overflow: hidden;
}

.pipeline-header {
    flex-shrink: 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    border-bottom: 1px solid var(--color-gray-400);
    
    .header-content {
        .pipeline-title {
            margin: 0;
            color: var(--color-gray-800);
            font-size: 1.5rem;
        }
        .pipeline-subtitle {
            margin: 0;
            color: var(--color-gray-600);
            font-size: 0.875rem;
        }
    }
}

.pipeline-content {
    flex-grow: 1;
    overflow: auto;
    padding: 1.5rem;
}

.linear-pipeline {
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 300px;
}

.pipeline-flow {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    padding: 1rem;
    flex-wrap: wrap;
    justify-content: center;
    
    @media (max-width: 768px) {
        flex-direction: column;
        gap: 1rem;
    }
}

.stage-container {
    opacity: 0;
    transform: translateY(20px) scale(0.95);
    animation: stage-fade-in 0.6s ease-out forwards;
}

.pipeline-arrow {
    flex-shrink: 0;
    opacity: 0;
    animation: arrow-fade-in 0.4s ease-out forwards;
    
    @media (max-width: 768px) {
        transform: rotate(90deg);
        margin: 0.5rem 0;
    }
}

.arrow-svg {
    width: 60px;
    height: 20px;
    color: var(--color-gray-400);
    
    @media (max-width: 768px) {
        width: 20px;
        height: 60px;
    }
}

.pipeline-state {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 1rem;
    padding: 2rem;
    text-align: center;
    height: 100%;
    
    h3 {
        font-size: 1.25rem;
        color: var(--color-gray-800);
        margin: 0;
    }
    p {
        color: var(--color-gray-600);
        margin: 0;
    }
    
    &.error-state p {
        color: var(--color-error);
    }
}

@keyframes stage-fade-in {
    to {
        opacity: 1;
        transform: translateY(0) scale(1);
    }
}

@keyframes arrow-fade-in {
    to {
        opacity: 1;
    }
}
</style>