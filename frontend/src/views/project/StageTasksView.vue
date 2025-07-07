<template>
    <div class="stage-tasks-view">
        <div class="view-header">
            <div class="header-left">
                <Button 
                    variant="secondary" 
                    @click="goBack"
                    class="back-button"
                >
                    ← Back to Pipeline
                </Button>
                <div class="stage-info">
                    <h2>{{ stageName }} Tasks</h2>
                    <p class="stage-description">{{ stageDescription || 'Manage tasks for this workflow stage' }}</p>
                </div>
            </div>
            <div class="view-actions">
                <Button 
                    variant="primary" 
                    @click="handleRefresh"
                    :disabled="isLoading"
                >
                    <font-awesome-icon :icon="faRefresh" />
                    Refresh
                </Button>
            </div>
        </div>

        <div class="tasks-container">
            <DataTable
                :data="taskTableData"
                :columns="tableColumns"
                :actions="getTableActions()"
                :row-actions="rowActions"
                :is-loading="isLoading"
                :error="errorMessage"
                :clickable-rows="true"
                :pagination="paginationData"
                title="Tasks"
                :empty-message="getEmptyMessage()"
                :empty-description="getEmptyDescription()"
                @action="handleTableAction"
                @row-action="handleRowAction"
                @row-click="handleTaskClick"
                @refresh="handleRefresh"
                @page-change="handlePageChange"
                @sort="handleSort"
            >
                <!-- Custom cell renderers -->
                <template #cell-priority="{ value }">
                    <div class="priority-cell" :class="getPriorityClass(value)">
                        <font-awesome-icon :icon="getPriorityIcon(value)" />
                        {{ getPriorityLabel(value) }}
                    </div>
                </template>

                <template #cell-status="{ value }">
                    <TaskStatusBadge :status="value" />
                </template>
                
                <template #cell-assignedTo="{ value }">
                    <div v-if="value" class="assigned-user">
                        <font-awesome-icon :icon="faUser" class="user-icon" />
                        {{ value }}
                    </div>
                    <div v-else class="unassigned">
                        <font-awesome-icon :icon="faUserSlash" class="unassigned-icon" />
                        Unassigned
                    </div>
                </template>
                
                <template #cell-dueDate="{ value }">
                    <div v-if="value" class="due-date" :class="getDueDateClass(value)">
                        <font-awesome-icon :icon="faCalendar" />
                        {{ formatDate(value) }}
                    </div>
                    <span v-else class="no-due-date">No due date</span>
                </template>
            </DataTable>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { 
    faRefresh, 
    faUser, 
    faUserSlash, 
    faCalendar,
    faEdit,
    faUserCog,
    faPlay,
    faTrash,
    faExclamationTriangle,
    faArrowUp,
    faMinus,
    faPlus
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import DataTable from '@/components/common/DataTable.vue';
import TaskStatusBadge from '@/components/project/task/TaskStatusBadge.vue';
import type { TaskTableRow } from '@/types/task';
import { TaskStatus } from '@/types/task';
import type { TableColumn, TableAction, TableRowAction } from '@/types/common';
import { useErrorHandler } from '@/composables/useErrorHandler';
import { taskService } from '@/services/api/taskService';
import { AppLogger } from '@/utils/logger';

const route = useRoute();
const router = useRouter();
const { handleError } = useErrorHandler();
const logger = AppLogger.createComponentLogger('StageTasksView');

const projectId = ref<number>(parseInt(route.params.projectId as string));
const workflowId = ref<number>(parseInt(route.params.workflowId as string));
const stageId = ref<number>(parseInt(route.params.stageId as string));
const stageName = ref<string>('');
const stageDescription = ref<string>('');

const tasks = ref<TaskTableRow[]>([]);
const isLoading = ref<boolean>(true);
const errorMessage = ref<string | null>(null);
const currentPage = ref<number>(1);
const pageSize = ref<number>(25);
const totalItems = ref<number>(0);
const sortKey = ref<string>('createdAt');
const sortDirection = ref<'asc' | 'desc'>('desc');
const hasAvailableAssets = ref<boolean>(true);
const availableAssetsCount = ref<number>(0);

// Table configuration
const tableColumns: TableColumn[] = [
    { key: 'assetName', label: 'Asset', sortable: true, width: '25%' },
    { key: 'priority', label: 'Priority', sortable: true, width: '10%', align: 'center' },
    { key: 'status', label: 'Status', sortable: true, width: '12%', align: 'center' },
    { key: 'assignedTo', label: 'Assigned To', sortable: true, width: '18%' },
    { key: 'dueDate', label: 'Due Date', sortable: true, width: '12%', format: 'date' },
    { key: 'createdAt', label: 'Created', sortable: true, width: '12%', format: 'datetime' },
    { key: 'completedAt', label: 'Completed', sortable: true, width: '11%', format: 'datetime' },
];

const getTableActions = (): TableAction[] => {
    const actions: TableAction[] = [
        { key: 'bulk-assign', label: 'Bulk Assign', icon: faUserCog, variant: 'secondary' },
    ];
    
    // Add "Import Assets" action if no assets are available
    if (!hasAvailableAssets.value) {
        actions.unshift({
            key: 'import-assets',
            label: 'Import Assets',
            icon: faPlus,
            variant: 'primary'
        });
    }
    
    return actions;
};

const rowActions: TableRowAction<TaskTableRow>[] = [
    { 
        key: 'open', 
        label: 'Open', 
        icon: faPlay, 
        variant: 'primary',
        disabled: (row: TaskTableRow) => row.status === TaskStatus.COMPLETED || row.status === TaskStatus.ARCHIVED
    },
    { key: 'edit', label: 'Edit', icon: faEdit, variant: 'secondary' },
    { key: 'assign', label: 'Assign', icon: faUserCog, variant: 'secondary' },
    { 
        key: 'delete', 
        label: 'Delete', 
        icon: faTrash, 
        variant: 'secondary',
        disabled: (row: TaskTableRow) => row.status === TaskStatus.IN_PROGRESS
    },
];

const taskTableData = computed((): TaskTableRow[] => {
    return tasks.value;
});

const paginationData = computed(() => ({
    currentPage: currentPage.value,
    totalPages: Math.ceil(totalItems.value / pageSize.value),
    pageSize: pageSize.value,
    totalItems: totalItems.value,
}));

const loadTasks = async () => {
    if (!projectId.value || !workflowId.value || !stageId.value) {
        errorMessage.value = 'Invalid parameters';
        isLoading.value = false;
        return;
    }

    isLoading.value = true;
    errorMessage.value = null;
    
    try {
        logger.info(`Loading tasks for stage ${stageId.value} in workflow ${workflowId.value}`);
        
        // First check if there are any available assets for task creation (non-blocking)
        try {
            const assetsCheck = await taskService.checkAssetsAvailable(projectId.value);
            hasAvailableAssets.value = assetsCheck.hasAssets;
            availableAssetsCount.value = assetsCheck.count;
        } catch (assetCheckError) {
            logger.warn('Failed to check assets availability, assuming assets are available', assetCheckError);
            hasAvailableAssets.value = false;
            availableAssetsCount.value = 0;
        }
        
        const params = {
            sortBy: sortKey.value,
            isAscending: sortDirection.value === 'asc',
            pageNumber: currentPage.value,
            pageSize: pageSize.value
        };

        const response = await taskService.getEnrichedTasksForStage(
            projectId.value,
            workflowId.value,
            stageId.value,
            params
        );
        
        tasks.value = response.tasks;
        totalItems.value = response.totalCount;
        stageName.value = response.stageName;
        stageDescription.value = response.stageDescription || 'Manage tasks for this workflow stage';
        
    } catch (error) {
        logger.error('Error loading tasks', error);
        errorMessage.value = 'Failed to load tasks';
        handleError(error, 'Loading tasks');
        
        // Fallback to mock data if backend fails
        logger.warn('Falling back to mock data...');
        await loadMockTasks();
    } finally {
        isLoading.value = false;
    }
};

const loadMockTasks = async () => {
    // Mock delay to simulate API call
    await new Promise(resolve => setTimeout(resolve, 800));
    
    // Generate mock data based on stage ID for variety
    const mockTasks: TaskTableRow[] = generateMockTasks(stageId.value);
    
    tasks.value = mockTasks;
    totalItems.value = mockTasks.length;
    
    // Set stage name and description based on common stage types
    const stageNames = {
        1: 'Initial Upload',
        2: 'Annotation',
        3: 'Review',
        4: 'Quality Control',
        5: 'Final Export'
    };
    
    const stageDescriptions = {
        1: 'Assets are uploaded and prepared for annotation',
        2: 'Primary annotation tasks for uploaded assets',
        3: 'Annotation review and validation tasks',
        4: 'Quality control and final validation',
        5: 'Final processing and export preparation'
    };
    
    stageName.value = stageNames[stageId.value as keyof typeof stageNames] || `Stage ${stageId.value}`;
    stageDescription.value = stageDescriptions[stageId.value as keyof typeof stageDescriptions] || 'Manage tasks for this workflow stage';
};

const generateMockTasks = (stageId: number): TaskTableRow[] => {
    const baseAssets = [
        'image_001.jpg', 'image_002.jpg', 'image_003.jpg', 'image_004.jpg', 'image_005.jpg',
        'document_001.pdf', 'scan_001.png', 'photo_001.jpg', 'diagram_001.svg', 'chart_001.png'
    ];
    
    const users = [
        'john.doe@example.com', 
        'jane.smith@example.com', 
        'bob.wilson@example.com', 
        'alice.johnson@example.com',
        null, null // Some unassigned tasks
    ];
    
    const statuses: TaskStatus[] = [TaskStatus.NOT_STARTED, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED];
    
    return baseAssets.slice(0, Math.min(6, baseAssets.length)).map((asset, index) => {
        const taskId = stageId * 100 + index + 1;
        const status = statuses[index % statuses.length];
        const assignedTo = users[index % users.length];
        
        const createdDate = new Date('2025-07-06');
        createdDate.setHours(createdDate.getHours() - (index * 2));
        
        const task: TaskTableRow = {
            id: taskId,
            assetId: taskId + 1000, // Mock assetId
            assetName: asset,
            priority: Math.floor(Math.random() * 3) + 1, // Random priority 1-3
            status,
            assignedTo: assignedTo || undefined,
            createdAt: createdDate.toISOString(),
            stage: stageName.value
        };
        
        // Add due dates for some tasks
        if (index % 2 === 0) {
            const dueDate = new Date('2025-07-06');
            dueDate.setDate(dueDate.getDate() + (index + 1));
            task.dueDate = dueDate.toISOString().split('T')[0];
        }
        
        // Add completion dates for completed tasks
        if (status === 'COMPLETED') {
            const completedDate = new Date('2025-07-06');
            completedDate.setHours(completedDate.getHours() - index);
            task.completedAt = completedDate.toISOString();
        }
        
        return task;
    });
};

const handleTableAction = (actionKey: string) => {
    logger.debug('Table action triggered', { actionKey });
    switch (actionKey) {
        case 'bulk-assign':
            // TODO: Show bulk assignment dialog
            break;
        case 'import-assets':
            // Navigate to data sources page for asset import
            router.push({
                name: 'ProjectDataSources',
                params: {
                    projectId: projectId.value
                }
            });
            break;
    }
};

const handleRowAction = (actionKey: string, row: TaskTableRow, index: number) => {
    logger.debug('Row action triggered', { actionKey, taskId: row.id, index });
    switch (actionKey) {
        case 'open':
            handleTaskClick(row, index);
            break;
        case 'edit':
            // TODO: Show edit task dialog
            break;
        case 'assign':
            // TODO: Show assignment dialog
            break;
        case 'delete':
            // TODO: Show delete confirmation
            break;
    }
};

const handleTaskClick = (task: TaskTableRow, _index: number) => {
    logger.info('Navigating to annotation workspace', { taskId: task.id, assetId: task.assetId, assetName: task.assetName });
    
    // Navigate to annotation workspace using projectId and the task's assetId
    // The workspace will load the asset and any existing annotations
    router.push({
        name: 'AnnotationWorkspace',
        params: {
            projectId: projectId.value.toString(),
            assetId: task.assetId.toString()
        },
        query: {
            taskId: task.id.toString() // Pass taskId as query parameter for context
        }
    });
};

const handleRefresh = () => {
    loadTasks();
};

const handlePageChange = (page: number) => {
    currentPage.value = page;
    loadTasks();
};

const handleSort = (key: string, direction: 'asc' | 'desc') => {
    sortKey.value = key;
    sortDirection.value = direction;
    loadTasks();
};

const goBack = () => {
    router.push({
        name: 'WorkflowPipeline',
        params: {
            projectId: projectId.value,
            workflowId: workflowId.value
        }
    });
};

const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString();
};

