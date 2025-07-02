<template>
    <div class="workflow-pipeline-viewer">
        <div class="pipeline-header">
            <div class="header-content">
                <h3 class="pipeline-title">Workflow Pipeline: {{ workflowName }}</h3>
                <p class="pipeline-subtitle">{{ stages.length }} stages configured</p>
            </div>
            <div class="pipeline-controls">
                <Button 
                    variant="primary" 
                    @click="$emit('edit-pipeline')"
                    v-if="canEdit"
                    class="control-btn"
                >
                    Edit Pipeline
                </Button>
                <Button 
                    variant="secondary" 
                    @click="refreshPipeline"
                    class="control-btn"
                >
                    Refresh
                </Button>
            </div>
        </div>
        
        <div class="pipeline-canvas" ref="canvasContainer">
            <WorkflowStage
                v-for="stage in stages" 
                :key="stage.id"
                :stage="stage"
                :position="getStagePosition(stage)"
                :size="{ width: 220, height: 140 }"
                :show-actions="canEdit"
                :show-members="true"
                :show-task-count="false"
                @stage-click="handleStageClick"
                @edit-stage="handleEditStage"
                @manage-assignments="handleManageAssignments"
            />
            
            <!-- SVG overlay for drawing connections -->
            <svg 
                class="pipeline-connections" 
                :width="canvasWidth" 
                :height="canvasHeight"
            >
                <defs>
                    <marker 
                        id="arrowhead" 
                        markerWidth="10" 
                        markerHeight="7" 
                        refX="9" 
                        refY="3.5" 
                        orient="auto"
                    >
                        <polygon 
                            points="0 0, 10 3.5, 0 7" 
                            fill="var(--connection-color)"
                        />
                    </marker>
                </defs>
                
                <line
                    v-for="connection in connections"
                    :key="`${connection.from.id}-${connection.to.id}`"
                    :x1="connection.x1"
                    :y1="connection.y1"
                    :x2="connection.x2"
                    :y2="connection.y2"
                    class="connection-line"
                    marker-end="url(#arrowhead)"
                />
            </svg>
        </div>
        
        <div v-if="isLoading" class="pipeline-state">
            <div class="loading-spinner"></div>
            <p>Loading pipeline...</p>
        </div>
        
        <div v-if="error" class="pipeline-state error-state">
            <p>Error loading pipeline: {{ error }}</p>
            <Button variant="secondary" @click="refreshPipeline">
                Try Again
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue';
import Button from '@/components/common/Button.vue';
import WorkflowStage from './WorkflowStage.vue';
import type { WorkflowStagePipeline } from '@/types/workflow';
import type { Connection } from '@/types/workflow/pipeline';

interface Props {
    workflowId: number;
    workflowName: string;
    stages: WorkflowStagePipeline[];
    canEdit?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
    'edit-pipeline': [];
    'refresh': [];
    'stage-click': [stage: WorkflowStagePipeline];
    'edit-stage': [stage: WorkflowStagePipeline];
    'manage-assignments': [stage: WorkflowStagePipeline];
}>();

// Reactive state
const isLoading = ref(false);
const error = ref<string | null>(null);
const canvasContainer = ref<HTMLElement>();
const canvasWidth = ref(800);
const canvasHeight = ref(600);

// Computed properties
const connections = computed<Connection[]>(() => {
    const result: Connection[] = [];
    
    props.stages.forEach(stage => {
        stage.nextStageIds.forEach(nextStageId => {
            const nextStage = props.stages.find(s => s.id === nextStageId);
            if (nextStage) {
                result.push(calculateConnection(stage, nextStage));
            }
        });
    });
    
    return result;
});

// Methods
const getStagePosition = (stage: WorkflowStagePipeline): { x: number; y: number } => {
    // Simple auto-layout: arrange stages in a flow from left to right
    // This is a basic implementation - can be enhanced with proper graph layout algorithms
    
    const baseX = 50;
    const baseY = 50;
    const horizontalSpacing = 250;
    const verticalSpacing = 150;
    
    // Use provided positions if available, otherwise auto-calculate
    const x = stage.positionX || baseX + (stage.stageOrder * horizontalSpacing);
    const y = stage.positionY || baseY + ((stage.id % 3) * verticalSpacing);
    
    return { x, y };
};

