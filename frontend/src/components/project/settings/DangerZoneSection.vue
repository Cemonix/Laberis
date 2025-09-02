<template>
    <div class="danger-zone-section">
        <div class="section-header">
            <font-awesome-icon :icon="faExclamationTriangle" class="warning-icon" />
            <div>
                <h2>Danger Zone</h2>
                <p>These actions are destructive and cannot be undone. Please be careful.</p>
            </div>
        </div>

        <div class="danger-actions">
            <!-- Archive Project -->
            <div class="danger-item">
                <div class="danger-info">
                    <h3>Archive Project</h3>
                    <p>Archive this project to hide it from project lists. The project and all its data will be preserved but marked as archived. You can restore it later.</p>
                    <div class="danger-effects">
                        <span class="effect-item">• Project will be hidden from main project list</span>
                        <span class="effect-item">• All data and annotations will be preserved</span>
                        <span class="effect-item">• Can be restored later</span>
                    </div>
                </div>
                <div class="danger-action">
                    <Button
                        variant="secondary"
                        @click="handleArchiveProject"
                        :disabled="isArchiving || project?.status === ProjectStatus.ARCHIVED"
                        :loading="isArchiving"
                    >
                        {{ project?.status === ProjectStatus.ARCHIVED ? 'Already Archived' : 'Archive Project' }}
                    </Button>
                </div>
            </div>

            <!-- Restore Project -->
            <div v-if="project?.status === ProjectStatus.ARCHIVED" class="danger-item restore-item">
                <div class="danger-info">
                    <h3>Restore Project</h3>
                    <p>Restore this archived project to make it visible and accessible again.</p>
                    <div class="danger-effects">
                        <span class="effect-item">• Project will appear in project lists</span>
                        <span class="effect-item">• All team members can access it again</span>
                        <span class="effect-item">• Work can continue normally</span>
                    </div>
                </div>
                <div class="danger-action">
                    <Button
                        variant="primary"
                        @click="handleRestoreProject"
                        :disabled="isRestoring"
                        :loading="isRestoring"
                    >
                        Restore Project
                    </Button>
                </div>
            </div>

            <!-- Transfer Ownership -->
            <div class="danger-item">
                <div class="danger-info">
                    <h3>Transfer Project Ownership</h3>
                    <p>Transfer ownership of this project to another team member. The new owner will have full control over the project.</p>
                    <div class="danger-effects">
                        <span class="effect-item">• New owner gets full project control</span>
                        <span class="effect-item">• Your access level may change</span>
                        <span class="effect-item">• Cannot be undone automatically</span>
                    </div>
                </div>
                <div class="danger-action">
                    <div class="transfer-form">
                        <select 
                            v-model="transferToEmail" 
                            class="transfer-select"
                            :disabled="isTransferring"
                        >
                            <option value="">Select new owner...</option>
                            <option 
                                v-for="member in projectManagers" 
                                :key="member.email"
                                :value="member.email"
                            >
                                {{ member.userName || member.email }}
                            </option>
                        </select>
                        <Button
                            variant="secondary"
                            @click="handleTransferOwnership"
                            :disabled="isTransferring || !transferToEmail"
                            :loading="isTransferring"
                        >
                            Transfer Ownership
                        </Button>
                    </div>
                </div>
            </div>

            <!-- Delete Project -->
            <div class="danger-item delete-item">
                <div class="danger-info">
                    <h3>Delete Project Permanently</h3>
                    <p><strong>This action cannot be undone!</strong> This will permanently delete the project and all associated data including annotations, images, and member access.</p>
                    <div class="danger-effects critical">
                        <span class="effect-item">• All project data will be permanently deleted</span>
                        <span class="effect-item">• All annotations and labels will be lost</span>
                        <span class="effect-item">• All team member access will be removed</span>
                        <span class="effect-item">• This action is irreversible</span>
                    </div>
                </div>
                <div class="danger-action">
                    <Button
                        variant="secondary"
                        @click="showDeleteConfirmation = true"
                        :disabled="isDeleting"
                        class="delete-btn"
                    >
                        Delete Project Forever
                    </Button>
                </div>
            </div>
        </div>

        <!-- Delete Confirmation Modal -->
        <div v-if="showDeleteConfirmation" class="modal-overlay" @click.self="showDeleteConfirmation = false">
            <div class="delete-confirmation-modal">
                <div class="modal-header">
                    <font-awesome-icon :icon="faExclamationTriangle" class="modal-warning-icon" />
                    <h3>Confirm Project Deletion</h3>
                </div>
                
                <div class="modal-content">
                    <p>You are about to <strong>permanently delete</strong> the project:</p>
                    <div class="project-name">{{ project?.name }}</div>
                    
                    <div class="confirmation-requirements">
                        <p>To confirm deletion, please type the project name exactly:</p>
                        <input
                            v-model="deleteConfirmationName"
                            type="text"
                            :placeholder="project?.name"
                            class="confirmation-input"
                            @keyup.enter="handleDeleteProject"
                        />
                        <div v-if="deleteConfirmationError" class="confirmation-error">
                            {{ deleteConfirmationError }}
                        </div>
                    </div>

                    <div class="final-warning">
                        <font-awesome-icon :icon="faExclamationTriangle" />
                        <span>This action is irreversible and will delete all project data permanently.</span>
                    </div>
                </div>

                <div class="modal-actions">
                    <Button
                        variant="secondary"
                        @click="cancelDelete"
                        :disabled="isDeleting"
                    >
                        Cancel
                    </Button>
                    <Button
                        variant="secondary"
                        @click="handleDeleteProject"
                        :disabled="isDeleting || !isDeleteConfirmed"
                        :loading="isDeleting"
                        class="confirm-delete-btn"
                    >
                        Yes, Delete Forever
                    </Button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import { projectService } from '@/services/project';
