<template>
    <div class="annotations-panel">
        <div class="panel-header">
            <h3 class="panel-title">Annotations</h3>
            <div class="annotation-count">{{ annotations.length }}</div>
        </div>
        
        <!-- Loading state -->
        <div v-if="isLoading" class="panel-loading">
            <span>Loading annotations...</span>
        </div>
        
        <!-- No annotations state -->
        <div v-else-if="annotations.length === 0" class="no-annotations">
            <p>No annotations yet</p>
            <small>Select a tool and start annotating the image.</small>
        </div>
        
        <!-- Annotations table -->
        <div v-else class="annotations-table-container">
            <table class="annotations-table">
                <thead>
                    <tr>
                        <th class="col-index">#</th>
                        <th class="col-type">Type</th>
                        <th class="col-label">Label</th>
                        <th class="col-actions">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <tr 
                        v-for="(annotation, index) in annotations" 
                        :key="annotation.annotationId || annotation.clientId"
                        class="annotation-row"
                        :class="{ 'annotation-hover': hoveredAnnotationId === annotation.annotationId }"
                        @mouseenter="handleAnnotationHover(annotation.annotationId)"
                        @mouseleave="handleAnnotationLeave()"
                    >
                        <!-- Index -->
                        <td class="col-index">
                            <span class="annotation-index">{{ index + 1 }}</span>
                        </td>
                        
                        <!-- Type -->
                        <td class="col-type">
                            <div class="type-indicator">
                                <font-awesome-icon :icon="getAnnotationIcon(annotation.annotationType)" class="type-icon" />
                            </div>
                        </td>
                        
                        <!-- Label -->
                        <td class="col-label">
                            <div class="label-display">
                                <div 
                                    v-if="getCurrentLabel(annotation.labelId)"
                                    class="label-color-dot"
                                    :style="{ backgroundColor: getCurrentLabel(annotation.labelId)?.color }"
                                ></div>
                                <span class="label-name">
                                    {{ getCurrentLabel(annotation.labelId)?.name || 'No Label' }}
                                </span>
                            </div>
                        </td>
                        
                        <!-- Actions -->
                        <td class="col-actions">
                            <div class="action-buttons">
                                <button 
                                    class="action-btn focus-btn"
                                    @click="handleFocusAnnotation(annotation)"
                                    title="Focus on annotation"
                                >
                                    <font-awesome-icon :icon="faEye" />
                                </button>
                                <button 
                                    class="action-btn edit-btn"
                                    @click="handleEditLabel(annotation)"
                                    :disabled="!annotation.annotationId"
                                    title="Change label"
                                >
                                    <font-awesome-icon :icon="faEdit" />
                                </button>
                                <button 
                                    class="action-btn delete-btn"
                                    @click="handleDeleteAnnotation(annotation.annotationId)"
                                    :disabled="!annotation.annotationId"
                                    title="Delete annotation"
                                >
                                    <font-awesome-icon :icon="faTrash" />
                                </button>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Label Edit Modal -->
        <ModalWindow
            v-if="isLabelEditModalOpen"
            :is-open="isLabelEditModalOpen"
            title="Change Label"
            @close="closeLabelEditModal"
            @confirm="confirmLabelChange"
            confirm-text="Change Label"
            cancel-text="Cancel"
            :confirm-disabled="!selectedNewLabelId"
        >
            <div class="label-edit-content">
                <p class="modal-description">
                    Select a new label for annotation #{{ editingAnnotation?.annotationId }}:
                </p>
                
                <div class="label-options">
                    <div 
                        v-for="label in availableLabels"
                        :key="label.labelId"
                        :class="['label-option', { 'selected': selectedNewLabelId === label.labelId }]"
                        @click="selectedNewLabelId = label.labelId"
                    >
                        <div 
                            class="label-color-preview"
                            :style="{ backgroundColor: label.color }"
                        ></div>
                        <div class="label-info">
                            <div class="label-name">{{ label.name }}</div>
                            <div v-if="label.description" class="label-description">{{ label.description }}</div>
                        </div>
                    </div>
                </div>
            </div>
        </ModalWindow>
        
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import type { Annotation } from '@/types/workspace/annotation';
import { AnnotationType } from '@/types/workspace/annotation';
import type { Label } from '@/types/label/label';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { 
    faEye, 
    faEdit, 
    faTrash, 
    faDotCircle, 
    faSquare, 
    faMinus, 
    faWaveSquare, 
    faDrawPolygon,
    faQuestion
} from '@fortawesome/free-solid-svg-icons';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';

