<template>
    <div class="workflow-pipeline-view">
        <div class="view-header">
            <div class="header-left">
                <Button 
                    variant="secondary" 
                    size="small"
                    @click="goBackToWorkflows"
                >
                    ← Back to Workflows
                </Button>
                <h2>Workflow Pipeline Builder</h2>
            </div>
            <div class="view-actions">
                <Button
                    variant="primary"
                    @click="showCreateStageModal = true"
                >
                    Add Stage
                </Button>
                <Button
                    variant="secondary"
                    @click="showConnectionModal = true"
                >
                    Add Connection
                </Button>
            </div>
        </div>

        <div class="pipeline-container">
            <WorkflowPipelineViewer
                :workflow-id="workflowId"
                :workflow-name="workflowName"
                :stages="pipelineStages"
                :can-edit="true"
                @edit-pipeline="handleEditPipeline"
                @refresh="loadPipelineData"
            />
        </div>

        <!-- Placeholder modals - will implement these next -->
        <div v-if="showCreateStageModal" class="modal-overlay" @click="showCreateStageModal = false">
            <div class="modal-content" @click.stop>
                <h3>Add New Stage</h3>
                <p>Stage creation form will be implemented here</p>
                <Button @click="showCreateStageModal = false">Close</Button>
            </div>
        </div>

        <div v-if="showConnectionModal" class="modal-overlay" @click="showConnectionModal = false">
            <div class="modal-content" @click.stop>
                <h3>Add Stage Connection</h3>
                <p>Connection creation form will be implemented here</p>
                <Button @click="showConnectionModal = false">Close</Button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from '@/components/common/Button.vue';
import WorkflowPipelineViewer from '@/components/project/workflow/WorkflowPipelineViewer.vue';
import type { WorkflowStagePipeline } from '@/types/workflow';
import { WorkflowStageType } from '@/types/workflow';

const route = useRoute();
const router = useRouter();

// Get workflow ID from route params
const workflowId = ref<number>(parseInt(route.params.workflowId as string));
const workflowName = ref<string>('Sample Workflow');

// Modal states
const showCreateStageModal = ref(false);
const showConnectionModal = ref(false);

// Mock data for demonstration
const pipelineStages = ref<WorkflowStagePipeline[]>([
    {
        id: 1,
        name: 'Data Import',
        description: 'Import raw data from source',
        stageOrder: 1,
        stageType: WorkflowStageType.ANNOTATION,
        isInitialStage: true,
        isFinalStage: false,
        previousStageIds: [],
        nextStageIds: [2],
        assignedUserCount: 2,
        positionX: 50,
        positionY: 100
    },
    {
        id: 2,
        name: 'Annotation',
        description: 'Annotate imported data',
        stageOrder: 2,
        stageType: WorkflowStageType.ANNOTATION,
        isInitialStage: false,
        isFinalStage: false,
        previousStageIds: [1],
        nextStageIds: [3],
        assignedUserCount: 5,
        positionX: 350,
        positionY: 100
    },
    {
        id: 3,
        name: 'Review',
        description: 'Review annotated data for quality',
        stageOrder: 3,
        stageType: WorkflowStageType.REVIEW,
        isInitialStage: false,
        isFinalStage: false,
        previousStageIds: [2],
        nextStageIds: [4, 2], // Can go to final acceptance or back to annotation
        assignedUserCount: 2,
        positionX: 650,
        positionY: 100
    },
    {
        id: 4,
        name: 'Final Acceptance',
        description: 'Final acceptance of reviewed data',
        stageOrder: 4,
        stageType: WorkflowStageType.ACCEPTED,
        isInitialStage: false,
        isFinalStage: true,
        previousStageIds: [3],
        nextStageIds: [],
        assignedUserCount: 1,
        positionX: 950,
        positionY: 100
    }
]);

// Methods
const loadPipelineData = async () => {
    // TODO: Load actual data from API
    console.log('Loading pipeline data for workflow:', workflowId.value);
};

const handleEditPipeline = () => {
    console.log('Edit pipeline requested');
    // TODO: Implement pipeline editing functionality
};

const goBackToWorkflows = () => {
    router.push({
        name: 'ProjectWorkflows',
        params: { projectId: route.params.projectId }
    });
};

// Lifecycle
onMounted(() => {
    loadPipelineData();
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workflow-pipeline-view {
    display: flex;
    flex-direction: column;
    background-color: vars.$theme-background;
}

.view-header {
    display: flex;
    height: 100%;
    justify-content: space-between;
    align-items: center;
    padding: vars.$padding-large;
    background-color: vars.$theme-surface;
    border-bottom: 1px solid vars.$theme-border;
    
    .header-left {
        display: flex;
        align-items: center;
        gap: vars.$gap-medium;
    }
    
    h2 {
        margin: 0;
        color: vars.$theme-text;
        font-size: vars.$font-size-xlarge;
    }
}

.view-actions {
    display: flex;
    gap: vars.$gap-medium;
}

.pipeline-container {
    flex: 1;
    padding: vars.$padding-medium;
    overflow: hidden;
}

.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
}

.modal-content {
    background-color: vars.$color-white;
    padding: vars.$padding-large;
    border-radius: vars.$border-radius-md;
    box-shadow: vars.$shadow-lg;
    max-width: 500px;
    width: 90%;
    
    h3 {
        margin: 0 0 vars.$margin-medium 0;
        color: vars.$theme-text;
    }
    
    p {
        margin: 0 0 vars.$margin-medium 0;
        color: vars.$theme-text-light;
    }
}
</style>
