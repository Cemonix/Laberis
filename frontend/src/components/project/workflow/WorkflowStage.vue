<template>
    <div 
        class="workflow-stage"
        :class="stageClasses"
        :style="stageStyle"
        @click="$emit('stage-click', stage)"
    >
        <div class="stage-header">
            <h4 class="stage-name">{{ stage.name }}</h4>
            <div class="stage-badges">
                <span v-if="stage.isInitialStage" class="badge initial-badge">Start</span>
                <span v-if="stage.isFinalStage" class="badge final-badge">End</span>
                <span v-if="stage.stageType" class="badge type-badge">{{ formatStageType(stage.stageType) }}</span>
            </div>
        </div>
        
        <div class="stage-content">
            <p v-if="stage.description" class="stage-description">
                {{ stage.description }}
            </p>
            
            <div class="stage-stats">
                <div class="stat-item">
                    <span class="stat-label">Assigned Users:</span>
                    <span class="stat-value">{{ stage.assignedUserCount || 0 }}</span>
                </div>
                
                <div v-if="showTaskCount" class="stat-item">
                    <span class="stat-label">Tasks:</span>
                    <span class="stat-value">{{ taskCount }}</span>
                </div>
                
                <div class="stat-item">
                    <span class="stat-label">Order:</span>
                    <span class="stat-value">#{{ stage.stageOrder }}</span>
                </div>
            </div>
            
            <div v-if="showMembers && assignedMembers.length > 0" class="stage-members">
                <h5 class="members-title">Assigned Members:</h5>
                <div class="members-list">
                    <div 
                        v-for="member in assignedMembers.slice(0, 3)" 
                        :key="member.id"
                        class="member-avatar"
                        :title="member.userName"
                    >
                        {{ member.userName.charAt(0).toUpperCase() }}
                    </div>
                    <div v-if="assignedMembers.length > 3" class="member-more">
                        +{{ assignedMembers.length - 3 }}
                    </div>
                </div>
            </div>
        </div>

        <div v-if="showActions" class="stage-actions">
            <Button 
                variant="secondary" 
                @click.stop="$emit('edit-stage', stage)"
                class="action-btn"
            >
                Edit
            </Button>
            <Button 
                variant="secondary" 
                @click.stop="$emit('manage-assignments', stage)"
                class="action-btn"
            >
                Manage Users
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Button from '@/components/common/Button.vue';
import type { WorkflowStagePipeline, WorkflowUser } from '@/types/workflow';
import { formatStageType } from '@/types/workflow';

interface Props {
    stage: WorkflowStagePipeline;
    position?: { x: number; y: number };
    size?: { width: number; height: number };
    showActions?: boolean;
    showMembers?: boolean;
    showTaskCount?: boolean;
    taskCount?: number;
    assignedMembers?: WorkflowUser[];
}

const props = withDefaults(defineProps<Props>(), {
    position: () => ({ x: 0, y: 0 }),
    size: () => ({ width: 220, height: 140 }),
    showActions: false,
    showMembers: true,
    showTaskCount: false,
    taskCount: 0,
    assignedMembers: () => [],
});

defineEmits<{
    'stage-click': [stage: WorkflowStagePipeline];
    'edit-stage': [stage: WorkflowStagePipeline];
    'manage-assignments': [stage: WorkflowStagePipeline];
}>();

const stageClasses = computed(() => {
    const classes = ['stage'];
    
    if (props.stage.stageType) {
        classes.push(`stage-${props.stage.stageType.toLowerCase()}`);
    }
    
    if (props.stage.isInitialStage) classes.push('initial-stage');
    if (props.stage.isFinalStage) classes.push('final-stage');
    
    return classes;
});

