<template>
    <Card class="scheme-card">
        <template #header>
            <h3 class="scheme-name">{{ scheme.name }}</h3>
            <div class="card-actions">
                <Button variant="primary" @click="openAddLabelModal">Add Label</Button>
                <Button variant="secondary">Edit Scheme</Button>
            </div>
        </template>

        <p v-if="scheme.description" class="scheme-description">{{ scheme.description }}</p>
        
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
import { ref, onMounted } from 'vue';
import type { LabelScheme } from '@/types/label/labelScheme';
import type { Label } from '@/types/label/label';
import type { CreateLabelRequest } from '@/types/label/requests';
import { labelService } from '@/services/api/labelService';
import { useAlert } from '@/composables/useAlert';
import LabelChip from './LabelChip.vue';
import CreateLabelForm from './CreateLabelForm.vue';
import Button from '@/components/common/Button.vue';
import Card from '@/components/common/Card.vue';
import ModalWindow from '@/components/common/modals/ModalWindow.vue';

const props = defineProps<{
    scheme: LabelScheme;
}>();

const labels = ref<Label[]>([]);
const isLoadingLabels = ref(false);
const isAddLabelModalOpen = ref(false);

const { showAlert } = useAlert();

const openAddLabelModal = () => {
    isAddLabelModalOpen.value = true;
};

const closeAddLabelModal = () => {
    isAddLabelModalOpen.value = false;
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
        await showAlert('Success', 'Label created successfully!');
    } catch (error) {
        await showAlert('Error', 'Failed to create label. Please try again.');
        console.error('Failed to create label:', error);
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
        console.error('Failed to fetch labels for scheme:', error);
        labels.value = [];
    } finally {
        isLoadingLabels.value = false;
    }
};

onMounted(fetchLabels);
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.scheme-name {
    font-size: vars.$font-size-large;
}

.card-actions {
    display: flex;
    gap: vars.$gap-small;
}

.scheme-description {
    font-style: italic;
    color: vars.$theme-text-light;
    margin-bottom: vars.$margin-medium;
}

.labels-container {
    display: flex;
    flex-wrap: wrap;
    gap: vars.$gap-small;
}

.loading-labels {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-small;
    font-style: italic;
}

.no-labels {
    color: vars.$theme-text-light;
    font-size: vars.$font-size-small;
}
</style>