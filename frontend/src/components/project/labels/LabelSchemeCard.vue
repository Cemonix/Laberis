<template>
    <Card class="scheme-card" :data-inactive="!scheme.isActive">
        <template #header>
            <h3 class="scheme-name">{{ scheme.name }}</h3>
            <div class="card-actions">
                <Button 
                    v-permission="{ permission: PERMISSIONS.LABEL_SCHEME.CREATE }"
                    variant="primary" 
                    @click="openAddLabelModal" 
                    :disabled="!scheme.isActive"
                >
                    Add Label
                </Button>
                <Button 
                    v-permission="{ permission: PERMISSIONS.LABEL_SCHEME.UPDATE }"
                    variant="secondary" 
                    @click="handleEditScheme" 
                    :disabled="!scheme.isActive"
                >
                    Edit Scheme
                </Button>
                <Button 
                    v-permission="{ permission: PERMISSIONS.LABEL_SCHEME.DELETE }"
                    variant="danger" 
                    @click="handleDeleteScheme"
                >
                    {{ scheme.isActive ? 'Delete' : 'Reactivate' }}
                </Button>
            </div>
        </template>

        <p v-if="scheme.description" class="scheme-description">{{ scheme.description }}</p>
        <div v-if="!scheme.isActive" class="soft-deleted-notice">
            <p>⚠️ This scheme has been deleted. Existing annotations remain, but no new annotations can be created with these labels.</p>
        </div>
        
        <div class="labels-container">
            <div v-if="isLoadingLabels" class="loading-labels">
                Loading labels...
            </div>
            <template v-else>
                <LabelChip
                    v-for="label in labels"
                    :key="label.labelId"
                    :label="label"
                />
                <p v-if="!labels || labels.length === 0" class="no-labels">
                    This scheme has no labels.
                </p>
            </template>
        </div>

        <!-- Add Label Modal -->
        <ModalWindow 
            :is-open="isAddLabelModalOpen" 
            title="Add New Label" 
            @close="closeAddLabelModal" 
            :hide-footer="true"
        >
            <CreateLabelForm 
                @cancel="closeAddLabelModal" 
                @save="handleCreateLabel" 
                :disabled="isLoadingLabels"
            />
        </ModalWindow>
    </Card>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue';
import type {LabelScheme} from '@/types/label/labelScheme';
import type {Label} from '@/types/label/label';
import type {CreateLabelRequest} from '@/types/label/requests';
import {labelService} from '@/services/api/projects/labelService';
import {useToast} from '@/composables/useToast';
import LabelChip from './LabelChip.vue';
import CreateLabelForm from './CreateLabelForm.vue';
import Button from '@/components/common/Button.vue';
import Card from '@/components/common/Card.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import {AppLogger} from '@/utils/logger';
import {PERMISSIONS} from '@/types/permissions';

const logger = AppLogger.createComponentLogger('LabelSchemeCard');

const props = defineProps<{
    scheme: LabelScheme;
}>();

const emits = defineEmits<{
    editScheme: [scheme: LabelScheme];
    deleteScheme: [scheme: LabelScheme];
    reactivateScheme: [scheme: LabelScheme];
}>();

const labels = ref<Label[]>([]);
const isLoadingLabels = ref(false);
const isAddLabelModalOpen = ref(false);

const { showCreateSuccess, showError } = useToast();

const openAddLabelModal = () => {
    isAddLabelModalOpen.value = true;
};

const closeAddLabelModal = () => {
    isAddLabelModalOpen.value = false;
};

const handleEditScheme = () => {
    emits('editScheme', props.scheme);
};

const handleDeleteScheme = () => {
    if (props.scheme.isActive) {
        emits('deleteScheme', props.scheme);
    } else {
        emits('reactivateScheme', props.scheme);
    }
};

const handleCreateLabel = async (formData: CreateLabelRequest) => {
    try {
        isLoadingLabels.value = true;
        
        const newLabel = await labelService.createLabel(
            props.scheme.projectId,
            props.scheme.labelSchemeId,
            formData
        );
        
        // Add the new label to the current list
        labels.value.push(newLabel);
        
        // Don't close modal - let user create multiple labels
        showCreateSuccess('Label');
    } catch (error) {
        showError('Error', 'Failed to create label. Please try again.');
        logger.error('Failed to create label:', error);
    } finally {
        isLoadingLabels.value = false;
    }
};

const fetchLabels = async () => {
    // If labels are already provided with the scheme, use them
    if (props.scheme.labels) {
        labels.value = props.scheme.labels;
        return;
    }

    try {
        isLoadingLabels.value = true;
        const result = await labelService.getLabelsForScheme(
            props.scheme.projectId, 
            props.scheme.labelSchemeId
        );
        labels.value = result.data;
    } catch (error) {
        logger.error('Failed to fetch labels for scheme:', error);
        labels.value = [];
    } finally {
        isLoadingLabels.value = false;
    }
};

onMounted(fetchLabels);
</script>

<style scoped>
.scheme-name {
    font-size: 1.25rem;
}

.card-actions {
    display: flex;
    gap: 0.5rem;
}

.scheme-description {
    font-style: italic;
    color: var(--color-gray-600);
    margin-bottom: 1rem;
}

.labels-container {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}

.loading-labels {
    color: var(--color-gray-600);
    font-size: 0.875rem;
    font-style: italic;
}

.no-labels {
    color: var(--color-gray-600);
    font-size: 0.875rem;
}

.soft-deleted-notice {
    background-color: var(--color-yellow-50);
    border: 1px solid var(--color-yellow-300);
    border-radius: 0.375rem;
    padding: 0.75rem;
    margin-bottom: 1rem;
}

.soft-deleted-notice p {
    color: var(--color-yellow-800);
    margin: 0;
    font-size: 0.875rem;
}

.scheme-card[data-inactive="true"] {
    opacity: 0.7;
    border-color: var(--color-gray-300);
}
</style>