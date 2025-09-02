<template>
    <div class="label-schemes-page">
        <div class="page-header">
            <h1>Label Schemes</h1>
            <p>Manage the label schemes and labels for this project.</p>
        </div>
        
        <div class="schemes-list">
            <div v-if="isLoading" class="loading-state" role="status" aria-live="polite">
                <p>Loading label schemes...</p>
            </div>
            <template v-else>
                <LabelSchemeCard
                    v-for="scheme in labelSchemes"
                    :key="scheme.labelSchemeId"
                    :scheme="scheme"
                    @edit-scheme="handleEditScheme"
                    @delete-scheme="handleDeleteScheme"
                    @reactivate-scheme="handleReactivateScheme"
                />
                <p v-if="!labelSchemes || labelSchemes.length === 0">
                    No label schemes have been created for this project yet.
                </p>
            </template>
        </div>

        <FloatingActionButton 
            :permission="PERMISSIONS.LABEL_SCHEME.CREATE"
            :onClick="openModal" 
            :disabled="isLoading"
            aria-label="Create New Label Scheme"
            title="Create New Label Scheme"
        />

        <!-- Create/Edit Label Scheme Modal -->
        <ModalWindow 
            :is-open="isModalOpen" 
            :title="editingScheme ? 'Edit Label Scheme' : 'Create New Label Scheme'" 
            @close="closeModal" 
            :hide-footer="true"
        >
            <CreateLabelSchemeForm 
                @cancel="closeModal" 
                @save="formData => editingScheme ? handleUpdateScheme(formData) : handleCreateScheme(formData)" 
                :disabled="isLoading"
                :initial-data="editingScheme || undefined"
            />
        </ModalWindow>

        <!-- Delete Confirmation Modal -->
        <ModalWindow 
            :is-open="isDeleteModalOpen" 
            title="Delete Label Scheme" 
            @close="closeDeleteModal" 
            :hide-footer="true"
        >
            <DeletionImpactDialog 
                v-if="deletionImpact && !isDeletionImpactLoading" 
                :impact="deletionImpact"
                @confirm="confirmDelete"
                @cancel="closeDeleteModal"
                :is-loading="isLoading"
            />
            <div v-else class="loading-deletion-impact">
                <div class="loading-content">
                    <div class="spinner"></div>
                    <p>Analyzing deletion impact...</p>
                    <p class="loading-subtext">Checking how many annotations will be affected</p>
                </div>
                <div class="loading-actions">
                    <Button variant="secondary" @click="closeDeleteModal">
                        Cancel
                    </Button>
                </div>
            </div>
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue';
import {useRoute} from 'vue-router';
import LabelSchemeCard from '@/components/project/labels/LabelSchemeCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateLabelSchemeForm from '@/components/project/labels/CreateLabelSchemeForm.vue';
import DeletionImpactDialog from '@/components/project/labels/DeletionImpactDialog.vue';
import FloatingActionButton from '@/components/common/FloatingActionButton.vue';
import Button from '@/components/common/Button.vue';
import type {FormPayloadLabelScheme, LabelScheme, LabelSchemeDeletionImpact} from '@/services/project/labelScheme/label.types';
import {labelSchemeService, labelService} from '@/services/project';
import {useAlert} from '@/composables/useAlert';
import {useToast} from '@/composables/useToast';
import {AppLogger} from '@/core/logger/logger';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const logger = AppLogger.createComponentLogger('LabelSchemesView');

const route = useRoute();
const { showAlert } = useAlert();
const { showToast, showWarning, showError } = useToast();

const labelSchemes = ref<LabelScheme[]>([]);
const isModalOpen = ref(false);
const isDeleteModalOpen = ref(false);
const isLoading = ref(false);
const isDeletionImpactLoading = ref(false);
const editingScheme = ref<LabelScheme | null>(null);
const schemeToDelete = ref<LabelScheme | null>(null);
const deletionImpact = ref<LabelSchemeDeletionImpact | null>(null);

const openModal = () => {
    editingScheme.value = null;
    isModalOpen.value = true;
};

const closeModal = () => {
    editingScheme.value = null;
    isModalOpen.value = false;
};

const closeDeleteModal = () => {
    schemeToDelete.value = null;
    deletionImpact.value = null;
    isDeletionImpactLoading.value = false; // Reset loading state
    isDeleteModalOpen.value = false;
};

/**
 * Fetches label schemes for the current project
 */
const fetchLabelSchemes = async () => {
    const projectId = Number(route.params.projectId);
    if (!projectId) return;

    try {
        isLoading.value = true;
        const result = await labelSchemeService.getLabelSchemesForProject(projectId);
        labelSchemes.value = result.data;
    } catch (error) {
        await showAlert('Error', 'Failed to load label schemes. Please try again.');
        logger.error('Failed to fetch label schemes:', error);
    } finally {
        isLoading.value = false;
    }
};

/**
 * Creates a new label scheme with labels
 */
