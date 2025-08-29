<template>
    <div v-if="isVisible" class="modal-overlay" @click="handleOverlayClick">
        <div class="modal-content" @click.stop>
            <div class="modal-header">
                <h3>Edit Stage - {{ stage.name }}</h3>
                <button class="close-button" @click="handleClose" type="button">
                    <font-awesome-icon :icon="faTimes" />
                </button>
            </div>
            
            <div class="modal-body">
                <div class="stage-edit-modal">
                    <div v-if="isLoading" class="loading-state">
                        <p>Loading stage details...</p>
                    </div>
                    
                    <div v-else-if="error" class="error-state">
                        <p class="error-message">{{ error }}</p>
                        <Button variant="secondary" @click="loadStageDetails">Retry</Button>
                    </div>
                    
                    <div v-else class="edit-content">
                        <form @submit.prevent="handleSave" class="edit-form">
                            <div class="form-group">
                                <label for="stageName" class="form-label">Stage Name</label>
                                <input
                                    id="stageName"
                                    v-model="formData.name"
                                    type="text"
                                    class="form-input"
                                    placeholder="Enter stage name"
                                    required
                                />
                            </div>
                            
                            <div class="form-group">
                                <label for="stageDescription" class="form-label">Description</label>
                                <textarea
                                    id="stageDescription"
                                    v-model="formData.description"
                                    class="form-textarea"
                                    placeholder="Enter stage description (optional)"
                                    rows="4"
                                ></textarea>
                            </div>
                            
                            <div class="stage-info">
                                <div class="info-item">
                                    <span class="info-label">Stage Type:</span>
                                    <span class="info-value">{{ stage.stageType ? formatStageType(stage.stageType) : 'Unknown' }}</span>
                                </div>
                                <div class="info-item">
                                    <span class="info-label">Stage Order:</span>
                                    <span class="info-value">#{{ stage.stageOrder }}</span>
                                </div>
                                <div class="info-item">
                                    <span class="info-label">Stage Position:</span>
                                    <span class="info-value">
                                        <span v-if="stage.isInitialStage" class="badge start-badge">Start</span>
                                        <span v-else-if="stage.isFinalStage" class="badge end-badge">End</span>
                                        <span v-else class="badge middle-badge">Middle</span>
                                    </span>
                                </div>
                            </div>
                        </form>
                    </div>
                
                    <div class="modal-actions">
                        <Button variant="secondary" @click="handleClose" :disabled="isSaving">
                            Cancel
                        </Button>
                        <Button
                            variant="primary"
                            @click="handleSave"
                            :disabled="isSaving || !isFormValid"
                        >
                            <template v-if="isSaving">
                                Saving...
                            </template>
                            <template v-else>
                                Save Changes
                            </template>
                        </Button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, reactive, ref} from 'vue';
import Button from '@/components/common/Button.vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faTimes} from '@fortawesome/free-solid-svg-icons';
import type {WorkflowStagePipeline, UpdateWorkflowStageRequest} from '@/services/project/workflow/workflowStage.types';
import {formatStageType} from '@/services/project/workflow/workflowStage.types';
import {workflowStageService} from '@/services/project';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {AppLogger} from '@/core/logger/logger';

interface Props {
    stage: WorkflowStagePipeline;
    projectId: number;
    workflowId: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
    close: [];
    updated: [];
}>();

const logger = AppLogger.createComponentLogger('StageEditModal');
const {handleError} = useErrorHandler();

// Reactive state
const isVisible = ref(true);
const isLoading = ref(true);
const isSaving = ref(false);
const error = ref<string | null>(null);

const formData = reactive({
    name: '',
    description: '',
});

// Computed properties
const isFormValid = computed(() => {
    return formData.name.trim().length > 0;
});

// Methods
const handleClose = () => {
    isVisible.value = false;
    emit('close');
};

const handleOverlayClick = () => {
    if (!isSaving.value) {
        handleClose();
    }
};

const loadStageDetails = async () => {
    isLoading.value = true;
    error.value = null;
    
    try {
        logger.info(`Loading details for stage ${props.stage.id}`);
        
        // Load the full stage details with proper type information
        const stageDetails = await workflowStageService.getWorkflowStageById(
            props.projectId, 
            props.workflowId, 
            props.stage.id
        );
        
        // Populate form data
        formData.name = stageDetails.name;
        formData.description = stageDetails.description || '';
        
        logger.info('Successfully loaded stage details');
        
    } catch (err) {
        logger.error('Error loading stage details', err);
        error.value = 'Failed to load stage details';
        handleError(err, 'Loading stage details');
    } finally {
        isLoading.value = false;
    }
};