const getDueDateClass = (dueDate: string): string => {
    const due = new Date(dueDate);
    const now = new Date();
    const diffDays = Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays < 0) return 'overdue';
    if (diffDays <= 1) return 'urgent';
    if (diffDays <= 3) return 'soon';
    return 'normal';
};

const getPriorityClass = (priority: number): string => {
    if (priority >= 3) return 'priority-high';
    if (priority === 2) return 'priority-medium';
    return 'priority-low';
};

const getPriorityIcon = (priority: number) => {
    if (priority >= 3) return faExclamationTriangle;
    if (priority === 2) return faArrowUp;
    return faMinus;
};

const getPriorityLabel = (priority: number): string => {
    if (priority >= 3) return 'High';
    if (priority === 2) return 'Medium';
    return 'Low';
};

const getEmptyMessage = (): string => {
    if (!hasAvailableAssets.value) {
        return 'No assets available for task creation';
    }
    return 'No tasks found';
};

const getEmptyDescription = (): string => {
    if (!hasAvailableAssets.value) {
        return 'There are no assets imported in this project yet. Assets need to be imported into data sources before tasks can be automatically created. Go to the Data Sources page to import assets.';
    }
    return 'Tasks will be automatically created when assets are added to this workflow stage.';
};

onMounted(() => {
    loadTasks();
});
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.stage-tasks-view {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
}