const handleCreateScheme = async (formData: FormPayloadLabelScheme) => {
    const projectId = Number(route.params.projectId);

    if (labelSchemes.value.some(scheme => scheme.name.toLowerCase() === formData.name.toLowerCase())) {
        showWarning('Duplicate Scheme', 'This label scheme already exists.');
        return;
    }

    try {
        isLoading.value = true;
        
        // Create the label scheme first
        const newScheme = await labelSchemeService.createLabelScheme(projectId, {
            name: formData.name,
            description: formData.description
        });

        // Create labels for the scheme if any were provided
        if (formData.labels && formData.labels.length > 0) {
            await Promise.all(
                formData.labels.map(labelData => 
                    labelService.createLabel(
                        projectId, 
                        newScheme.labelSchemeId, 
                        {
                            name: labelData.name,
                            color: labelData.color,
                            description: labelData.description
                        }
                    )
                )
            );
        }

        // Refresh the list to get the latest data
        await fetchLabelSchemes();
        
        closeModal();
        showToast('Label Scheme Created', `Label Scheme "${formData.name}" has been created successfully.`);
    } catch (error) {
        showError('Error', 'Failed to create label scheme. Please try again.');
        logger.error('Failed to create label scheme:', error);
    } finally {
        isLoading.value = false;
    }
};

const handleEditScheme = (scheme: LabelScheme) => {
    editingScheme.value = scheme;
    isModalOpen.value = true;
};

const handleUpdateScheme = async (formData: FormPayloadLabelScheme) => {
    if (!editingScheme.value) return;
    
    const projectId = Number(route.params.projectId);
    
    try {
        isLoading.value = true;
        
        await labelSchemeService.updateLabelScheme(projectId, editingScheme.value.labelSchemeId, {
            name: formData.name,
            description: formData.description
        });
        
        await fetchLabelSchemes();
        closeModal();
        showToast('Label Scheme Updated', `Label Scheme "${formData.name}" has been updated successfully.`);
    } catch (error) {
        showError('Error', 'Failed to update label scheme. Please try again.');
        logger.error('Failed to update label scheme:', error);
    } finally {
        isLoading.value = false;
    }
};

const handleDeleteScheme = async (scheme: LabelScheme) => {
    schemeToDelete.value = scheme;
    deletionImpact.value = null; // Reset previous data
    isDeleteModalOpen.value = true; // Open modal immediately
    
    try {
        isDeletionImpactLoading.value = true;
        const projectId = Number(route.params.projectId);
        deletionImpact.value = await labelSchemeService.getDeletionImpact(projectId, scheme.labelSchemeId);
    } catch (error) {
        showError('Error', 'Failed to load deletion impact. Please try again.');
        logger.error('Failed to get deletion impact:', error);
        closeDeleteModal();
    } finally {
        isDeletionImpactLoading.value = false;
    }
};

const confirmDelete = async () => {
    if (!schemeToDelete.value) return;
    
    const projectId = Number(route.params.projectId);
    
    try {
        isLoading.value = true;
        await labelSchemeService.softDeleteLabelScheme(projectId, schemeToDelete.value.labelSchemeId);
        await fetchLabelSchemes();
        closeDeleteModal();
        showToast('Label Scheme Deleted', `Label Scheme "${schemeToDelete.value.name}" has been deleted successfully.`);
    } catch (error) {
        showError('Error', 'Failed to delete label scheme. Please try again.');
        logger.error('Failed to delete label scheme:', error);
    } finally {
        isLoading.value = false;
    }
};

const handleReactivateScheme = async (scheme: LabelScheme) => {
    const projectId = Number(route.params.projectId);
    
    try {
        isLoading.value = true;
        await labelSchemeService.reactivateLabelScheme(projectId, scheme.labelSchemeId);
        await fetchLabelSchemes();
        showToast('Label Scheme Reactivated', `Label Scheme "${scheme.name}" has been reactivated successfully.`);
    } catch (error) {
        showError('Error', 'Failed to reactivate label scheme. Please try again.');
        logger.error('Failed to reactivate label scheme:', error);
    } finally {
        isLoading.value = false;
    }
};

onMounted(async () => {
    await fetchLabelSchemes();
});
</script>

<style scoped>
.page-header {
    margin-bottom: 2rem;
    h1 {
        font-size: 1.5rem;
        margin-bottom: 0.25rem;
    }
    p {
        color: var(--color-gray-600);
        margin-bottom: 1rem;
    }
}

.schemes-list {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.loading-state {
    text-align: center;
    padding: 2rem;
    color: var(--color-gray-600);
}

.loading-deletion-impact {
    text-align: center;
    padding: 2rem;
    min-width: 400px;
}

.loading-content {
    margin-bottom: 2rem;
}

.loading-content p {
    margin: 0.5rem 0;
    color: var(--color-gray-700);
}

.loading-subtext {
    font-size: 0.875rem;
    color: var(--color-gray-500) !important;
}

.loading-actions {
    display: flex;
    justify-content: center;
}

.spinner {
    display: inline-block;
    width: 2rem;
    height: 2rem;
    border: 3px solid var(--color-gray-200);
    border-top: 3px solid var(--color-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem auto;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

</style>