const handleSave = async () => {
    if (!isFormValid.value) return;
    
    isSaving.value = true;
    
    try {
        logger.info(`Saving changes to stage ${props.stage.id}`, formData);
        
        const updateRequest: UpdateWorkflowStageRequest = {
            name: formData.name.trim(),
            description: formData.description.trim() || undefined,
        };
        
        await workflowStageService.updateWorkflowStage(
            props.projectId,
            props.workflowId,
            props.stage.id,
            updateRequest
        );
        
        logger.info('Successfully updated stage');
        emit('updated');
        handleClose();
        
    } catch (err) {
        logger.error('Error saving stage changes', err);
        handleError(err, 'Saving stage changes');
    } finally {
        isSaving.value = false;
    }
};

onMounted(() => {
    loadStageDetails();
});
</script>

<style scoped>
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1rem;
}

.modal-content {
    background: var(--color-white);
    border-radius: 0.5rem;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1), 0 1px 3px rgba(0, 0, 0, 0.08);
    width: 100%;
    max-width: 500px;
    max-height: 80vh;
    overflow: hidden;
    display: flex;
    flex-direction: column;
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    border-bottom: 1px solid var(--color-gray-400);
    background: var(--color-gray-50);
}

.modal-header h3 {
    margin: 0;
    color: var(--color-gray-800);
    font-size: 1.25rem;
    font-weight: 600;
}

.close-button {
    background: none;
    border: none;
    color: var(--color-gray-600);
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0.25rem;
    line-height: 1;
    transition: color 0.2s ease;
}

.close-button:hover {
    color: var(--color-gray-800);
}

.modal-body {
    flex: 1;
    overflow: auto;
    padding: 1.5rem;
}

.stage-edit-modal {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.loading-state,
.error-state {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 1rem;
    padding: 2rem;
    text-align: center;
    flex: 1;
}

.error-message {
    color: var(--color-error);
    margin: 0;
}

.edit-content {
    flex: 1;
    display: flex;
    flex-direction: column;
}

.edit-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.form-label {
    font-weight: 600;
    color: var(--color-gray-800);
    font-size: 0.875rem;
}

.form-input,
.form-textarea,
.form-select {
    padding: 0.75rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    font-size: 0.875rem;
    transition: all 0.2s ease;
    background: var(--color-white);
}

.form-input:focus,
.form-textarea:focus,
.form-select:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px var(--color-primary-light);
}

.form-textarea {
    resize: vertical;
    min-height: 80px;
    font-family: inherit;
}

.checkbox-group {
    flex-direction: row;
    align-items: flex-start;
    gap: 0.75rem;
}

.checkbox-label {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    cursor: pointer;
}

.form-checkbox {
    width: 18px;
    height: 18px;
    margin-right: 0.5rem;
    cursor: pointer;
}

.checkbox-text {
    font-weight: 500;
    color: var(--color-gray-800);
    font-size: 0.875rem;
}

.checkbox-help {
    color: var(--color-gray-600);
    font-size: 0.75rem;
    margin-left: 28px;
}

.modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-400);
}

@media (max-width: 768px) {
    .modal-actions {
        flex-direction: column-reverse;
    }
    
    .checkbox-group {
        flex-direction: column;
        gap: 0.5rem;
    }
    
    .checkbox-help {
        margin-left: 0;
    }
}

.stage-info {
    background: var(--color-gray-50);
    border: 1px solid var(--color-gray-400);
    border-radius: 6px;
    padding: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.info-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.info-label {
    font-weight: 500;
    color: var(--color-gray-700);
    font-size: 0.875rem;
}

.info-value {
    color: var(--color-gray-800);
    font-size: 0.875rem;
    font-weight: 500;
}

.badge {
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.start-badge {
    background: var(--color-success-light);
    color: var(--color-success);
    border: 1px solid var(--color-success);
}

.end-badge {
    background: var(--color-primary-light);
    color: var(--color-primary);
    border: 1px solid var(--color-primary);
}

.middle-badge {
    background: var(--color-info-light);
    color: var(--color-info);
    border: 1px solid var(--color-info);
}
</style>