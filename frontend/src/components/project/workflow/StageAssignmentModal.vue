<template>
    <div v-if="isVisible" class="modal-overlay" @click="handleOverlayClick">
        <div class="modal-content" @click.stop>
            <div class="modal-header">
                <h3>Team Members - {{ stage.name }}</h3>
                <button class="close-button" @click="handleClose" type="button">
                    <font-awesome-icon :icon="faTimes" />
                </button>
            </div>
            
            <div class="modal-body">
                <div class="assignment-modal">
                <div v-if="isLoading" class="loading-state">
                    <p>Loading assignments...</p>
                </div>
                
                <div v-else-if="error" class="error-state">
                    <p class="error-message">{{ error }}</p>
                    <Button variant="secondary" @click="loadAssignments">Retry</Button>
                </div>
                
                <div v-else class="assignments-content">
                    <div v-if="props.stage.stageType" class="role-info">
                        <div class="info-message">
                            <font-awesome-icon :icon="faInfoCircle" />
                            <span>Only {{ stageRoleDescription.toLowerCase() }} can be assigned to {{ props.stage.stageType.toLowerCase() }} stages.</span>
                        </div>
                    </div>
                    
                    <div class="section">
                        <h4>Assigned {{ stageRoleDescription }}</h4>
                        <div v-if="assignedMembers.length === 0" class="empty-state">
                            <p>No {{ stageRoleDescription.toLowerCase() }} assigned to this stage.</p>
                        </div>
                        <div v-else class="member-list">
                            <div
                                v-for="member in assignedMembers"
                                :key="member.id"
                                class="member-item"
                            >
                                <div class="member-info">
                                    <div class="member-name">{{ member.userName || member.email.split('@')[0] }}</div>
                                    <div class="member-role">{{ formatRole(member.role) }}</div>
                                    <div class="member-email">{{ member.email }}</div>
                                </div>
                                <Button
                                    variant="danger"
                                    size="small"
                                    @click="handleRemoveMember(member.id)"
                                    :disabled="isUpdating"
                                >
                                    <font-awesome-icon :icon="faTrash" />
                                    Remove
                                </Button>
                            </div>
                        </div>
                    </div>
                    
                    <div class="section">
                        <h4>Available {{ stageRoleDescription }}</h4>
                        <div v-if="availableMembers.length === 0" class="empty-state">
                            <p v-if="roleCompatibleMembers.length === 0">
                                No {{ stageRoleDescription.toLowerCase() }} available in this project. 
                                Only {{ stageRoleDescription.toLowerCase() }} can be assigned to {{ props.stage.stageType?.toLowerCase() || 'this' }} stages.
                            </p>
                            <p v-else>
                                All {{ stageRoleDescription.toLowerCase() }} are already assigned to this stage.
                            </p>
                        </div>
                        <div v-else class="member-list">
                            <div
                                v-for="member in availableMembers"
                                :key="member.id"
                                class="member-item"
                            >
                                <div class="member-info">
                                    <div class="member-name">{{ member.userName || member.email.split('@')[0] }}</div>
                                    <div class="member-role">{{ formatRole(member.role) }}</div>
                                    <div class="member-email">{{ member.email }}</div>
                                </div>
                                <Button
                                    variant="primary"
                                    size="small"
                                    @click="handleAddMember(member.id)"
                                    :disabled="isUpdating"
                                >
                                    <font-awesome-icon :icon="faPlus" />
                                    Assign
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class="modal-actions">
                <Button variant="secondary" @click="handleClose">
                    Close
                </Button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue';
import Button from '@/components/common/Button.vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faPlus, faTrash, faTimes, faInfoCircle} from '@fortawesome/free-solid-svg-icons';
import type {WorkflowStagePipeline, WorkflowStageAssignment} from '@/types/workflow';
import type {ProjectMember} from '@/types/projectMember';
import {ProjectRole} from '@/types/project';
import {projectMemberService, workflowStageAssignmentService} from '@/services/api/projects';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {AppLogger} from '@/utils/logger';
import {filterMembersByStageType, getRoleDescriptionForStageType} from '@/core/validation/workflowRoleValidation';

