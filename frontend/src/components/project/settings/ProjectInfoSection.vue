<template>
    <div class="project-info-section">
        <div class="section-header">
            <h2>Project Information</h2>
            <p>Update your project's basic information and settings.</p>
        </div>

        <div v-if="isLoading" class="loading-state">
            <div class="loading-spinner"></div>
            <p>Loading project information...</p>
        </div>

        <form v-else @submit.prevent="handleUpdateProject" class="project-form">
            <!-- Project Name -->
            <div class="form-group">
                <label for="project-name">Project Name</label>
                <input
                    id="project-name"
                    v-model="form.name"
                    type="text"
                    placeholder="Enter project name"
                    :disabled="isSaving || !canUpdate"
                    required
                />
                <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
            </div>

            <!-- Project Description -->
            <div class="form-group">
                <label for="project-description">Description</label>
                <textarea
                    id="project-description"
                    v-model="form.description"
                    placeholder="Enter project description (optional)"
                    rows="4"
                    :disabled="isSaving || !canUpdate"
                ></textarea>
                <div v-if="errors.description" class="field-error">{{ errors.description }}</div>
            </div>


            <!-- Project Metadata -->
            <div class="project-metadata">
                <div class="metadata-grid">
                    <div class="metadata-item">
                        <span class="metadata-label">Created</span>
                        <span class="metadata-value">{{ formatDate(project?.createdAt) }}</span>
                    </div>
                    <div class="metadata-item">
                        <span class="metadata-label">Last Updated</span>
                        <span class="metadata-value">{{ formatDate(project?.updatedAt) }}</span>
                    </div>
                    <div class="metadata-item">
                        <span class="metadata-label">Owner</span>
                        <span class="metadata-value">{{ project?.ownerEmail || 'Unknown' }}</span>
                    </div>
                    <div class="metadata-item">
                        <span class="metadata-label">Project Type</span>
                        <span class="metadata-value">{{ formatProjectType(project?.projectType) }}</span>
                    </div>
                </div>
            </div>

            <!-- Form Actions -->
            <div v-if="canUpdate" class="form-actions">
                <Button
                    type="button"
                    variant="secondary"
                    @click="resetForm"
                    :disabled="isSaving || !hasChanges"
                >
                    Reset Changes
                </Button>
                <Button
                    type="submit"
                    variant="primary"
                    :disabled="isSaving || !hasChanges || !isFormValid"
                    :loading="isSaving"
                >
                    {{ isSaving ? 'Saving...' : 'Save Changes' }}
                </Button>
            </div>

            <div v-else class="read-only-notice">
                <font-awesome-icon :icon="faLock" />
                <p>You don't have permission to modify project settings.</p>
            </div>
        </form>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, reactive, ref, watch} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faLock} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import {projectService} from '@/services/project';
import {useToast} from '@/composables/useToast';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/services/auth/permissions.types';
import {AppLogger} from '@/core/logger/logger';
import {ProjectType, ProjectStatus} from '@/services/project/project.types';
import type {Project, UpdateProjectRequest} from '@/services/project/project.types';

const logger = AppLogger.createComponentLogger('ProjectInfoSection');

interface Props {
    projectId: number;
}

const props = defineProps<Props>();

const { showToast } = useToast();
const { handleError } = useErrorHandler();
const { hasProjectPermission } = usePermissions();

// State
const project = ref<Project | null>(null);
const isLoading = ref(true);
const isSaving = ref(false);
const originalForm = ref<any>(null);

// Form state
const form = reactive({
    name: '',
    description: ''
});

const errors = reactive({
    name: '',
    description: ''
});

// Computed
const canUpdate = computed(() => 
    hasProjectPermission(PERMISSIONS.PROJECT.UPDATE)
);

const hasChanges = computed(() => {
    if (!originalForm.value) return false;
    return (
        form.name !== originalForm.value.name ||
        form.description !== originalForm.value.description
    );
});

const isFormValid = computed(() => {
    return form.name.trim().length > 0 && 
           !errors.name && 
           !errors.description;
});

// Methods
const loadProject = async () => {
    isLoading.value = true;
    try {
        project.value = await projectService.getProject(props.projectId);
        
        // Populate form
        if (project.value) {
            form.name = project.value.name || '';
            form.description = project.value.description || '';
        }
        
        // Store original values
        originalForm.value = {
            name: form.name,
            description: form.description
        };
        
        logger.info('Project information loaded successfully');
    } catch (error) {
        logger.error('Failed to load project information', error);
        handleError(error, 'Failed to load project information');
    } finally {
        isLoading.value = false;
    }
};

