<template>
    <Card class="members-section">
        <template #header>
            <h2>Manage Members & Invitations</h2>
        </template>

        <!-- Invitation Form - Only available to users who can invite members -->
        <div v-permission="{ permission: PERMISSIONS.PROJECT_MEMBER.INVITE }" class="invite-form-section">
            <h3>Invite New Member</h3>
            <Form @submit="handleSendInvite" class="invite-form">
                <div class="form-group">
                    <label for="invite-email">Email Address</label>
                    <input
                        id="invite-email"
                        v-model="inviteForm.email"
                        type="email"
                        placeholder="Enter email address"
                        required
                        :disabled="isInviting"
                    />
                    <div v-if="inviteErrors.email" class="field-error">{{ inviteErrors.email }}</div>
                </div>

                <div class="form-group">
                    <label for="invite-role">Role</label>
                    <select
                        id="invite-role"
                        v-model="inviteForm.role"
                        required
                        :disabled="isInviting"
                    >
                        <option value="">Select a role</option>
                        <option 
                            v-for="role in availableRoles" 
                            :key="role.value" 
                            :value="role.value"
                        >
                            {{ role.label }}
                        </option>
                    </select>
                    <div class="field-help">{{ getRoleDescription(inviteForm.role) }}</div>
                    <div v-if="inviteErrors.role" class="field-error">{{ inviteErrors.role }}</div>
                </div>

                <div class="form-actions">
                    <Button
                        type="submit"
                        variant="primary"
                        :disabled="isInviting || !isInviteFormValid"
                    >
                        {{ isInviting ? 'Sending...' : 'Send Invitation' }}
                    </Button>
                </div>
            </Form>
        </div>

        <!-- Current Members Section -->
        <div class="members-list-section">
            <h3>Current Members ({{ currentMembers.length }})</h3>
            <div v-if="isLoadingMembers" class="loading-state">
                Loading members...
            </div>
            <div v-else-if="currentMembers.length === 0" class="empty-state">
                No members found.
            </div>
            <div v-else class="members-list">
                <div 
                    v-for="member in currentMembers" 
                    :key="member.id"
                    class="member-item"
                >
                    <div class="member-info">
                        <div class="member-details">
                            <span class="member-name">{{ member.userName || member.email }}</span>
                            <span class="member-email" v-if="member.userName && member.email">{{ member.email }}</span>
                        </div>
                        <div class="member-meta">
                            <span class="member-role" :class="`role-${member.role.toLowerCase()}`">
                                {{ getRoleLabel(member.role) }}
                            </span>
                            <span class="member-joined">
                                Joined {{ formatDate(member.joinedAt || member.createdAt) }}
                            </span>
                        </div>
                    </div>
                    <div class="member-actions">
                        <select
                            v-permission="{ permission: PERMISSIONS.PROJECT_MEMBER.UPDATE }"
                            v-model="member.role"
                            @change="handleUpdateRole(member)"
                            class="role-select"
                            :disabled="isUpdatingRole || member.email === currentUserEmail"
                        >
                            <option 
                                v-for="role in availableRoles" 
                                :key="role.value" 
                                :value="role.value"
                            >
                                {{ role.label }}
                            </option>
                        </select>
                        <Button
                            v-permission="{ permission: PERMISSIONS.PROJECT_MEMBER.REMOVE }"
                            variant="secondary"
                            size="small"
                            @click="handleRemoveMember(member)"
                            :disabled="isRemoving || member.email === currentUserEmail"
                            class="remove-btn"
                        >
                            Remove
                        </Button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Pending Invitations Section -->
        <div class="invitations-list-section">
            <h3>Pending Invitations ({{ pendingInvitations.length }})</h3>
            <div v-if="isLoadingInvitations" class="loading-state">
                Loading invitations...
            </div>
            <div v-else-if="pendingInvitations.length === 0" class="empty-state">
                No pending invitations.
            </div>
            <div v-else class="invitations-list">
                <div 
                    v-for="invitation in pendingInvitations" 
                    :key="invitation.id"
                    class="invitation-item"
                >
                    <div class="invitation-info">
                        <div class="invitation-details">
                            <span class="invitation-email">{{ invitation.email }}</span>
                            <span class="invitation-role" :class="`role-${invitation.role.toLowerCase()}`">
                                {{ getRoleLabel(invitation.role) }}
                            </span>
                        </div>
                        <div class="invitation-meta">
                            <span class="invitation-sent">
                                Invited {{ formatDate(invitation.invitedAt) }}
                            </span>
                        </div>
                    </div>
                    <div class="invitation-actions">
                        <Button
                            v-permission="{ permission: PERMISSIONS.PROJECT_MEMBER.REMOVE }"
                            variant="secondary"
                            size="small"
                            @click="handleRevokeInvitation(invitation)"
                            :disabled="isRevoking"
                            class="revoke-btn"
                        >
                            Revoke
                        </Button>
                    </div>
                </div>
            </div>
        </div>
    </Card>