.view-header {
    flex-shrink: 0;
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding: vars.$padding-large;
    background-color: vars.$theme-surface;
    border-bottom: 1px solid vars.$theme-border;
    gap: vars.$gap-large;
}

.header-left {
    display: flex;
    align-items: flex-start;
    gap: vars.$gap-medium;
    flex-grow: 1;
}

.back-button {
    flex-shrink: 0;
    margin-top: 2px; // Align with title
}

.stage-info {
    flex-grow: 1;
    min-width: 0;
    
    h2 {
        margin: 0 0 vars.$margin-xsmall;
        color: vars.$theme-text;
        font-size: vars.$font-size-xlarge;
        line-height: 1.2;
    }
    
    .stage-description {
        margin: 0;
        color: vars.$theme-text-light;
        font-size: vars.$font-size-small;
        line-height: 1.4;
    }
}

.view-actions {
    flex-shrink: 0;
    display: flex;
    gap: vars.$gap-small;
}

.tasks-container {
    flex-grow: 1;
    padding: vars.$padding-large;
    overflow: hidden;
}

// Custom cell styling
.assigned-user {
    display: flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    
    .user-icon {
        color: vars.$color-success;
    }
}

.unassigned {
    display: flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    color: vars.$theme-text-light;
    font-style: italic;
    
    .unassigned-icon {
        color: vars.$color-warning;
    }
}

