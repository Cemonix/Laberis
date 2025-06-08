<template>
    <div class="label-setup-page">
        <div class="page-header">
            <h1>Label Schemes</h1>
            <p>Manage the label schemes and labels for this project.</p>
        </div>
        
        <div class="schemes-list">
            <LabelSchemeCard
            v-for="scheme in labelSchemes"
            :key="scheme.labelSchemeId"
            :scheme="scheme"
            />
            <p v-if="!labelSchemes || labelSchemes.length === 0">
                No label schemes have been created for this project yet.
            </p>
        </div>

        <button class="fab" @click="openModal" aria-label="Create New Scheme">+</button>

        <ModalWindow :is-open="isModalOpen" title="Create New Label Scheme" @close="closeModal" :hide-footer="true">
            <CreateLabelSchemeForm @cancel="closeModal" @save="handleCreateScheme" />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import LabelSchemeCard from '@/components/labels/LabelSchemeCard.vue';
import ModalWindow from '@/components/common/ModalWindow.vue';
import CreateLabelSchemeForm from '@/components/labels/CreateLabelSchemeForm.vue';
import type { LabelScheme, FormPayloadLabelScheme } from '@/types/label/labelScheme';
import { useWorkspaceStore } from '@/stores/workspaceStore';

const route = useRoute();
const workspaceStore = useWorkspaceStore();

const labelSchemes = ref<LabelScheme[]>([]);
const isModalOpen = ref(false);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const handleCreateScheme = (formData: FormPayloadLabelScheme) => {
    const projectId = Number(route.params.projectId);

    if (labelSchemes.value.some(scheme => scheme.name.toLowerCase() === formData.name.toLowerCase())) {
        alert('This label name already exists in the list.');
        return;
    }

    const newScheme: LabelScheme = {
        labelSchemeId: Date.now(), // Mock ID
        name: formData.name,
        description: formData.description,
        labels: formData.labels.map((label, index) => ({
            ...label,
            labelId: Date.now() + index, // Mock label ID
            labelSchemeId: 0 // This would be set by the backend
        })),
        projectId: projectId,
        isDefault: false,
    };
    labelSchemes.value.push(newScheme);

    // TODO: API service here
    // await labelSchemeService.createScheme(projectId, formData);
    // await fetchLabelSchemes(); // And then refresh the list from the server

    closeModal(); // Close the modal on success
};

onMounted(() => {
    const mockScheme = workspaceStore.getCurrentLabelScheme;
    if (mockScheme) {
        labelSchemes.value = [mockScheme];
    }
});
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;

.page-header {
    margin-bottom: vars.$margin-xlarge;
    h1 {
        font-size: vars.$font_size_xlarge;
        margin-bottom: vars.$padding-tiny;
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

.label-setup-page.fade-slide-leave-active .fab {
    opacity: 0;
}
</style>