</template>

<script setup lang="ts">
// TODO: REFACTOR THIS FILE

import {computed, onMounted, reactive, ref} from 'vue';
import Card from '@/components/common/Card.vue';
import Button from '@/components/common/Button.vue';
import Form from '@/components/common/Form.vue';
import {projectMemberService} from '@/services/project';
import {useToast} from '@/composables/useToast';
import {useConfirm} from '@/composables/useConfirm';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {useAuthStore} from '@/stores/authStore';
import {AppLogger} from '@/core/logger/logger';
import type {InviteMemberRequest, ProjectMember} from '@/services/project/projectMember.types';
import {ProjectRole} from '@/services/project/project.types';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const logger = AppLogger.createComponentLogger('MembersSection');

interface Props {
    projectId: number;
}

const props = defineProps<Props>();

const { showCreateSuccess, showDeleteSuccess } = useToast();
const { showConfirm } = useConfirm();
const { handleError } = useErrorHandler();
const authStore = useAuthStore();

// State
const currentMembers = ref<ProjectMember[]>([]);
const pendingInvitations = ref<ProjectMember[]>([]);
const isLoadingMembers = ref(false);
const isLoadingInvitations = ref(false);
const isInviting = ref(false);
const isRemoving = ref(false);
const isRevoking = ref(false);
const isUpdatingRole = ref(false);

// Form state
const inviteForm = reactive({
    email: '',
    role: '' as ProjectRole | ''
});

const inviteErrors = reactive({
    email: '',
    role: ''
});

// Available roles for selection
const availableRoles = [
    { value: ProjectRole.MANAGER, label: 'Manager' },
    { value: ProjectRole.ANNOTATOR, label: 'Annotator' },
    { value: ProjectRole.REVIEWER, label: 'Reviewer' },
    { value: ProjectRole.VIEWER, label: 'Viewer' }
];

// Computed
const currentUserEmail = computed(() => authStore.user?.email);

const isInviteFormValid = computed(() => {
    return inviteForm.email.trim() && 
        inviteForm.role && 
        !inviteErrors.email && 
        !inviteErrors.role;
});

// Methods
const loadMembers = async () => {
    isLoadingMembers.value = true;
    try {
        const members = await projectMemberService.getProjectMembers(props.projectId);
        
        // Separate current members from pending invitations
        currentMembers.value = members.filter(m => m.joinedAt);
        pendingInvitations.value = members.filter(m => !m.joinedAt);
        
        logger.info(`Loaded ${currentMembers.value.length} members and ${pendingInvitations.value.length} pending invitations`);
    } catch (error) {
        handleError(error, 'Failed to load project members');
    } finally {
        isLoadingMembers.value = false;
        isLoadingInvitations.value = false;
    }
};

