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
            <CreateWorkflowWizard 
                :project-id="Number(route.params.projectId)" 
                @cancel="closeModal" 
                @submit="handleCreateWorkflow" 
            />
        </ModalWindow>
    </div>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import WorkflowCard from '@/components/project/workflow/WorkflowCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateWorkflowWizard from '@/components/project/workflow/CreateWorkflowWizard.vue';
import Button from '@/components/common/Button.vue';
import {type CreateWorkflowWithStagesRequest, type Workflow} from '@/types/workflow';
import {workflowService} from '@/services/api/workflows';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/utils/logger';
import {useConfirm} from '@/composables/useConfirm';
import {useToast} from '@/composables/useToast';
import {useErrorHandler} from '@/composables/useErrorHandler';

const logger = AppLogger.createComponentLogger('WorkflowsView');
const route = useRoute();
const router = useRouter();
const { showAlert } = useAlert();
const { showConfirm } = useConfirm();
const { showCreateSuccess, showDeleteSuccess, showError } = useToast();
const { handleError } = useErrorHandler();

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
        workflows.value = response.data;
        logger.info(`Loaded ${response.data.length} workflows for project ${projectId}`);
    } catch (error) {
        handleError(error, 'Failed to load workflows');
    } finally {
        isLoading.value = false;
    }
};

const handleCreateWorkflow = async (formData: CreateWorkflowWithStagesRequest) => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        showError('Error', 'Invalid project ID');
        return;
    }

    // Check if workflow with same name already exists
    const existingWorkflow = workflows.value.find(w => 
        w.name.toLowerCase().trim() === formData.name.toLowerCase().trim()
    );
    
    if (existingWorkflow) {
        showError('Duplicate Name', `A workflow named "${formData.name}" already exists. Please choose a different name.`);
        return;
    }

    isCreating.value = true;
    try {
        const newWorkflow = await workflowService.createWorkflowWithStages(projectId, formData);
        workflows.value.push(newWorkflow);
        logger.info(`Created workflow with stages: ${newWorkflow.name} (ID: ${newWorkflow.id})`);
        closeModal();
        showCreateSuccess("Workflow", newWorkflow.name);
    } catch (error) {
        handleError(error, 'Failed to create workflow');
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
    const confirmed = await showConfirm('Confirm Deletion', `Are you sure you want to delete the workflow "${workflow.name}"? This action cannot be undone.`);
    if (!confirmed) return;

    const projectId = Number(route.params.projectId);
    try {
        await workflowService.deleteWorkflow(projectId, workflow.id);
        workflows.value = workflows.value.filter(w => w.id !== workflow.id);
        logger.info(`Deleted workflow: ${workflow.name} (ID: ${workflow.id})`);
        showDeleteSuccess("Workflow", workflow.name);
    } catch (error) {
        handleError(error, 'Failed to delete workflow');
    }
};

const handleManageStages = async (workflow: Workflow) => {
    logger.info(`Navigating to pipeline view for workflow: ${workflow.name}`);
    await router.push({
        name: 'WorkflowPipeline',
        params: {
            projectId: route.params.projectId,
            workflowId: workflow.id
        }
    });
};

onMounted(() => {
    loadWorkflows();
});
</script>

<style lang="scss" scoped>
.page-header {
    margin-bottom: 2rem;
    h1 {
        font-size: 1.5rem;
        margin-bottom: 0.25rem;
    }
    p {
        color: var(--color-gray-600);
        margin-bottom: 1rem;
        max-width: 80ch;
    }
}

.workflows-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
    gap: 1.5rem;
}

.no-content-message,
.loading-message {
    color: var(--color-gray-600);
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
    bottom: 2rem;
    right: 2rem;
    width: 60px;
    height: 60px;
    border-radius: 50%;
    background-color: var(--color-primary);
    color: var(--color-white);
    border: none;
    font-size: 2.5rem;
    line-height: 1;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.1);
    cursor: pointer;
    display: flex;
    justify-content: center;
    align-items: center;
    padding-bottom: 4px;
    transition: background-color 0.2s ease-in-out, transform 0.2s ease-in-out;
    animation: fab-enter 0.2s ease-out 0.35s backwards;

    &:hover {
        background-color: var(--color-primary-hover);
        transform: scale(1.1);
        transition: transform 0.2s ease, background-color 0.3s ease;
    }
}

.workflows-page.fade-slide-leave-active .fab {
    opacity: 0;
}

// Make modal larger for workflow wizard
:deep(.modal-window) {
    max-width: 1000px;
    width: 90vw;
}
</style>