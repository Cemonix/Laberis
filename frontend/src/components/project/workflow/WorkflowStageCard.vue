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
                Manage Team
            </Button>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faCheckCircle,
    faCog,
    faEdit,
    faPenNib,
    faSearch,
    faSortNumericUp,
    faUserCog,
    faUsers
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import type {WorkflowStagePipeline} from '@/types/workflow';
import {formatStageType, WorkflowStageType} from '@/types/workflow';

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
.workflow-stage-card {
    width: 280px;
    min-height: 280px; /* Ensure consistent height */
    display: flex;
    flex-direction: column;
    background: var(--color-white);
    border: 2px solid var(--color-gray-400);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
    overflow: hidden;
    transition: all 0.3s ease;
    cursor: pointer;
    
    &:hover {
        transform: translateY(-4px);
        box-shadow: 0 1px 3px rgba(var(--color-black), 0.1);
    }
    
    // Stage type specific styling with hover colors
    &.stage-annotation {
        border-left: 4px solid var(--color-info);
        
        &:hover {
            border-color: var(--color-info);
        }
    }
    
    &.stage-revision {
        border-left: 4px solid var(--color-warning);
        
        &:hover {
            border-color: var(--color-warning);
        }
    }
    
    &.stage-completion {
        border-left: 4px solid var(--color-success);
        
        &:hover {
            border-color: var(--color-success);
        }
    }
    
    // Default hover for cards without specific stage types
    &:not(.stage-annotation):not(.stage-revision):not(.stage-completion):hover {
        border-color: var(--color-primary);
    }
    
    // Clickable hover effects with stage-specific colors
    &.stage-clickable.stage-annotation:hover {
        .stage-header {
            background: linear-gradient(135deg, var(--color-info), var(--color-info));

            .stage-name, .stage-type {
                color: var(--color-white);
            }
            
            .stage-icon svg {
                color: var(--color-white);
            }
        }
    }
    
    &.stage-clickable.stage-revision:hover {
        .stage-header {
            background: linear-gradient(135deg, var(--color-warning), var(--color-warning));

            .stage-name, .stage-type {
                color: var(--color-white);
            }
            
            .stage-icon svg {
                color: var(--color-white);
            }
        }
    }
    
    &.stage-clickable.stage-completion:hover {
        .stage-header {
            background: linear-gradient(135deg, var(--color-success), var(--color-success));

            .stage-name, .stage-type {
                color: var(--color-white);
            }
            
            .stage-icon svg {
                color: var(--color-white);
            }
        }
    }
    
    // Default clickable hover for cards without specific stage types
    &.stage-clickable:not(.stage-annotation):not(.stage-revision):not(.stage-completion):hover {
        .stage-header {
            background: linear-gradient(135deg, var(--color-primary-light), var(--color-primary));
            
            .stage-name, .stage-type {
                color: var(--color-white);
            }
            
            .stage-icon svg {
                color: var(--color-white);
            }
        }
    }
}

.stage-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    background: var(--color-gray-50);
    border-bottom: 1px solid var(--color-gray-400);
    transition: all 0.3s ease;
}

.stage-icon {
    flex-shrink: 0;
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--color-white);
    border-radius: 4px;
    box-shadow: 0 1px 2px rgba(var(--color-black), 0.05);
    
    svg {
        font-size: 1.25rem;
        color: var(--color-primary);
        transition: color 0.3s ease;
    }
}

.stage-info {
    flex-grow: 1;
    min-width: 0;
}

.stage-name {
    margin: 0 0 0.25rem;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-gray-800);
    line-height: 1.2;
    transition: color 0.3s ease;
}

.stage-type {
    font-size: 0.875rem;
    color: var(--color-gray-600);
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    transition: color 0.3s ease;
}

.stage-content {
    padding: 1rem;
    flex-grow: 1; /* Allow content to grow and fill available space */
    display: flex;
    flex-direction: column;
}

.stage-description {
    margin: 0 0 1rem;
    font-size: 0.875rem;
    color: var(--color-gray-600);
    line-height: 1.4;
}

.stage-stats {
    display: flex;
    gap: 1rem;
    margin-bottom: 1rem;
}

.stat-item {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.875rem;
    color: var(--color-gray-600);
    
    svg {
        width: 16px;
        text-align: center;
        color: var(--color-primary);
    }
}

.stage-badges {
    display: flex;
    gap: 0.25rem;
    flex-wrap: wrap;
    margin-top: auto; /* Push badges to bottom of content area */
}

.badge {
    padding: 2px 8px;
    border-radius: 2px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    
    &.start-badge {
        background: var(--color-success-light);
        color: var(--color-success);
        border: 1px solid var(--color-success);
    }
    
    &.end-badge {
        background: var(--color-primary-light);
        color: var(--color-primary);
        border: 1px solid var(--color-primary);
    }
}

.stage-actions {
    display: flex;
    gap: 0.25rem;
    padding: 0.5rem 1rem;
    background: var(--color-gray-50);
    border-top: 1px solid var(--color-gray-400);
}

.action-btn {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;

    svg {
        font-size: 0.75rem;
    }
}

@media (max-width: 768px) {
    .workflow-stage-card {
        width: 100%;
        max-width: 320px;
    }
    
    .stage-stats {
        flex-direction: column;
        gap: 0.25rem;
    }
    
    .stage-actions {
        flex-direction: column;
    }
}
</style>
