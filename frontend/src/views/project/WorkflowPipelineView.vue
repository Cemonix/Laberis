<template>
    <div class="workflow-pipeline-view">
        <div class="view-header">
            <div class="header-left">
                <h2>Pipeline Viewer</h2>
            </div>
        </div>

        <div class="pipeline-container">
            <WorkflowPipelineViewer
                :workflow-id="workflowId"
                :workflow-name="workflowName"
                :stages="pipelineStages"
                :can-edit="true"
                :is-loading="isLoading"
                :error="errorMessage"
                @edit-pipeline="handleEditPipeline"
                @stage-click="handleStageClick"
                @edit-stage="handleEditStage"
                @manage-assignments="handleManageAssignments"
                @refresh="loadPipelineData"
            />
        </div>
        
        <!-- Stage Assignment Management Modal -->
        <StageAssignmentModal
            v-if="showAssignmentModal && selectedStage"
            :stage="selectedStage"
            :project-id="projectId"
            :workflow-id="workflowId"
            @close="closeAssignmentModal"
            @updated="handleAssignmentUpdated"
        />
        
        <!-- Stage Edit Modal -->
        <StageEditModal
            v-if="showEditModal && selectedStage"
            :stage="selectedStage"
            :project-id="projectId"
            :workflow-id="workflowId"
            @close="closeEditModal"
            @updated="handleStageUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import {onMounted, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import WorkflowPipelineViewer from '@/components/project/workflow/WorkflowPipelineViewer.vue';
import StageAssignmentModal from '@/components/project/workflow/StageAssignmentModal.vue';
import StageEditModal from '@/components/project/workflow/StageEditModal.vue';
import type {WorkflowStagePipeline, WorkflowStage, WorkflowStageConnection} from '@/types/workflow';
import {workflowStageService} from '@/services/api/projects';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {AppLogger} from '@/utils/logger';

const logger = AppLogger.createComponentLogger('WorkflowPipelineView');
const route = useRoute();
const router = useRouter();
const { handleError } = useErrorHandler();

const workflowId = ref<number>(parseInt(route.params.workflowId as string));
const projectId = ref<number>(parseInt(route.params.projectId as string));
const workflowName = ref<string>('');
const pipelineStages = ref<WorkflowStagePipeline[]>([]);
const isLoading = ref<boolean>(true);
const errorMessage = ref<string | null>(null);

// Modal state
const showAssignmentModal = ref<boolean>(false);
const showEditModal = ref<boolean>(false);
const selectedStage = ref<WorkflowStagePipeline | null>(null);

const loadPipelineData = async () => {
    if (!workflowId.value || isNaN(workflowId.value)) {
        errorMessage.value = 'Invalid workflow ID';
        isLoading.value = false;
        return;
    }

    if (!projectId.value || isNaN(projectId.value)) {
        errorMessage.value = 'Invalid project ID';
        isLoading.value = false;
        return;
    }

    isLoading.value = true;
    errorMessage.value = null;
    
    try {
        logger.info(`Loading pipeline data for workflow ${workflowId.value} in project ${projectId.value}`);
        
        const workflowWithStages = await workflowStageService.getWorkflowWithStages(
            projectId.value, 
            workflowId.value
        );
        
        workflowName.value = workflowWithStages.name;
        
        // Transform WorkflowStage[] to WorkflowStagePipeline[]
        pipelineStages.value = workflowWithStages.stages.map((stage: WorkflowStage) => ({
            id: stage.id,
            name: stage.name,
            description: stage.description,
            stageOrder: stage.stageOrder,
            stageType: stage.stageType,
            isInitialStage: stage.isInitialStage,
            isFinalStage: stage.isFinalStage,
            previousStageIds: stage.incomingConnections?.map((conn: WorkflowStageConnection) => conn.fromStageId) || [],
            nextStageIds: stage.outgoingConnections?.map((conn: WorkflowStageConnection) => conn.toStageId) || [],
            assignedUserCount: stage.assignments?.length || 0,
        }));
        
        logger.info(`Successfully loaded ${pipelineStages.value.length} stages for workflow "${workflowName.value}"`);
        
    } catch (error) {
        logger.error('Error loading pipeline data', error);
        errorMessage.value = 'Failed to load workflow pipeline data';
        handleError(error, 'Loading workflow pipeline');
    } finally {
        isLoading.value = false;
    }
};

const handleEditPipeline = () => {
    logger.debug('Edit pipeline requested');
    // TODO: Navigate to edit mode or show edit modal
};

const handleStageClick = (stage: WorkflowStagePipeline) => {
    logger.info('Stage clicked, navigating to tasks view', { stageId: stage.id, stageName: stage.name });
    router.push({
        name: 'StageTasks',
        params: {
            projectId: projectId.value,
            workflowId: workflowId.value,
            stageId: stage.id
        }
    });
};

const handleEditStage = (stage: WorkflowStagePipeline) => {
    logger.debug('Edit stage requested', { stageId: stage.id, stageName: stage.name });
    selectedStage.value = stage;
    showEditModal.value = true;
};

const handleManageAssignments = (stage: WorkflowStagePipeline) => {
    logger.debug('Manage assignments requested', { stageId: stage.id, stageName: stage.name });
    selectedStage.value = stage;
    showAssignmentModal.value = true;
};

const closeAssignmentModal = () => {
    showAssignmentModal.value = false;
    selectedStage.value = null;
};

const closeEditModal = () => {
    showEditModal.value = false;
    selectedStage.value = null;
};

const handleAssignmentUpdated = () => {
    logger.info('Stage assignments updated, refreshing pipeline data');
    loadPipelineData();
    closeAssignmentModal();
};

const handleStageUpdated = () => {
    logger.info('Stage updated, refreshing pipeline data');
    loadPipelineData();
    closeEditModal();
};

onMounted(() => {
    loadPipelineData();
});
</script>

<style lang="scss" scoped>
.workflow-pipeline-view {
    display: flex;
    flex-direction: column;
    height: 100%; 
    min-height: 0;
}

.view-header {
    flex-shrink: 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    background-color: var(--color-white);
    border-bottom: 1px solid var(--color-gray-400);
    
    .header-left {
        display: flex;
        align-items: center;
        gap: 1rem;
    }
    
    h2 {
        margin: 0;
        color: var(--color-gray-800);
        font-size: 1.5rem;
    }
}

.view-actions {
    display: flex;
    gap: 1rem;
}

.view-actions {
    display: flex;
    gap: 1rem;
}

.pipeline-container {
    flex-grow: 1;
    position: relative;
    overflow: hidden;
}
</style>