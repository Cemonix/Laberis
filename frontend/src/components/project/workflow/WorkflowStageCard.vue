<template>
    <div 
        class="workflow-stage-card"
        :class="stageClasses"
        @click="$emit('stage-click', stage)"
    >
        <div class="stage-header">
            <div class="stage-icon">
                <font-awesome-icon :icon="stageIcon" />
            </div>
            <div class="stage-info">
                <h4 class="stage-name">{{ stage.name }}</h4>
                <span class="stage-type">{{ stage.stageType ? formatStageType(stage.stageType) : 'Unknown' }}</span>
            </div>
        </div>
        
        <div class="stage-content">
            <p v-if="stage.description" class="stage-description">
                {{ stage.description }}
            </p>
            
            <div class="stage-stats">
                <div class="stat-item">
                    <font-awesome-icon :icon="faUsers" />
                    <span>{{ stage.assignedUserCount || 0 }} users</span>
                </div>
                
                <div class="stat-item">
                    <font-awesome-icon :icon="faSortNumericUp" />
                    <span>Order #{{ stage.stageOrder }}</span>
                </div>
            </div>
            
            <div class="stage-badges">
                <span v-if="stage.isInitialStage" class="badge start-badge">Start</span>
                <span v-if="stage.isFinalStage" class="badge end-badge">End</span>
            </div>
        </div>

        <div v-if="showActions" class="stage-actions">
            <Button 
                variant="secondary" 
                @click.stop="$emit('edit-stage', stage)"
                class="action-btn"
            >
                <font-awesome-icon :icon="faEdit" />
                Edit
            </Button>
            <Button 
                variant="secondary" 
                @click.stop="$emit('manage-assignments', stage)"
                class="action-btn"
            >
                <font-awesome-icon :icon="faUserCog" />
                Users
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faPenNib, faSearch, faCheckCircle, faCog, faUsers, faSortNumericUp, faEdit, faUserCog } from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import type { WorkflowStagePipeline } from '@/types/workflow';
import { WorkflowStageType, formatStageType } from '@/types/workflow';

interface Props {
    stage: WorkflowStagePipeline;
    showActions?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
    showActions: false,
});

defineEmits<{
    'stage-click': [stage: WorkflowStagePipeline];
    'edit-stage': [stage: WorkflowStagePipeline];
    'manage-assignments': [stage: WorkflowStagePipeline];
}>();

const stageClasses = computed(() => ({
    'stage-initial': props.stage.isInitialStage,
    'stage-final': props.stage.isFinalStage,
    'stage-annotation': props.stage.stageType === WorkflowStageType.ANNOTATION,
    'stage-revision': props.stage.stageType === WorkflowStageType.REVISION,
    'stage-completion': props.stage.stageType === WorkflowStageType.COMPLETION,
    'stage-clickable': !props.showActions,
}));

const stageIcon = computed(() => {
    switch (props.stage.stageType) {
        case WorkflowStageType.ANNOTATION:
            return faPenNib;
        case WorkflowStageType.REVISION:
            return faSearch;
        case WorkflowStageType.COMPLETION:
            return faCheckCircle;
        default:
            return faCog;
    }
});
</script>

<style lang="scss" scoped>
@use "sass:color" as color;
@use "@/styles/variables" as vars;