const validateInviteForm = (): boolean => {
    inviteErrors.email = '';
    inviteErrors.role = '';
    
    if (!inviteForm.email.trim()) {
        inviteErrors.email = 'Email address is required';
        return false;
    }
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(inviteForm.email.trim())) {
        inviteErrors.email = 'Please enter a valid email address';
        return false;
    }
    
    if (!inviteForm.role) {
        inviteErrors.role = 'Please select a role';
        return false;
    }
    
    // Check if user is already invited or a member
    const email = inviteForm.email.trim().toLowerCase();
    const isAlreadyMember = currentMembers.value.some(m => 
        m.email?.toLowerCase() === email
    );
    const isAlreadyInvited = pendingInvitations.value.some(m => 
        m.email?.toLowerCase() === email
    );
    
    if (isAlreadyMember) {
        inviteErrors.email = 'This user is already a member of the project';
        return false;
    }
    
    if (isAlreadyInvited) {
        inviteErrors.email = 'This user has already been invited';
        return false;
    }
    
    return true;
};

const handleSendInvite = async () => {
    if (!validateInviteForm()) {
        return;
    }
    
    isInviting.value = true;
    try {
        const inviteData: InviteMemberRequest = {
            email: inviteForm.email.trim(),
            role: inviteForm.role as ProjectRole
        };
        
        await projectMemberService.inviteMember(props.projectId, inviteData);
        // TODO: Update the pendingInvitations state with the new invitation (Backend is not returning the full member object)
        // For example:
        // pendingInvitations.value.push(newInvitation);
        
        // Reset form
        inviteForm.email = '';
        inviteForm.role = '';
        
        logger.info(`Sent invitation to ${inviteData.email} with role ${inviteData.role}`);
        showCreateSuccess('Invitation', `Invitation sent to ${inviteData.email}`);
    } catch (error) {
        handleError(error, 'Members Section');
    } finally {
        isInviting.value = false;
    }
};

const handleUpdateRole = async (member: ProjectMember) => {
    const confirmed = await showConfirm(
        'Update Role', 
        `Are you sure you want to change ${member.userName || member.email}'s role to ${getRoleLabel(member.role)}?`
    );
    
    if (!confirmed) {
        // Revert the select value by reloading
        await loadMembers();
        return;
    }
    
    isUpdatingRole.value = true;
    try {
        await projectMemberService.updateMemberRole(props.projectId, member.email, member.role);
        logger.info(`Updated role for ${member.email} to ${member.role}`);
        showCreateSuccess('Role Updated', `${member.userName || member.email}'s role has been updated`);
    } catch (error) {
        handleError(error, 'Failed to update member role');
        // Reload to revert changes
        await loadMembers();
    } finally {
        isUpdatingRole.value = false;
    }
};

const handleRemoveMember = async (member: ProjectMember) => {
    const confirmed = await showConfirm(
        'Remove Member', 
        `Are you sure you want to remove ${member.userName || member.email} from this project? This action cannot be undone.`
    );
    
    if (!confirmed) return;
    
    isRemoving.value = true;
    try {
        await projectMemberService.removeMember(props.projectId, member.email);
        currentMembers.value = currentMembers.value.filter(m => m.id !== member.id);
        
        logger.info(`Removed member ${member.email} from project ${props.projectId}`);
        showDeleteSuccess('Member', member.userName || member.email || 'User');
    } catch (error) {
        handleError(error, 'Failed to remove member');
    } finally {
        isRemoving.value = false;
    }
};

const handleRevokeInvitation = async (invitation: ProjectMember) => {
    const confirmed = await showConfirm(
        'Revoke Invitation', 
        `Are you sure you want to revoke the invitation for ${invitation.email}?`
    );
    
    if (!confirmed) return;
    
    isRevoking.value = true;
    try {
        await projectMemberService.removeMember(props.projectId, invitation.email);
        pendingInvitations.value = pendingInvitations.value.filter(i => i.id !== invitation.id);
        
        logger.info(`Revoked invitation for ${invitation.email}`);
        showDeleteSuccess('Invitation', invitation.email || 'Invitation');
    } catch (error) {
        handleError(error, 'Failed to revoke invitation');
    } finally {
        isRevoking.value = false;
    }
};

const getRoleLabel = (role: ProjectRole): string => {
    const roleObj = availableRoles.find(r => r.value === role);
    return roleObj ? roleObj.label : role;
};