const handleStageClick = (stage: WorkflowStagePipeline) => {
    emit('stage-click', stage);
};

const handleEditStage = (stage: WorkflowStagePipeline) => {
    emit('edit-stage', stage);
};

const handleManageAssignments = (stage: WorkflowStagePipeline) => {
    emit('manage-assignments', stage);
};

const calculateConnection = (from: WorkflowStagePipeline, to: WorkflowStagePipeline): Connection => {
    // Get stage positions
    const fromPos = getStagePosition(from);
    const toPos = getStagePosition(to);
    
    // Calculate connection points (center to center with offset)
    const stageWidth = 220;
    const stageHeight = 140;
    
    return {
        from,
        to,
        x1: fromPos.x + stageWidth, // right edge of from stage
        y1: fromPos.y + stageHeight / 2, // center height
        x2: toPos.x, // left edge of to stage  
        y2: toPos.y + stageHeight / 2 // center height
    };
};

const refreshPipeline = () => {
    emit('refresh');
};

const updateCanvasSize = () => {
    if (canvasContainer.value) {
        canvasWidth.value = canvasContainer.value.offsetWidth;
        canvasHeight.value = canvasContainer.value.offsetHeight;
    }
};

// Lifecycle
onMounted(async () => {
    await nextTick();
    updateCanvasSize();
    window.addEventListener('resize', updateCanvasSize);
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workflow-pipeline-viewer {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    background: linear-gradient(135deg, vars.$theme-surface 0%, rgba(248, 250, 252, 0.8) 100%);
    border-radius: vars.$border-radius-lg;
    overflow: hidden;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.pipeline-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: vars.$padding-large;
    background: linear-gradient(135deg, vars.$theme-surface-variant 0%, rgba(255, 255, 255, 0.9) 100%);
    border-bottom: 1px solid vars.$theme-border;
    backdrop-filter: blur(10px);
    
    .header-content {
        flex: 1;
        
        .pipeline-title {
            margin: 0 0 vars.$margin-xsmall 0;
            color: vars.$theme-text;
            font-size: vars.$font-size-xlarge;
            font-weight: vars.$font-weight-large;
            line-height: 1.2;
        }
        
        .pipeline-subtitle {
            margin: 0;
            color: vars.$theme-text-light;
            font-size: vars.$font-size-small;
            font-weight: vars.$font-weight-medium;
        }
    }
}

.pipeline-controls {
    display: flex;
    gap: vars.$gap-medium;
    
    .control-btn {
        padding: vars.$padding-small vars.$padding-medium;
        border-radius: vars.$border-radius-md;
        font-weight: vars.$font-weight-medium;
        transition: all 0.2s ease;
        
        &:hover {
            transform: translateY(-1px);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        }
    }
}

.pipeline-canvas {
    position: relative;
    height: 100%;
    min-height: 500px;
    flex: 1;
    background: 
        radial-gradient(circle at 25% 25%, rgba(59, 130, 246, 0.05) 0%, transparent 50%),
        radial-gradient(circle at 75% 75%, rgba(16, 185, 129, 0.05) 0%, transparent 50%),
        linear-gradient(90deg, rgba(var(vars.$color-gray-300), 0.08) 1px, transparent 1px),
        linear-gradient(rgba(var(vars.$color-gray-300), 0.08) 1px, transparent 1px);
    background-size: 
        800px 800px,
        600px 600px,
        20px 20px,
        20px 20px;
    overflow: auto;
    scrollbar-width: thin;
    scrollbar-color: rgba(var(vars.$color-gray-400), 0.5) transparent;
    
    &::-webkit-scrollbar {
        width: 8px;
        height: 8px;
    }
    
    &::-webkit-scrollbar-track {
        background: rgba(var(vars.$color-gray-200), 0.3);
        border-radius: vars.$border-radius-circle;
    }
    
    &::-webkit-scrollbar-thumb {
        background: rgba(var(vars.$color-gray-400), 0.5);
        border-radius: vars.$border-radius-circle;
        
        &:hover {
            background: rgba(var(vars.$color-gray-500), 0.7);
        }
    }
}

.pipeline-connections {
    position: absolute;
    top: 0;
    left: 0;
    pointer-events: none;
    z-index: 1;
}

.connection-line {
    stroke: var(--connection-color, #{vars.$color-gray-400});
    stroke-width: 2;
    fill: none;
    opacity: 0.7;
    transition: opacity 0.2s ease;
    
    &:hover {
        opacity: 1;
        stroke-width: 3;
    }
}

.pipeline-state {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: vars.$gap-medium;
    height: 200px;
    padding: vars.$padding-large;
    
    p {
        color: vars.$theme-text-light;
        font-size: vars.$font-size-medium;
        margin: 0;
    }
    
    &.error-state {
        p {
            color: vars.$color-error;
            font-weight: vars.$font-weight-medium;
        }
    }
}

.loading-spinner {
    width: 32px;
    height: 32px;
    border: 3px solid rgba(var(vars.$color-primary), 0.2);
    border-top: 3px solid vars.$color-primary;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

// CSS Custom Properties for theming
:root {
    --connection-color: #{vars.$color-gray-400};
}

// Responsive Design
@media (max-width: 1024px) {
    .pipeline-header {
        padding: vars.$padding-medium;
        flex-direction: column;
        align-items: flex-start;
        gap: vars.$gap-medium;
        
        .pipeline-controls {
            width: 100%;
            justify-content: flex-end;
        }
    }
}

@media (max-width: 768px) {
    .pipeline-header {
        .header-content {
            .pipeline-title {
                font-size: vars.$font-size-large;
            }
        }
        
        .pipeline-controls {
            gap: vars.$gap-small;
            
            .control-btn {
                padding: vars.$padding-xsmall vars.$padding-small;
                font-size: vars.$font-size-small;
            }
        }
    }
    
    .pipeline-canvas {
        min-height: 400px;
    }
}

@media (max-width: 480px) {
    .pipeline-header {
        padding: vars.$padding-small;
        
        .pipeline-controls {
            width: 100%;
            justify-content: stretch;
            
            .control-btn {
                flex: 1;
            }
        }
    }
}

// Dark mode support (if implemented)
@media (prefers-color-scheme: dark) {
    .workflow-pipeline-viewer {
        background: linear-gradient(135deg, var(--dark-surface, #{vars.$theme-surface}) 0%, rgba(30, 41, 59, 0.8) 100%);
    }
    
    .pipeline-header {
        background: linear-gradient(135deg, var(--dark-surface-variant, #{vars.$theme-surface-variant}) 0%, rgba(51, 65, 85, 0.9) 100%);
    }
    
    .pipeline-canvas {
        background: 
            radial-gradient(circle at 25% 25%, rgba(59, 130, 246, 0.08) 0%, transparent 50%),
            radial-gradient(circle at 75% 75%, rgba(16, 185, 129, 0.08) 0%, transparent 50%),
            linear-gradient(90deg, rgba(100, 116, 139, 0.15) 1px, transparent 1px),
            linear-gradient(rgba(100, 116, 139, 0.15) 1px, transparent 1px);
    }
    
    :root {
        --connection-color: rgba(148, 163, 184, 0.8);
    }
}

// Animation for stage entrance
@keyframes stageEnter {
    from {
        opacity: 0;
        transform: scale(0.8) translateY(20px);
    }
    to {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

// Apply entrance animation to stages
:deep(.workflow-stage) {
    animation: stageEnter 0.5s ease-out;
    animation-fill-mode: backwards;
    
    // Stagger the animation for each stage
    @for $i from 1 through 10 {
        &:nth-child(#{$i}) {
            animation-delay: #{$i * 0.1}s;
        }
    }
}
</style>
