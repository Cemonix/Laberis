<template>
    <div class="workflow-pipeline-viewer">
        <div class="pipeline-header">
            <div class="header-content">
                <h3 class="pipeline-title">Workflow: {{ workflowName }}</h3>
                <p class="pipeline-subtitle">{{ stages.length }} stages configured</p>
            </div>
            <div class="pipeline-controls">
                <Button 
                    variant="primary" 
                    @click="$emit('edit-pipeline')"
                    v-if="canEdit"
                >
                    Manage Pipeline
                </Button>
            </div>
        </div>
        
        <div class="pipeline-canvas-wrapper">
            <div 
                class="pipeline-canvas" 
                ref="canvasContainer" 
                :style="{ width: `${graphLayout.width}px`, height: `${graphLayout.height}px` }"
            >
                <WorkflowStage
                    v-for="(stage, index) in stages" 
                    :key="stage.id"
                    :stage="stage"
                    :position="graphLayout.stagePositions.get(stage.id)"
                    :show-actions="canEdit"
                    @stage-click="handleStageClick"
                    @edit-stage="handleEditStage"
                    @manage-assignments="handleManageAssignments"
                    :style="{ animationDelay: `${index * 100}ms` }"
                />
                
                <svg
                    class="pipeline-connections" 
                    :width="graphLayout.width" 
                    :height="graphLayout.height"
                >
                    <defs>
                        <marker 
                            id="arrowhead" 
                            viewBox="0 0 10 10" 
                            refX="9" 
                            refY="5" 
                            markerWidth="6" 
                            markerHeight="6" 
                            orient="auto-start-reverse"
                        >
                            <path d="M 0 0 L 10 5 L 0 10 z" class="arrowhead-polygon"></path>
                        </marker>
                    </defs>
                    
                    <path
                        v-for="(connection, index) in graphLayout.connections"
                        :key="`${connection.from.id}-${connection.to.id}`"
                        :d="connection.path"
                        class="connection-path"
                        marker-end="url(#arrowhead)"
                        :style="{ animationDelay: `${(stages.length + index) * 100}ms` }"
                    />
                </svg>
            </div>
        </div>
        
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
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import Button from '@/components/common/Button.vue';
import WorkflowStage from './WorkflowStage.vue';
import type { WorkflowStagePipeline } from '@/types/workflow';

const STAGE_WIDTH = 240;
const STAGE_HEIGHT = 190;

interface ConnectionPath {
    from: WorkflowStagePipeline;
    to: WorkflowStagePipeline;
    path: string;
}

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

const canvasContainer = ref<HTMLElement | null>(null);

const graphLayout = computed(() => {
    const stagePositions = new Map<number, { x: number; y: number }>();
    const connections: ConnectionPath[] = [];
    
    return {
        stagePositions,
        connections,
        width: 800,
        height: 600,
    };
});

const handleStageClick = (stage: WorkflowStagePipeline) => emit('stage-click', stage);
const handleEditStage = (stage: WorkflowStagePipeline) => emit('edit-stage', stage);
const handleManageAssignments = (stage: WorkflowStagePipeline) => emit('manage-assignments', stage);
const refreshPipeline = () => emit('refresh');
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workflow-pipeline-viewer {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    background: vars.$theme-background;
    border-radius: vars.$border-radius-lg;
    overflow: hidden;
}

.pipeline-header {
    flex-shrink: 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: vars.$padding-medium vars.$padding-large;
    border-bottom: 1px solid vars.$theme-border;
    
    .header-content {
        .pipeline-title {
            margin: 0;
            color: vars.$theme-text;
            font-size: vars.$font-size-xlarge;
        }
        .pipeline-subtitle {
            margin: 0;
            color: vars.$theme-text-light;
            font-size: vars.$font-size-small;
        }
    }
}

.pipeline-canvas-wrapper {
    flex-grow: 1;
    overflow: auto;
    position: relative;
    background-color: vars.$color-gray-100;
    background-image: radial-gradient(vars.$color-gray-300 1px, transparent 1px);
    background-size: 20px 20px;
}

.pipeline-canvas {
    position: relative;
}

.pipeline-connections {
    position: relative;
    pointer-events: none;
    z-index: 1;

    .arrowhead-polygon {
        fill: #9ca3af;
    }

    .connection-path {
        stroke: #9ca3af;
        stroke-width: 2.5;
        fill: none;
        opacity: 0;
        animation: draw-line 0.5s ease-out forwards;
    }
}

.pipeline-state {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: vars.$gap-medium;
    padding: vars.$padding-xlarge;
    text-align: center;
    height: 100%;
    
    h3 {
        font-size: vars.$font-size-large;
        color: vars.$theme-text;
    }
    p {
        color: vars.$theme-text-light;
        margin: 0;
    }
    
    &.error-state p {
        color: vars.$color-error;
    }
}

@keyframes draw-line {
  to {
    opacity: 1;
  }
}

:deep(.workflow-stage) {
    z-index: 2;
    opacity: 0;
    transform: scale(0.9) translateY(10px);
    animation: stage-enter 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards;
}

@keyframes stage-enter {
  to {
    opacity: 1;
    transform: scale(1) translateY(0);
  }
}
</style>