const workspaceStore = useWorkspaceStore();
const hoveredAnnotationId = ref<number | null>(null);

// Modal state
const isLabelEditModalOpen = ref(false);
const editingAnnotation = ref<Annotation | null>(null);
const selectedNewLabelId = ref<number | null>(null);

const isLoading = computed(() => workspaceStore.getLoadingState);
const annotations = computed(() => workspaceStore.getAnnotations);
const availableLabels = computed(() => workspaceStore.getAvailableLabels);

const getCurrentLabel = (labelId: number): Label | undefined => {
    return workspaceStore.getLabelById(labelId);
};

const handleLabelChange = async (annotation: Annotation, newLabelId: string | undefined) => {
    if (!annotation.annotationId || !newLabelId) return;
    
    const labelId = parseInt(newLabelId, 10);
    if (isNaN(labelId)) return;
    
    try {
        await workspaceStore.updateAnnotation(annotation.annotationId, { labelId });
    } catch (error) {
        console.error('Failed to update annotation label:', error);
    }
};

const handleDeleteAnnotation = async (annotationId: number | undefined) => {
    if (!annotationId) {
        console.warn('Cannot delete annotation: annotationId is missing');
        return;
    }
    
    if (confirm('Are you sure you want to delete this annotation?')) {
        try {
            await workspaceStore.deleteAnnotation(annotationId);
            console.log(`Successfully deleted annotation ${annotationId}`);
        } catch (error) {
            console.error('Failed to delete annotation:', error);
        }
    }
};

const handleEditLabel = (annotation: Annotation) => {
    if (!annotation.annotationId) {
        console.warn('Cannot edit annotation: annotationId is missing', annotation);
        return;
    }
    
    editingAnnotation.value = annotation;
    selectedNewLabelId.value = annotation.labelId;
    isLabelEditModalOpen.value = true;
};

const closeLabelEditModal = () => {
    isLabelEditModalOpen.value = false;
    editingAnnotation.value = null;
    selectedNewLabelId.value = null;
};

const confirmLabelChange = async () => {
    if (!editingAnnotation.value || !selectedNewLabelId.value) return;
    
    await handleLabelChange(editingAnnotation.value, selectedNewLabelId.value.toString());
    closeLabelEditModal();
};

const handleFocusAnnotation = (annotation: Annotation) => {
    // TODO: Implement focus functionality to highlight annotation on canvas
    console.log('Focus annotation:', annotation);
    // This will be implemented later to:
    // 1. Highlight the annotation on canvas
    // 2. Center the view on the annotation
    // 3. Temporarily show annotation details
};

const handleAnnotationHover = (annotationId: number | undefined) => {
    hoveredAnnotationId.value = annotationId || null;
    // TODO: Add visual highlighting on canvas for hovered annotation
};

const handleAnnotationLeave = () => {
    hoveredAnnotationId.value = null;
    // TODO: Remove visual highlighting on canvas
};

const getAnnotationIcon = (type: AnnotationType) => {
    switch (type) {
        case AnnotationType.POINT:
            return faDotCircle;
        case AnnotationType.BOUNDING_BOX:
            return faSquare;
        case AnnotationType.LINE:
            return faMinus;
        case AnnotationType.POLYLINE:
            return faWaveSquare;
        case AnnotationType.POLYGON:
            return faDrawPolygon;
        case AnnotationType.TEXT:
            return faQuestion; // TODO: Add text icon
        default:
            return faQuestion;
    }
};

</script>

<style scoped>
.annotations-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: 1rem;
    background-color: var(--color-dark-blue-2);
}

.panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid var(--color-accent-blue);
}

.panel-title {
    color: var(--color-gray-200);
    font-size: 1.25rem;
    margin: 0;
}

.annotation-count {
    background-color: var(--color-primary);
    color: var(--color-white);
    padding: 0.25rem 0.5rem;
    border-radius: 12px;
    font-size: 0.875rem;
    font-weight: bold;
    min-width: 24px;
    text-align: center;
}