.due-date {
    display: flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    
    &.normal {
        color: vars.$theme-text;
    }
    
    &.soon {
        color: vars.$color-warning;
        font-weight: vars.$font-weight-medium;
    }
    
    &.urgent {
        color: vars.$color-warning;
        font-weight: vars.$font-weight-large;
    }
    
    &.overdue {
        color: vars.$color-error;
        font-weight: vars.$font-weight-large;
    }
}

.no-due-date {
    color: vars.$theme-text-light;
    font-style: italic;
}

.priority-cell {
    display: flex;
    align-items: center;
    gap: vars.$gap-xsmall;
    font-weight: vars.$font-weight-medium;
    
    &.priority-high {
        color: vars.$color-error;
    }
    
    &.priority-medium {
        color: vars.$color-warning;
    }
    
    &.priority-low {
        color: vars.$theme-text-light;
    }
}

@media (max-width: 768px) {
    .view-header {
        flex-direction: column;
        align-items: stretch;
        gap: vars.$gap-medium;
    }
    
    .header-left {
        flex-direction: column;
        align-items: stretch;
        gap: vars.$gap-small;
    }
    
    .back-button {
        align-self: flex-start;
        margin-top: 0;
    }
    
    .view-actions {
        justify-content: center;
    }
    
    .tasks-container {
        padding: vars.$padding-medium;
    }
}
</style>
