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
                    :stages="workflowStages.get(workflow.id) || []"
                    :user-role="userRole || undefined"
                    :user-email="userEmail || undefined"
                />
                <p v-if="!workflows || workflows.length === 0" class="no-content-message">
                    No workflows have been created for this project yet.
                </p>
            </template>
        </div>

        <FloatingActionButton 
            :permission="PERMISSIONS.WORKFLOW.CREATE"
            :onClick="openModal" 
            aria-label="Create New Workflow"
            title="Create New Workflow"
        />

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
import {useRoute} from 'vue-router';
import WorkflowCard from '@/components/project/workflow/WorkflowCard.vue';
import ModalWindow from '@/components/common/modal/ModalWindow.vue';
import CreateWorkflowWizard from '@/components/project/workflow/CreateWorkflowWizard.vue';
import FloatingActionButton from '@/components/common/FloatingActionButton.vue';
import {type CreateWorkflowWithStagesRequest, type Workflow} from '@/services/project/workflow/workflow.types';
import {type WorkflowStage} from '@/services/project/workflow/workflowStage.types';
import {type ProjectRole} from '@/services/project/project.types';
import {workflowService} from '@/services/project/workflow/workflowService';
import {workflowStageService} from '@/services/project/workflow/workflowStageService';
import {projectMemberService} from '@/services/project';
import {useAlert} from '@/composables/useAlert';
import {AppLogger} from '@/core/logger/logger';
import {useToast} from '@/composables/useToast';
import {useErrorHandler} from '@/composables/useErrorHandler';
import { PERMISSIONS } from '@/services/auth/permissions.types';

const logger = AppLogger.createComponentLogger('WorkflowsView');
const route = useRoute();
const { showAlert } = useAlert();
const { showCreateSuccess, showError } = useToast();
const { handleError } = useErrorHandler();

const workflows = ref<Workflow[]>([]);
const workflowStages = ref<Map<number, WorkflowStage[]>>(new Map());
const userRole = ref<ProjectRole | null>(null);
const userEmail = ref<string | null>(null);
const isModalOpen = ref(false);
const isLoading = ref(false);
const isCreating = ref(false);

const openModal = () => isModalOpen.value = true;
const closeModal = () => isModalOpen.value = false;

const loadUserRole = async (projectId: number) => {
    try {
        const membership = await projectMemberService.getCurrentUserMembership(projectId);
        if (membership) {
            userRole.value = membership.role;
            userEmail.value = membership.email;
            logger.info(`User role in project ${projectId}: ${membership.role}`);
        } else {
            logger.warn(`User is not a member of project ${projectId}`);
        }
    } catch (error) {
        logger.error('Failed to load user role', error);
    }
};

const loadWorkflowStages = async (projectId: number, workflowId: number) => {
    try {
        const response = await workflowStageService.getWorkflowStages(projectId, workflowId);
        workflowStages.value.set(workflowId, response.data);
        logger.info(`Loaded ${response.data.length} stages for workflow ${workflowId}`);
    } catch (error) {
        logger.error(`Failed to load stages for workflow ${workflowId}`, error);
        workflowStages.value.set(workflowId, []); // Set empty array as fallback
    }
};

const loadWorkflows = async () => {
    const projectId = Number(route.params.projectId);
    if (!projectId || isNaN(projectId)) {
        logger.error('Invalid project ID in route params', route.params.projectId);
        await showAlert('Error', 'Invalid project ID');
        return;
    }

    isLoading.value = true;
    try {
        // Load user role and workflows in parallel
        const [workflowResponse] = await Promise.all([
            workflowService.getWorkflows(projectId),
            loadUserRole(projectId)
        ]);

        workflows.value = workflowResponse.data;
        logger.info(`Loaded ${workflowResponse.data.length} workflows for project ${projectId}`);

        // Load stages for each workflow
        const stagePromises = workflowResponse.data.map(workflow => 
            loadWorkflowStages(projectId, workflow.id)
        );
        await Promise.all(stagePromises);
        
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
        const newWorkflow = await workflowService.createWorkflow(projectId, formData);
        workflows.value.push(newWorkflow);
        logger.info(`Created workflow with stages: ${newWorkflow.name} (ID: ${newWorkflow.id})`);
        
        // Load stages for the newly created workflow
        await loadWorkflowStages(projectId, newWorkflow.id);
        
        closeModal();
        showCreateSuccess("Workflow", newWorkflow.name);
    } catch (error: any) {
        // Handle specific data source conflict errors
        if (error?.response?.status === 400 && error?.response?.data?.message) {
            const errorMessage = error.response.data.message as string;
            if (errorMessage.includes('cannot be used as input data source because it is already in use')) {
                showError('Data Source Conflict', errorMessage);
                return;
            }
        }
        // Fall back to generic error handler
        handleError(error, 'Failed to create workflow');
    } finally {
        isCreating.value = false;
    }
};

// Note: Edit, delete, and manage functions removed since WorkflowCard now handles navigation directly via role-based buttons

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


// Make modal larger for workflow wizard
:deep(.modal-window) {
    max-width: 1000px;
    width: 90vw;
}
</style>