interface Props {
    stage: WorkflowStagePipeline;
    projectId: number;
    workflowId: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
    close: [];
    updated: [];
}>();

const logger = AppLogger.createComponentLogger('StageAssignmentModal');
const {handleError} = useErrorHandler();

// Reactive state
const isVisible = ref(true);
const isLoading = ref(true);
const isUpdating = ref(false);
const error = ref<string | null>(null);
const allMembers = ref<ProjectMember[]>([]);
const assignedMemberIds = ref<Set<number>>(new Set());
const assignmentMap = ref<Map<number, number>>(new Map()); // memberId -> assignmentId

// Computed properties
const roleCompatibleMembers = computed(() => {
    if (!props.stage.stageType) {
        // If no stage type, allow all members (fallback for safety)
        return allMembers.value;
    }
    return filterMembersByStageType(allMembers.value, props.stage.stageType);
});

const assignedMembers = computed(() => 
    roleCompatibleMembers.value.filter(member => assignedMemberIds.value.has(member.id))
);

const availableMembers = computed(() => 
    roleCompatibleMembers.value.filter(member => !assignedMemberIds.value.has(member.id))
);

const stageRoleDescription = computed(() => {
    if (!props.stage.stageType) return 'team members';
    return getRoleDescriptionForStageType(props.stage.stageType);
});

// Utility functions
const formatRole = (role: ProjectRole): string => {
    switch (role) {
        case ProjectRole.MANAGER:
            return 'Manager';
        case ProjectRole.ANNOTATOR:
            return 'Annotator';
        case ProjectRole.REVIEWER:
            return 'Reviewer';
        case ProjectRole.VIEWER:
            return 'Viewer';
        default:
            return role;
    }
};

// Methods
const handleClose = () => {
    isVisible.value = false;
    emit('close');
};

const handleOverlayClick = () => {
    handleClose();
};

const loadAssignments = async () => {
    isLoading.value = true;
    error.value = null;
    
    try {
        logger.info(`Loading assignments for stage ${props.stage.id}`);
        
        // Get all project members and stage assignments in parallel
        const [members, assignments] = await Promise.all([
            projectMemberService.getProjectMembers(props.projectId),
            workflowStageAssignmentService.getStageAssignments(props.projectId, props.workflowId, props.stage.id)
        ]);
        
        allMembers.value = members;
        
        // Build assignment maps
        const assignedIds = new Set<number>();
        const memberToAssignmentMap = new Map<number, number>();
        
        // Create a lookup map for members by email
        const membersByEmail = new Map<string, ProjectMember>();
        allMembers.value.forEach(member => {
            membersByEmail.set(member.email, member);
        });
        
        // Process assignments
        assignments.forEach((assignment: WorkflowStageAssignment) => {
            const member = membersByEmail.get(assignment.projectMember.email);
            if (member) {
                assignedIds.add(member.id);
                memberToAssignmentMap.set(member.id, assignment.id);
            }
        });
        
        assignedMemberIds.value = assignedIds;
        assignmentMap.value = memberToAssignmentMap;
        
        logger.info(`Loaded ${allMembers.value.length} project members, ${assignedIds.size} already assigned`);
        
    } catch (err) {
        logger.error('Error loading stage assignments', err);
        error.value = 'Failed to load stage assignments';
        handleError(err, 'Loading stage assignments');
    } finally {
        isLoading.value = false;
    }
};

const handleAddMember = async (memberId: number) => {
    isUpdating.value = true;
    
    try {
        logger.info(`Adding member ${memberId} to stage ${props.stage.id}`);
        
        const assignment = await workflowStageAssignmentService.createStageAssignment(
            props.projectId,
            props.workflowId,
            props.stage.id,
            memberId
        );
        
        // Update local state
        assignedMemberIds.value.add(memberId);
        assignmentMap.value.set(memberId, assignment.id);
        
        logger.info(`Successfully added member ${memberId} to stage`);
        emit('updated');
        
    } catch (err) {
        logger.error('Error adding member to stage', err);
        handleError(err, 'Adding member to stage');
    } finally {
        isUpdating.value = false;
    }
};