const stageStyle = computed(() => {
    return {
        left: `${props.position.x}px`,
        top: `${props.position.y}px`,
        width: `${props.size.width}px`,
        minHeight: `${props.size.height}px`
    };
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workflow-stage {
    position: absolute;
    background: linear-gradient(135deg, vars.$color-white 0%, rgba(248, 250, 252, 0.8) 100%);
    border: 2px solid vars.$theme-border;
    border-radius: vars.$border-radius-lg;
    padding: vars.$padding-medium;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08), 0 2px 4px rgba(0, 0, 0, 0.03);
    transition: all 0.2s ease;
    cursor: pointer;
    backdrop-filter: blur(10px);
    
    &:hover {
        box-shadow: 0 8px 25px rgba(0, 0, 0, 0.12), 0 4px 10px rgba(0, 0, 0, 0.06);
        transform: translateY(-2px);
        border-color: vars.$color-primary;
    }
    
    &.initial-stage {
        border-color: vars.$color-success;
        background: linear-gradient(135deg, vars.$color-white 0%, rgba(34, 197, 94, 0.05) 100%);
    }
    
    &.final-stage {
        border-color: vars.$color-primary;
        background: linear-gradient(135deg, vars.$color-white 0%, rgba(59, 130, 246, 0.05) 100%);
    }
    
    &.stage-annotation {
        background: linear-gradient(135deg, vars.$color-white 0%, rgba(14, 165, 233, 0.03) 100%);
    }
    
    &.stage-review {
        background: linear-gradient(135deg, vars.$color-white 0%, rgba(245, 158, 11, 0.03) 100%);
    }
    
    &.stage-accepted {
        background: linear-gradient(135deg, vars.$color-white 0%, rgba(34, 197, 94, 0.03) 100%);
    }
}

.stage-header {
    margin-bottom: vars.$margin-small;
    
    .stage-name {
        margin: 0 0 vars.$margin-xsmall 0;
        font-size: vars.$font-size-medium;
        font-weight: vars.$font-weight-large;
        color: vars.$theme-text;
        line-height: 1.2;
    }
}

.stage-badges {
    display: flex;
    gap: vars.$gap-xsmall;
    flex-wrap: wrap;
}

.badge {
    padding: 2px 8px;
    border-radius: vars.$border-radius-circle;
    font-size: vars.$font-size-xsmall;
    font-weight: vars.$font-weight-medium;
    line-height: 1;
    
    &.initial-badge {
        background-color: vars.$color-success-light;
        color: vars.$color-success-dark;
    }
    
    &.final-badge {
        background-color: vars.$color-primary-light;
        color: vars.$color-primary-dark;
    }
    
    &.type-badge {
        background-color: vars.$color-gray-100;
        color: vars.$color-gray-700;
    }
}

.stage-content {
    margin-bottom: vars.$margin-small;
    
    .stage-description {
        font-size: vars.$font-size-small;
        color: vars.$theme-text-light;
        margin: 0 0 vars.$margin-small 0;
        line-height: vars.$line-height-medium;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }
}

.stage-stats {
    display: grid;
    grid-template-columns: 1fr;
    gap: vars.$gap-xsmall;
    margin-bottom: vars.$margin-small;
    
    .stat-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: vars.$font-size-small;
        
        .stat-label {
            color: vars.$theme-text-light;
            font-weight: vars.$font-weight-medium;
        }
        
        .stat-value {
            font-weight: vars.$font-weight-large;
            color: vars.$theme-text;
        }
    }
}

.stage-members {
    margin-bottom: vars.$margin-small;
    
    .members-title {
        font-size: vars.$font-size-xsmall;
        font-weight: vars.$font-weight-medium;
        color: vars.$theme-text-light;
        margin: 0 0 vars.$margin-xsmall 0;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
    
    .members-list {
        display: flex;
        gap: vars.$gap-xsmall;
        flex-wrap: wrap;
    }
    
    .member-avatar {
        width: 24px;
        height: 24px;
        border-radius: 50%;
        background-color: vars.$color-primary;
        color: vars.$color-white;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: vars.$font-size-xsmall;
        font-weight: vars.$font-weight-medium;
    }
    
    .member-more {
        width: 24px;
        height: 24px;
        border-radius: 50%;
        background-color: vars.$color-gray-300;
        color: vars.$color-gray-700;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: vars.$font-size-xsmall;
        font-weight: vars.$font-weight-medium;
    }
}

.stage-actions {
    display: flex;
    gap: vars.$gap-xsmall;
    flex-wrap: wrap;
    
    .action-btn {
        flex: 1;
        min-width: 0;
        font-size: vars.$font-size-xsmall;
        padding: vars.$padding-xsmall vars.$padding-small;
    }
}

// Responsive adjustments
@media (max-width: 768px) {
    .workflow-stage {
        padding: vars.$padding-small;
        
        .stage-name {
            font-size: vars.$font-size-small;
        }
        
        .stage-description {
            -webkit-line-clamp: 1;
            line-clamp: 1;
        }
        
        .stage-stats {
            grid-template-columns: 1fr 1fr;
            gap: vars.$gap-xsmall;
        }
    }
}
</style>
