<template>
    <div class="workflows-page">
        <div class="page-header">
            <h1>Workflows</h1>
            <p>Define and manage annotation workflows that guide tasks through different stages from initial import to final acceptance.</p>
        </div>
        
        <div class="workflows-list">
            <div v-if="isLoading" class="loading-message">
                Loading workflows...
            </div>
            <template v-else>
                <WorkflowCard
                    v-for="workflow in workflows"
                    :key="workflow.id"
                    :workflow="workflow"
                    @edit="handleEditWorkflow"
                    @delete="handleDeleteWorkflow"
                    @manage-stages="handleManageStages"
                />
                <p v-if="!workflows || workflows.length === 0" class="no-content-message">
                    No workflows have been created for this project yet.
                </p>
            </template>
        </div>

        <Button class="fab" @click="openModal" aria-label="Create New Workflow">+</Button>

        <ModalWindow :is-open="isModalOpen" title="Create New Workflow" @close="closeModal" :hide-footer="true">
            <CreateWorkflowForm 
                :project-id="Number(route.params.projectId)" 
                @cancel="closeModal" 
                @save="handleCreateWorkflow" 
            />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import WorkflowCard from '@/components/project/WorkflowCard.vue';
import ModalWindow from '@/components/common/modals/ModalWindow.vue';
import CreateWorkflowForm from '@/components/project/CreateWorkflowForm.vue';
import Button from '@/components/common/Button.vue';
import { type Workflow, type CreateWorkflowRequest } from '@/types/workflow';
import { workflowService } from '@/services/api/workflowService';
import { useAlert } from '@/composables/useAlert';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('WorkflowsView');
const route = useRoute();
const { showAlert } = useAlert();

const workflows = ref<Workflow[]>([]);
const isModalOpen = ref(false);
const isLoading = ref(false);
const isCreating = ref(false);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const loadWorkflows = async () => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        await showAlert('Error', 'Invalid project ID');
        return;
    }

    isLoading.value = true;
    try {
        const response = await workflowService.getWorkflows(projectId);
        workflows.value = response;
        logger.info(`Loaded ${response.length} workflows for project ${projectId}`);
    } catch (error) {
        logger.error('Failed to load workflows', error);
        await showAlert('Error', 'Failed to load workflows. Please try again.');
    } finally {
        isLoading.value = false;
    }
};

const handleCreateWorkflow = async (formData: CreateWorkflowRequest) => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        await showAlert('Error', 'Invalid project ID');
        return;
    }

    isCreating.value = true;
    try {
        const newWorkflow = await workflowService.createWorkflow(projectId, formData);
        workflows.value.push(newWorkflow);
        logger.info(`Created workflow: ${newWorkflow.name} (ID: ${newWorkflow.id})`);
        closeModal();
        await showAlert('Success', `Workflow "${newWorkflow.name}" created successfully!`);
    } catch (error) {
        logger.error('Failed to create workflow', error);
        await showAlert('Error', `Failed to create workflow: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
        isCreating.value = false;
    }
};

const handleEditWorkflow = async (workflow: Workflow) => {
    // TODO: Implement edit workflow functionality
    logger.info(`Edit workflow requested for: ${workflow.name}`);
    await showAlert('Info', 'Edit workflow functionality coming soon!');
};

const handleDeleteWorkflow = async (workflow: Workflow) => {
    const confirmed = confirm(`Are you sure you want to delete the workflow "${workflow.name}"? This action cannot be undone.`);
    
    if (!confirmed) return;

    const projectId = Number(route.params.projectId);
    try {
        await workflowService.deleteWorkflow(projectId, workflow.id);
        workflows.value = workflows.value.filter(w => w.id !== workflow.id);
        logger.info(`Deleted workflow: ${workflow.name} (ID: ${workflow.id})`);
        await showAlert('Success', `Workflow "${workflow.name}" deleted successfully!`);
    } catch (error) {
        logger.error('Failed to delete workflow', error);
        await showAlert('Error', `Failed to delete workflow: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
};

const handleManageStages = async (workflow: Workflow) => {
    // TODO: Navigate to workflow stages management
    logger.info(`Manage stages requested for workflow: ${workflow.name}`);
    // For now, we'll show an info message. Later we'll navigate to a stages management page
    await showAlert('Info', 'Workflow stages management coming soon!');
};

onMounted(() => {
    loadWorkflows();
});
</script>

<style lang="scss" scoped>
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
        max-width: 80ch;
    }
}

.workflows-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
    gap: vars.$gap-large;
}

.no-content-message,
.loading-message {
    color: vars.$theme-text-light;
    font-style: italic;
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

.workflows-page.fade-slide-leave-active .fab {
    opacity: 0;
}
</style>