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
                />
                <p v-if="!labelSchemes || labelSchemes.length === 0">
                    No label schemes have been created for this project yet.
                </p>
            </template>
        </div>

        <Button 
            class="fab" 
            @click="openModal" 
            :disabled="isLoading"
            aria-label="Create New Scheme"
        >
            +
        </Button>

        <ModalWindow 
            :is-open="isModalOpen" 
            title="Create New Label Scheme" 
            @close="closeModal" 
            :hide-footer="true"
        >
            <CreateLabelSchemeForm 
                @cancel="closeModal" 
                @save="handleCreateScheme" 
                :disabled="isLoading"
            />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import LabelSchemeCard from '@/components/labels/LabelSchemeCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateLabelSchemeForm from '@/components/labels/CreateLabelSchemeForm.vue';
import Button from '@/components/common/Button.vue';
import type { LabelScheme, FormPayloadLabelScheme } from '@/types/label/labelScheme';
import { labelSchemeService } from '@/services/api/labelSchemeService';
import { labelService } from '@/services/api/labelService';
import { useAlert } from '@/composables/useAlert';
import { useToast } from '@/composables/useToast';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createComponentLogger('LabelSchemesView');

const route = useRoute();
const { showAlert } = useAlert();
const { showCreateSuccess, showWarning, showError } = useToast();

const labelSchemes = ref<LabelScheme[]>([]);
const isModalOpen = ref(false);
const isLoading = ref(false);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

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
        showCreateSuccess('Label Scheme');
    } catch (error) {
        showError('Error', 'Failed to create label scheme. Please try again.');
        logger.error('Failed to create label scheme:', error);
    } finally {
        isLoading.value = false;
    }
};

onMounted(async () => {
    await fetchLabelSchemes();
});
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.page-header {
    margin-bottom: vars.$margin-xlarge;
    h1 {
        font-size: vars.$font-size-xlarge;
        margin-bottom: vars.$padding-xsmall;
    }
    p {
        color: vars.$theme-text-light;
        margin-bottom: vars.$margin-medium;
    }
}

.schemes-list {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-large;
}

.loading-state {
    text-align: center;
    padding: vars.$padding-xlarge;
    color: vars.$theme-text-light;
}

@keyframes fab-enter {
  from {
    transform: scale(0);
    opacity: 0;
  }
  to {
    transform: scale(1);
    opacity: 1;
  }
}

.fab {
    position: absolute;
    bottom: vars.$padding-xlarge;
    right: vars.$padding-xlarge;
    width: 60px;
    height: 60px;
    border-radius: 50%;
    background-color: vars.$color-primary;
    color: vars.$color-white;
    border: none;
    font-size: 2.5rem;
    line-height: 1;
    box-shadow: vars.$shadow-md;
    cursor: pointer;
    display: flex;
    justify-content: center;
    align-items: center;
    padding-bottom: 4px;
    transition: background-color 0.2s ease-in-out, transform 0.2s ease-in-out;
    animation: fab-enter 0.2s ease-out 0.35s backwards;

    &:hover {
        background-color: vars.$color-primary-hover;
        transform: scale(1.1);
        transition: transform 0.2s ease, background-color 0.3s ease;
    }
}

.label-schemes-page.fade-slide-leave-active .fab {
    opacity: 0;
}
</style>