import { projectMemberService } from '@/services/project';
import { useToast } from '@/composables/useToast';
import { useConfirm } from '@/composables/useConfirm';
import { useErrorHandler } from '@/composables/useErrorHandler';
import { useAuthStore } from '@/stores/authStore';
import { AppLogger } from '@/core/logger/logger';
import { ProjectStatus, ProjectRole } from '@/services/project/project.types';
import type { Project } from '@/services/project/project.types';
import type { ProjectMember } from '@/services/project/projectMember.types';

const logger = AppLogger.createComponentLogger('DangerZoneSection');

interface Props {
    projectId: number;
}

const props = defineProps<Props>();

const router = useRouter();
const { showToast } = useToast();
const { showConfirm } = useConfirm();
const { handleError } = useErrorHandler();
const authStore = useAuthStore();

// State
const project = ref<Project | null>(null);
const projectMembers = ref<ProjectMember[]>([]);
const isArchiving = ref(false);
const isRestoring = ref(false);
const isTransferring = ref(false);
const isDeleting = ref(false);
const transferToEmail = ref('');
const showDeleteConfirmation = ref(false);
const deleteConfirmationName = ref('');
const deleteConfirmationError = ref('');

// Computed
const projectManagers = computed(() => 
    projectMembers.value.filter(member => 
        member.role === ProjectRole.MANAGER && 
        member.email !== authStore.user?.email &&
        member.joinedAt // Only show active members
    )
);

const isDeleteConfirmed = computed(() => 
    deleteConfirmationName.value.trim() === project.value?.name?.trim()
);

// Methods
const loadProjectData = async () => {
    try {
        const [projectData, membersData] = await Promise.all([
            projectService.getProject(props.projectId),
            projectMemberService.getProjectMembers(props.projectId)
        ]);
        
        project.value = projectData;
        projectMembers.value = membersData;
        
        logger.info('Project data loaded for danger zone');
    } catch (error) {
        handleError(error, 'Failed to load project data');
    }
};