.panel-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem;
    color: var(--color-gray-400);
    font-style: italic;
}

.no-annotations {
    padding: 1.5rem;
    text-align: center;
    color: var(--color-gray-400);
    
    p {
        margin-bottom: 0.5rem;
        font-weight: bold;
    }
    
    small {
        color: var(--color-gray-500);
        line-height: 1.4;
    }
}

.annotations-table-container {
    flex: 1;
    overflow-y: auto;
    margin-bottom: 1rem;
}

.annotations-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875rem;
    
    thead th {
        background-color: var(--color-dark-blue-1);
        color: var(--color-gray-200);
        font-weight: bold;
        padding: 0.5rem 0.25rem;
        text-align: left;
        border-bottom: 2px solid var(--color-accent-blue);
        position: sticky;
        top: 0;
        z-index: 10;
    }
    
    tbody tr {
        transition: all 0.2s ease-in-out;
        
        &:hover {
            background-color: rgba(var(--color-accent-blue), 0.1);
        }
        
        &.annotation-hover {
            background-color: rgba(var(--color-primary), 0.1);
        }
        
        &:not(:last-child) {
            border-bottom: 1px solid var(--color-gray-700);
        }
    }
    
    td {
        padding: 0.5rem 0.25rem;
        vertical-align: middle;
    }
}

.col-index {
    width: 40px;
    text-align: center;
    
    .annotation-index {
        font-weight: bold;
        color: var(--color-gray-300);
    }
}

.col-type {
    width: 50px;
    text-align: center;
    
    .type-indicator {
        display: flex;
        justify-content: center;
        
        .type-icon {
            color: var(--color-primary);
            font-size: 1rem;
        }
    }
}

.col-label {
    min-width: 100px;
    
    .label-display {
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }
    
    .label-color-dot {
        width: 12px;
        height: 12px;
        border-radius: 50%;
        border: 1px solid var(--color-gray-600);
        flex-shrink: 0;
    }
    
    .label-name {
        color: var(--color-gray-200);
        font-size: 0.875rem;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
}


.col-actions {
    width: 100px;
    
    .action-buttons {
        display: flex;
        gap: 0.25rem;
        justify-content: center;
    }
    
    .action-btn {
        background: none;
        border: none;
        cursor: pointer;
        padding: 0.25rem;
        border-radius: 3px;
        font-size: 0.875rem;
        transition: all 0.2s ease-in-out;
        
        &:disabled {
            opacity: 0.4;
            cursor: not-allowed;
        }
        
        &.focus-btn {
            color: var(--color-accent-blue);
            
            &:hover:not(:disabled) {
                background-color: rgba(var(--color-accent-blue), 0.2);
                color: var(--color-white);
            }
        }
        
        &.edit-btn {
            color: var(--color-warning);
            
            &:hover:not(:disabled) {
                background-color: rgba(var(--color-warning), 0.2);
                color: var(--color-white);
            }
        }
        
        &.delete-btn {
            color: var(--color-error);
            
            &:hover:not(:disabled) {
                background-color: rgba(var(--color-error), 0.2);
                color: var(--color-white);
            }
        }
    }
}

/* Label Edit Modal Styles */
.label-edit-content {
    padding: 1rem 0;
}

.modal-description {
    color: var(--color-gray-300);
    margin-bottom: 1.5rem;
    font-size: 0.95rem;
}

.label-options {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-height: 300px;
    overflow-y: auto;
}

.label-option {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem;
    border: 2px solid var(--color-gray-700);
    border-radius: 6px;
    background-color: var(--color-dark-blue-3);
    cursor: pointer;
    transition: all 0.2s ease-in-out;
    
    &:hover {
        border-color: var(--color-accent-blue);
        background-color: rgba(var(--color-accent-blue), 0.1);
    }
    
    &.selected {
        border-color: var(--color-primary);
        background-color: rgba(var(--color-primary), 0.1);
    }
}

.label-color-preview {
    width: 24px;
    height: 24px;
    border-radius: 4px;
    border: 1px solid var(--color-gray-600);
    flex-shrink: 0;
}

.label-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.label-name {
    color: var(--color-gray-200);
    font-weight: bold;
    font-size: 0.95rem;
}

.label-description {
    color: var(--color-gray-400);
    font-size: 0.85rem;
    line-height: 1.3;
}

</style>