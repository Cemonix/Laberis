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
                        {{ member.userName?.charAt(0).toUpperCase() }}
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
import type { WorkflowStagePipeline } from '@/types/workflow';
import { formatStageType } from '@/types/workflow';
import type { ProjectMember } from '@/types/projectMember';

interface Props {
    stage: WorkflowStagePipeline;
    position?: { x: number; y: number };
    size?: { width: number | string; height: number | string };
    showActions?: boolean;
    showMembers?: boolean;
    showTaskCount?: boolean;
    taskCount?: number;
    assignedMembers?: ProjectMember[];
}

const props = withDefaults(defineProps<Props>(), {
    position: () => ({ x: 0, y: 0 }),
    size: () => ({ width: 220, height: 'auto' }),
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
    const classes = [];
    if (props.stage.stageType) {
        classes.push(`stage-type-${props.stage.stageType.toLowerCase()}`);
    }
    if (props.stage.isInitialStage) classes.push('initial-stage');
    if (props.stage.isFinalStage) classes.push('final-stage');
    return classes;
});

const stageStyle = computed(() => ({
    left: `${props.position.x}px`,
    top: `${props.position.y}px`,
    width: `${props.size.width}px`,
    minHeight: typeof props.size.height === 'number' ? `${props.size.height}px` : props.size.height,
}));
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workflow-stage {
    position: absolute;
    background: linear-gradient(145deg, vars.$color-white, vars.$color-gray-50);
    border: 1px solid vars.$theme-border;
    border-left-width: 4px;
    border-radius: vars.$border-radius-lg;
    padding: vars.$padding-medium;
    box-shadow: vars.$shadow-sm;
    transition: all 0.25s ease-in-out;
    cursor: pointer;
    display: flex;
    flex-direction: column;

    &:hover {
        box-shadow: vars.$shadow-lg;
        transform: translateY(-5px) scale(1.02);
        border-color: vars.$color-primary;
    }
    
    &.initial-stage {
        border-left-color: vars.$color-success;
    }
    
    &.final-stage {
        border-left-color: vars.$color-primary;
    }

    &.stage-type-review {
        border-left-color: vars.$color-warning;
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
    padding: 3px 8px;
    border-radius: vars.$border-radius-circle;
    font-size: 11px;
    font-weight: vars.$font-weight-large;
    text-transform: uppercase;
    letter-spacing: 0.5px;
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
        background-color: vars.$color-gray-200;
        color: vars.$color-gray-700;
    }
}

.stage-content {
    margin-bottom: vars.$margin-medium;
    flex-grow: 1;
    
    .stage-description {
        font-size: vars.$font-size-small;
        color: vars.$theme-text-light;
        margin: 0 0 vars.$margin-small 0;
        line-height: vars.$line-height-medium;
        min-height: 2.6em;
    }
}

.stage-stats {
    display: grid;
    grid-template-columns: 1fr;
    gap: vars.$gap-xsmall;
    padding: vars.$padding-small;
    background-color: vars.$color-gray-100;
    border-radius: vars.$border-radius-md;
    margin-bottom: vars.$margin-medium;
    
    .stat-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: vars.$font-size-small;
        
        .stat-label {
            color: vars.$theme-text-light;
        }
        
        .stat-value {
            font-weight: vars.$font-weight-large;
            color: vars.$theme-text;
        }
    }
}

.stage-members {
    .members-title {
        font-size: 11px;
        font-weight: vars.$font-weight-medium;
        color: vars.$theme-text-light;
        margin: 0 0 vars.$margin-xsmall 0;
        text-transform: uppercase;
    }
    
    .members-list {
        display: flex;
        align-items: center;
        gap: -8px;
    }
    
    .member-avatar, .member-more {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: vars.$font-size-small;
        font-weight: vars.$font-weight-large;
        border: 2px solid white;
        box-shadow: vars.$shadow-sm;
    }

    .member-avatar {
        background-color: vars.$color-primary;
        color: vars.$color-white;
    }
    
    .member-more {
        background-color: vars.$color-gray-200;
        color: vars.$color-gray-700;
    }
}

.stage-actions {
    display: flex;
    gap: vars.$gap-small;
    margin-top: auto;
    padding-top: vars.$padding-small;
    border-top: 1px solid vars.$theme-border;
    
    .action-btn {
        flex: 1;
        font-size: vars.$font-size-small;
        padding: vars.$padding-xsmall vars.$padding-small;
    }
}
</style>