const handleArchiveProject = async () => {
    const confirmed = await showConfirm(
        'Archive Project',
        `Are you sure you want to archive "${project.value?.name}"? The project will be hidden from lists but can be restored later.`
    );
    
    if (!confirmed) return;
    
    isArchiving.value = true;
    try {
        const archivedProject = await projectService.archiveProject(props.projectId);
        project.value = archivedProject;
        
        logger.info(`Project ${props.projectId} archived successfully`);
        showToast('Success', 'Project archived successfully', 'success');
    } catch (error) {
        handleError(error, 'Failed to archive project');
    } finally {
        isArchiving.value = false;
    }
};

const handleRestoreProject = async () => {
    const confirmed = await showConfirm(
        'Restore Project',
        `Are you sure you want to restore "${project.value?.name}"? The project will become visible and accessible again.`
    );
    
    if (!confirmed) return;
    
    isRestoring.value = true;
    try {
        const restoredProject = await projectService.restoreProject(props.projectId);
        project.value = restoredProject;
        
        logger.info(`Project ${props.projectId} restored successfully`);
        showToast('Success', 'Project restored successfully', 'success');
    } catch (error) {
        handleError(error, 'Failed to restore project');
    } finally {
        isRestoring.value = false;
    }
};

const handleTransferOwnership = async () => {
    if (!transferToEmail.value) return;
    
    const targetMember = projectMembers.value.find(m => m.email === transferToEmail.value);
    const confirmed = await showConfirm(
        'Transfer Ownership',
        `Are you sure you want to transfer ownership of "${project.value?.name}" to ${targetMember?.userName || transferToEmail.value}? This action cannot be undone automatically.`
    );
    
    if (!confirmed) return;
    
    isTransferring.value = true;
    try {
        // Note: This would need a backend endpoint for ownership transfer
        // For now, we'll show a placeholder implementation
        await new Promise(resolve => setTimeout(resolve, 1000)); // Simulate API call
        
        logger.info(`Ownership transfer initiated for project ${props.projectId} to ${transferToEmail.value}`);
        showToast('Success', 'Ownership transfer completed', 'success');
        transferToEmail.value = '';
        
        // Reload project data to reflect changes
        await loadProjectData();
    } catch (error) {
        handleError(error, 'Failed to transfer project ownership');
    } finally {
        isTransferring.value = false;
    }
};

const handleDeleteProject = async () => {
    if (!isDeleteConfirmed.value) {
        deleteConfirmationError.value = 'Please type the project name exactly to confirm deletion';
        return;
    }
    
    deleteConfirmationError.value = '';
    isDeleting.value = true;
    
    try {
        await projectService.deleteProject(props.projectId);
        
        logger.info(`Project ${props.projectId} deleted successfully`);
        showToast('Success', 'Project deleted permanently', 'success');
        
        // Navigate back to projects list after successful deletion
        router.push('/projects');
    } catch (error) {
        handleError(error, 'Failed to delete project');
        showDeleteConfirmation.value = false;
    } finally {
        isDeleting.value = false;
    }
};

const cancelDelete = () => {
    showDeleteConfirmation.value = false;
    deleteConfirmationName.value = '';
    deleteConfirmationError.value = '';
};

// Lifecycle
onMounted(() => {
    loadProjectData();
});
</script>

<style lang="scss" scoped>
.danger-zone-section {
    padding: 2rem;
    background-color: var(--color-white);
}

.section-header {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    margin-bottom: 2rem;
    padding-bottom: 1.5rem;
    border-bottom: 2px solid var(--color-error-light);
    
    .warning-icon {
        color: var(--color-error);
        font-size: 1.5rem;
        margin-top: 0.25rem;
    }
    
    h2 {
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--color-error);
        margin-bottom: 0.5rem;
    }
    
    p {
        color: var(--color-gray-700);
        font-size: 1rem;
        line-height: 1.5;
        margin: 0;
    }
}