const validateForm = (): boolean => {
    errors.name = '';
    errors.description = '';
    
    if (!form.name.trim()) {
        errors.name = 'Project name is required';
        return false;
    }
    
    if (form.name.trim().length > 100) {
        errors.name = 'Project name must be 100 characters or less';
        return false;
    }
    
    if (form.description && form.description.length > 500) {
        errors.description = 'Description must be 500 characters or less';
        return false;
    }
    
    return true;
};

const handleUpdateProject = async () => {
    if (!validateForm() || !canUpdate.value) {
        return;
    }
    
    isSaving.value = true;
    try {
        const updateData: UpdateProjectRequest = {
            name: form.name.trim(),
            description: form.description.trim() || undefined,
            status: project.value?.status || ProjectStatus.ACTIVE
        };
        
        const updatedProject = await projectService.updateProject(props.projectId, updateData);
        project.value = updatedProject;
        
        // Update original form values
        originalForm.value = {
            name: form.name,
            description: form.description
        };
        
        logger.info('Project updated successfully');
        showToast('Success', 'Project information updated successfully', 'success');
    } catch (error) {
        logger.error('Failed to update project', error);
        handleError(error, 'Failed to update project information');
    } finally {
        isSaving.value = false;
    }
};

const resetForm = () => {
    if (originalForm.value) {
        form.name = originalForm.value.name;
        form.description = originalForm.value.description;
    }
    
    // Clear errors
    errors.name = '';
    errors.description = '';
};

const formatDate = (dateString?: string): string => {
    if (!dateString) return 'Unknown';
    
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
};

const formatProjectType = (projectType?: ProjectType): string => {
    if (!projectType) return 'Unknown';
    
    const typeLabels = {
        [ProjectType.OTHER]: 'Other',
        [ProjectType.IMAGE_CLASSIFICATION]: 'Image Classification',
        [ProjectType.OBJECT_DETECTION]: 'Object Detection',
        [ProjectType.IMAGE_SEGMENTATION]: 'Image Segmentation',
        [ProjectType.VIDEO_ANNOTATION]: 'Video Annotation',
        [ProjectType.TEXT_ANNOTATION]: 'Text Annotation'
    };
    
    return typeLabels[projectType] || projectType;
};

// Lifecycle
onMounted(() => {
    loadProject();
});

// Watch for form changes to clear errors
watch(() => form.name, () => {
    if (errors.name) errors.name = '';
});

watch(() => form.description, () => {
    if (errors.description) errors.description = '';
});
</script>

<style lang="scss" scoped>
.project-info-section {
    padding: 2rem;
}

.section-header {
    margin-bottom: 2rem;
    
    h2 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-gray-900);
        margin-bottom: 0.5rem;
    }
    
    p {
        color: var(--color-gray-600);
        line-height: 1.5;
    }
}

.loading-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--color-gray-600);
    
    .loading-spinner {
        width: 2rem;
        height: 2rem;
        border: 3px solid var(--color-gray-300);
        border-top: 3px solid var(--color-primary);
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }
    
    p {
        font-style: italic;
    }
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.project-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    max-width: 600px;
}

.form-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    
    label {
        font-weight: 500;
        color: var(--color-gray-700);
        font-size: 0.875rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
    
    input, textarea, select {
        padding: 0.75rem;
        border: 1px solid var(--color-gray-300);
        border-radius: 4px;
        font-size: 1rem;
        transition: border-color 0.2s ease;
        
        &:focus {
            outline: none;
            border-color: var(--color-primary);
            box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
        }
        
        &:disabled {
            background-color: var(--color-gray-50);
            color: var(--color-gray-500);
            cursor: not-allowed;
        }
    }
    
    textarea {
        resize: vertical;
        min-height: 100px;
        font-family: inherit;
    }
}

.field-error {
    color: var(--color-error);
    font-size: 0.875rem;
    font-weight: 500;
}

.field-help {
    color: var(--color-gray-600);
    font-size: 0.875rem;
    line-height: 1.4;
}

.project-metadata {
    background-color: var(--color-gray-50);
    border-radius: 6px;
    padding: 1.5rem;
    margin-top: 1rem;
    
    .metadata-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1rem;
    }
    
    .metadata-item {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        
        .metadata-label {
            font-size: 0.75rem;
            font-weight: 500;
            color: var(--color-gray-600);
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }
        
        .metadata-value {
            font-weight: 500;
            color: var(--color-gray-900);
        }
    }
}

.form-actions {
    display: flex;
    gap: 1rem;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-200);
    
    @media (max-width: 768px) {
        flex-direction: column-reverse;
    }
}

.read-only-notice {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem;
    background-color: var(--color-yellow-50);
    border: 1px solid var(--color-yellow-200);
    border-radius: 4px;
    color: var(--color-yellow-800);
    
    svg {
        color: var(--color-yellow-600);
    }
    
    p {
        margin: 0;
        font-size: 0.875rem;
    }
}
</style>