const handleRemoveMember = async (memberId: number) => {
    isUpdating.value = true;
    
    try {
        logger.info(`Removing member ${memberId} from stage ${props.stage.id}`);
        
        const assignmentId = assignmentMap.value.get(memberId);
        if (!assignmentId) {
            throw new Error(`No assignment found for member ${memberId}`);
        }
        
        await workflowStageAssignmentService.deleteStageAssignment(
            props.projectId,
            props.workflowId,
            props.stage.id,
            assignmentId
        );
        
        // Update local state
        assignedMemberIds.value.delete(memberId);
        assignmentMap.value.delete(memberId);
        
        logger.info(`Successfully removed member ${memberId} from stage`);
        emit('updated');
        
    } catch (err) {
        logger.error('Error removing member from stage', err);
        handleError(err, 'Removing member from stage');
    } finally {
        isUpdating.value = false;
    }
};

onMounted(() => {
    loadAssignments();
});
</script>

<style scoped>
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1rem;
}

.modal-content {
    background: var(--color-white);
    border-radius: 0.5rem;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1), 0 1px 3px rgba(0, 0, 0, 0.08);
    width: 100%;
    max-width: 600px;
    max-height: 80vh;
    overflow: hidden;
    display: flex;
    flex-direction: column;
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    border-bottom: 1px solid var(--color-gray-400);
    background: var(--color-gray-50);
}

.modal-header h3 {
    margin: 0;
    color: var(--color-gray-800);
    font-size: 1.25rem;
    font-weight: 600;
}

.close-button {
    background: none;
    border: none;
    color: var(--color-gray-600);
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0.25rem;
    line-height: 1;
    transition: color 0.2s ease;
}

.close-button:hover {
    color: var(--color-gray-800);
}

.modal-body {
    flex: 1;
    overflow: auto;
    padding: 1.5rem;
}

.assignment-modal {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.role-info {
    margin-bottom: 0.5rem;
}

.info-message {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem;
    background: var(--color-info-light);
    border: 1px solid var(--color-info);
    border-radius: 6px;
    font-size: 0.875rem;
    color: var(--color-info-dark);
}

.info-message svg {
    color: var(--color-info);
    font-size: 1rem;
    flex-shrink: 0;
}

.loading-state,
.error-state {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 1rem;
    padding: 2rem;
    text-align: center;
}

.error-message {
    color: var(--color-error);
    margin: 0;
}

.assignments-content {
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.section h4 {
    margin: 0 0 1rem;
    color: var(--color-gray-800);
    font-size: 1.125rem;
    font-weight: 600;
}

.member-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.member-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    background: var(--color-gray-50);
    border: 1px solid var(--color-gray-400);
    border-radius: 6px;
    transition: all 0.2s ease;
}

.member-item:hover {
    background: var(--color-gray-100);
    border-color: var(--color-gray-500);
}

.member-info {
    flex: 1;
}

.member-name {
    font-weight: 600;
    color: var(--color-gray-800);
    font-size: 0.95rem;
}

.member-role {
    font-size: 0.875rem;
    color: var(--color-primary);
    font-weight: 500;
    margin-top: 0.25rem;
}

.member-email {
    font-size: 0.75rem;
    color: var(--color-gray-600);
    margin-top: 0.25rem;
}

.empty-state {
    padding: 1.5rem;
    text-align: center;
    color: var(--color-gray-600);
    background: var(--color-gray-50);
    border-radius: 6px;
    border: 1px dashed var(--color-gray-400);
}

.empty-state p {
    margin: 0;
}

.modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    padding-top: 1rem;
    border-top: 1px solid var(--color-gray-400);
}

@media (max-width: 768px) {
    .member-item {
        flex-direction: column;
        align-items: stretch;
        gap: 0.75rem;
    }
    
    .modal-actions {
        flex-direction: column;
    }
}
</style>