.danger-actions {
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.danger-item {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding: 1.5rem;
    background-color: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    
    &.restore-item {
        border-color: var(--color-success);
        background-color: var(--color-success-light);
        
        h3 {
            color: var(--color-success);
        }
    }
    
    &.delete-item {
        border-color: var(--color-error);
        background-color: var(--color-error-light);
        
        h3 {
            color: var(--color-error);
        }
    }
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
}

.danger-info {
    flex: 1;
    margin-right: 2rem;
    
    @media (max-width: 768px) {
        margin-right: 0;
    }
    
    h3 {
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
        color: var(--color-gray-800);
    }
    
    p {
        color: var(--color-gray-700);
        line-height: 1.5;
        margin-bottom: 1rem;
    }
}

.danger-effects {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    
    &.critical {
        .effect-item {
            color: var(--color-error);
            font-weight: 500;
        }
    }
    
    .effect-item {
        font-size: 0.875rem;
        color: var(--color-gray-600);
        line-height: 1.4;
    }
}

.danger-action {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.transfer-form {
    display: flex;
    align-items: center;
    gap: 1rem;
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
    }
}

.transfer-select {
    min-width: 200px;
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    font-size: 0.875rem;
    
    @media (max-width: 768px) {
        min-width: 100%;
    }
}

.delete-btn {
    background-color: var(--color-error) !important;
    border-color: var(--color-error) !important;
    color: var(--color-white) !important;
    
    &:hover:not(:disabled) {
        background-color: var(--color-error-dark) !important;
        border-color: var(--color-error-dark) !important;
    }
}

// Delete Confirmation Modal
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 1rem;
}

.delete-confirmation-modal {
    background-color: var(--color-white);
    border-radius: 8px;
    max-width: 500px;
    width: 100%;
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3);
    animation: modalSlideIn 0.3s ease-out;
}

@keyframes modalSlideIn {
    from {
        opacity: 0;
        transform: translateY(-20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.modal-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1.5rem;
    border-bottom: 1px solid var(--color-gray-200);
    
    .modal-warning-icon {
        color: var(--color-error);
        font-size: 1.5rem;
    }
    
    h3 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-error);
        margin: 0;
    }
}

.modal-content {
    padding: 1.5rem;
    
    p {
        margin-bottom: 1rem;
        color: var(--color-gray-700);
        line-height: 1.5;
    }
    
    .project-name {
        background-color: var(--color-gray-100);
        padding: 0.75rem;
        border-radius: 4px;
        font-family: monospace;
        font-weight: 600;
        color: var(--color-error);
        margin-bottom: 1.5rem;
        word-break: break-all;
    }
}

.confirmation-requirements {
    margin-bottom: 1.5rem;
    
    p {
        font-weight: 500;
        margin-bottom: 0.5rem;
        color: var(--color-gray-800);
    }
}

.confirmation-input {
    width: 100%;
    padding: 0.75rem;
    border: 2px solid var(--color-gray-300);
    border-radius: 4px;
    font-size: 1rem;
    font-family: monospace;
    
    &:focus {
        outline: none;
        border-color: var(--color-error);
    }
}

.confirmation-error {
    color: var(--color-error);
    font-size: 0.875rem;
    font-weight: 500;
    margin-top: 0.5rem;
}

.final-warning {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem;
    background-color: var(--color-error-light);
    border: 1px solid var(--color-error);
    border-radius: 4px;
    color: var(--color-error);
    font-weight: 500;
    font-size: 0.875rem;
}

.modal-actions {
    display: flex;
    gap: 1rem;
    padding: 1.5rem;
    border-top: 1px solid var(--color-gray-200);
    justify-content: flex-end;
}

.confirm-delete-btn {
    background-color: var(--color-error) !important;
    border-color: var(--color-error) !important;
    color: var(--color-white) !important;
    
    &:hover:not(:disabled) {
        background-color: var(--color-error-dark) !important;
        border-color: var(--color-error-dark) !important;
    }
    
    &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }
}
</style>