const getRoleDescription = (role: ProjectRole | ''): string => {
    if (!role) return '';
    
    const descriptions = {
        [ProjectRole.MANAGER]: 'Full access to project settings and data',
        [ProjectRole.ANNOTATOR]: 'Can create and edit annotations',
        [ProjectRole.REVIEWER]: 'Can review and approve annotations',
        [ProjectRole.VIEWER]: 'Read-only access to project data'
    };
    
    return descriptions[role] || '';
};

const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
};

// Lifecycle
onMounted(() => {
    loadMembers();
});
</script>

<style lang="scss" scoped>
.members-section {
    max-width: 900px;
}

.invite-form-section,
.members-list-section,
.invitations-list-section {
    margin-bottom: 2rem;
    
    &:last-child {
        margin-bottom: 0;
    }
    
    h3 {
        font-size: 1.25rem;
        margin-bottom: 1rem;
        color: var(--color-gray-800);
    }
}

.invite-form {
    display: grid;
    grid-template-columns: 1fr 200px auto;
    gap: 1rem;
    align-items: end;
    padding: 1.5rem;
    background-color: var(--color-gray-50);
    border-radius: 4px;
    
    @media (max-width: 768px) {
        grid-template-columns: 1fr;
    }
}

.field-error {
    color: var(--color-error);
    font-size: 0.875rem;
    font-weight: 500;
    margin-top: 0.25rem;
}

.field-help {
    color: var(--color-gray-600);
    font-size: 0.875rem;
    line-height: 1.4;
    margin-top: 0.25rem;
}

.loading-state,
.empty-state {
    padding: 1.5rem;
    text-align: center;
    color: var(--color-gray-600);
    font-style: italic;
    background-color: var(--color-gray-50);
    border-radius: 4px;
}

.members-list,
.invitations-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.member-item,
.invitation-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    background-color: var(--color-white);
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    transition: box-shadow 0.2s ease-in-out;
    
    &:hover {
        box-shadow: 0 1px 3px rgba(var(--color-black), 0.05);
    }
}

.member-info,
.invitation-info {
    flex-grow: 1;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.member-details,
.invitation-details {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.member-name,
.invitation-email {
    font-weight: 500;
    color: var(--color-gray-800);
    font-size: 1rem;
}

.member-email {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.member-meta,
.invitation-meta {
    display: flex;
    gap: 1rem;
    align-items: center;
}

.member-role,
.invitation-role {
    padding: 0.25rem 0.5rem;
    border-radius: 2px;
    font-size: 0.875rem;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    
    &.role-manager {
        background-color: var(--color-error);
        color: var(--color-error);
    }
    
    &.role-annotator {
        background-color: var(--color-primary);
        color: var(--color-primary);
    }
    
    &.role-reviewer {
        background-color: var(--color-warning);
        color: var(--color-warning);
    }
    
    &.role-viewer {
        background-color: var(--color-secondary);
        color: var(--color-secondary);
    }
}

.member-joined,
.invitation-sent {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.member-actions,
.invitation-actions {
    display: flex;
    gap: 0.5rem;
    align-items: center;
}

.role-select {
    min-width: 120px;
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 2px;
    font-size: 0.875rem;
    background-color: var(--color-white);
    color: var(--color-gray-800);
    
    &:focus {
        outline: none;
        border-color: var(--color-primary);
    }
    
    &:disabled {
        background-color: var(--color-gray-50);
        opacity: 0.6;
        cursor: not-allowed;
    }
}

.remove-btn,
.revoke-btn {
    background-color: var(--color-error);
    border-color: var(--color-error);
    color: var(--color-white);
    
    &:hover:not(:disabled) {
        background-color: var(--color-error-dark);
        border-color: var(--color-error-dark);
    }
    
    &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }
}

// Responsive adjustments
@media (max-width: 768px) {
    .member-item,
    .invitation-item {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
    
    .member-actions,
    .invitation-actions {
        justify-content: flex-end;
    }
}
</style>