.workflow-stage-card {
    width: 280px;
    min-height: 280px; /* Ensure consistent height */
    display: flex;
    flex-direction: column;
    background: vars.$color-white;
    border: 2px solid vars.$theme-border;
    border-radius: vars.$border-radius-lg;
    box-shadow: vars.$shadow-sm;
    overflow: hidden;
    transition: all 0.3s ease;
    cursor: pointer;
    
    &:hover {
        transform: translateY(-4px);
        box-shadow: vars.$shadow-md;
    }
    
    // Stage type specific styling with hover colors
    &.stage-annotation {
        border-left: 4px solid vars.$color-info;
        
        &:hover {
            border-color: vars.$color-info;
        }
    }
    
    &.stage-revision {
        border-left: 4px solid vars.$color-warning;
        
        &:hover {
            border-color: vars.$color-warning;
        }
    }
    
    &.stage-completion {
        border-left: 4px solid vars.$color-success;
        
        &:hover {
            border-color: vars.$color-success;
        }
    }
    
    // Default hover for cards without specific stage types
    &:not(.stage-annotation):not(.stage-revision):not(.stage-completion):hover {
        border-color: vars.$color-primary;
    }
    
    // Clickable hover effects with stage-specific colors
    &.stage-clickable.stage-annotation:hover {
        .stage-header {
            background: linear-gradient(135deg, color.adjust(vars.$color-info, $lightness: 10%), vars.$color-info);

            .stage-name, .stage-type {
                color: vars.$color-white;
            }
            
            .stage-icon svg {
                color: vars.$color-white;
            }
        }
    }
    
    &.stage-clickable.stage-revision:hover {
        .stage-header {
            background: linear-gradient(135deg, color.adjust(vars.$color-warning, $lightness: 10%), vars.$color-warning);

            .stage-name, .stage-type {
                color: vars.$color-white;
            }
            
            .stage-icon svg {
                color: vars.$color-white;
            }
        }
    }
    
    &.stage-clickable.stage-completion:hover {
        .stage-header {
            background: linear-gradient(135deg, color.adjust(vars.$color-success, $lightness: 10%), vars.$color-success);

            .stage-name, .stage-type {
                color: vars.$color-white;
            }
            
            .stage-icon svg {
                color: vars.$color-white;
            }
        }
    }
    
    // Default clickable hover for cards without specific stage types
    &.stage-clickable:not(.stage-annotation):not(.stage-revision):not(.stage-completion):hover {
        .stage-header {
            background: linear-gradient(135deg, vars.$color-primary-light, vars.$color-primary);
            
            .stage-name, .stage-type {
                color: vars.$color-white;
            }
            
            .stage-icon svg {
                color: vars.$color-white;
            }
        }
    }
}

.stage-header {
    display: flex;
    align-items: center;
    gap: vars.$gap-medium;
    padding: vars.$padding-medium;
    background: vars.$color-gray-50;
    border-bottom: 1px solid vars.$theme-border;
    transition: all 0.3s ease;
}

.stage-icon {
    flex-shrink: 0;
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: vars.$color-white;
    border-radius: vars.$border-radius-md;
    box-shadow: vars.$shadow-xs;
    
    svg {
        font-size: vars.$font-size-large;
        color: vars.$color-primary;
        transition: color 0.3s ease;
    }
}

.stage-info {
    flex-grow: 1;
    min-width: 0;
}

.stage-name {
    margin: 0 0 vars.$gap-xsmall;
    font-size: vars.$font-size-medium;
    font-weight: 600;
    color: vars.$theme-text;
    line-height: 1.2;
    transition: color 0.3s ease;
}

.stage-type {
    font-size: vars.$font-size-small;
    color: vars.$theme-text-light;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    transition: color 0.3s ease;
}

.stage-content {
    padding: vars.$padding-medium;
    flex-grow: 1; /* Allow content to grow and fill available space */
    display: flex;
    flex-direction: column;
}

.stage-description {
    margin: 0 0 vars.$gap-medium;
    font-size: vars.$font-size-small;
    color: vars.$theme-text-light;
    line-height: 1.4;
}

.stage-stats {
    display: flex;
    gap: vars.$gap-medium;
    margin-bottom: vars.$gap-medium;
}

.stat-item {
    display: flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    font-size: vars.$font-size-small;
    color: vars.$theme-text-light;
    
    svg {
        width: 16px;
        text-align: center;
        color: vars.$color-primary;
    }
}

.stage-badges {
    display: flex;
    gap: vars.$gap-xsmall;
    flex-wrap: wrap;
    margin-top: auto; /* Push badges to bottom of content area */
}

.badge {
    padding: 2px 8px;
    border-radius: vars.$border-radius-sm;
    font-size: vars.$font-size-xsmall;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    
    &.start-badge {
        background: vars.$color-success-light;
        color: vars.$color-success;
        border: 1px solid vars.$color-success;
    }
    
    &.end-badge {
        background: vars.$color-primary-light;
        color: vars.$color-primary;
        border: 1px solid vars.$color-primary;
    }
}

.stage-actions {
    display: flex;
    gap: vars.$gap-xsmall;
    padding: vars.$padding-small vars.$padding-medium;
    background: vars.$color-gray-50;
    border-top: 1px solid vars.$theme-border;
}

.action-btn {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: vars.$gap-xsmall;

    svg {
        font-size: vars.$font-size-xsmall;
    }
}

@media (max-width: 768px) {
    .workflow-stage-card {
        width: 100%;
        max-width: 320px;
    }
    
    .stage-stats {
        flex-direction: column;
        gap: vars.$gap-xsmall;
    }
    
    .stage-actions {
        flex-direction: column;
    }
}
</style>
