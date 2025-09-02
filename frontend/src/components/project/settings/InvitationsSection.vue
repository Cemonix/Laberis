<template>
    <div class="invitations-section">
        <div class="section-header">
            <h2>Invitations</h2>
            <p>Send invitations to new team members and manage pending invitations.</p>
        </div>

        <!-- Invite New Member Form -->
        <div v-if="canInviteMembers" class="invite-form-card">
            <h3>
                <font-awesome-icon :icon="faEnvelope" />
                Invite New Member
            </h3>
            
            <form @submit.prevent="handleSendInvite" class="invite-form">
                <div class="form-grid">
                    <!-- Row 1: Labels -->
                    <label for="invite-email">Email Address</label>
                    <label for="invite-role">Role</label>
                    <span></span>
                    
                    <!-- Row 2: Controls -->
                    <input
                        id="invite-email"
                        v-model="inviteForm.email"
                        type="email"
                        placeholder="colleague@company.com"
                        required
                        :disabled="isSending"
                    />
                    <select
                        id="invite-role"
                        v-model="inviteForm.role"
                        required
                        :disabled="isSending"
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
                    <Button
                        type="submit"
                        variant="primary"
                        :disabled="isSending || !isInviteFormValid"
                        :loading="isSending"
                    >
                        {{ isSending ? 'Sending...' : 'Send Invitation' }}
                    </Button>
                </div>
                
                <!-- Error messages -->
                <div class="form-errors">
                    <div v-if="inviteErrors.email" class="field-error">{{ inviteErrors.email }}</div>
                    <div v-if="inviteErrors.role" class="field-error">{{ inviteErrors.role }}</div>
                </div>

                <!-- Role Description -->
                <div v-if="inviteForm.role" class="role-description">
                    <font-awesome-icon :icon="faInfoCircle" />
                    <span>{{ getRoleDescription(inviteForm.role) }}</span>
                </div>
            </form>
        </div>

        <!-- No Permission Message -->
        <div v-else class="no-permission">
            <font-awesome-icon :icon="faLock" />
            <p>You don't have permission to invite new members to this project.</p>
        </div>

        <!-- Pending Invitations List -->
        <div class="invitations-list-card">
            <div class="list-header">
                <h3>
                    <font-awesome-icon :icon="faClock" />
                    Pending Invitations
                    <span class="count-badge">{{ pendingInvitations.length }}</span>
                </h3>
                
                <Button
                    v-if="pendingInvitations.length > 0"
                    variant="secondary"
                    size="small"
                    @click="loadInvitations"
                    :disabled="isLoading"
                >
                    <font-awesome-icon :icon="faRefresh" />
                    Refresh
                </Button>
            </div>

            <div v-if="isLoading" class="loading-state">
                <div class="loading-spinner"></div>
                <p>Loading invitations...</p>
            </div>

            <div v-else-if="pendingInvitations.length === 0" class="empty-state">
                <font-awesome-icon :icon="faEnvelope" class="empty-icon" />
                <h4>No Pending Invitations</h4>
                <p>All sent invitations have been accepted or there are no invitations yet.</p>
            </div>

            <div v-else class="invitations-list">
                <div 
                    v-for="invitation in pendingInvitations" 
                    :key="invitation.id"
                    class="invitation-card"
                >
                    <div class="invitation-info">
                        <div class="invitation-main">
                            <span class="invitation-email">{{ invitation.email }}</span>
                            <span class="invitation-role" :class="`role-${invitation.role.toLowerCase()}`">
                                {{ getRoleLabel(invitation.role) }}
                            </span>
                        </div>
                        <div class="invitation-meta">
                            <span class="invitation-date">
                                <font-awesome-icon :icon="faCalendar" />
                                Invited {{ formatDate(invitation.invitedAt) }}
                            </span>
                        </div>
                    </div>
                    
                    <div class="invitation-actions">
                        <Button
                            v-if="canRevokeInvitations"
                            variant="danger"
                            size="small"
                            @click="handleRevokeInvitation(invitation)"
                            :disabled="isRevoking"
                        >
                            <font-awesome-icon :icon="faTimes" />
                            Revoke
                        </Button>
                        <Button
                            variant="secondary"
                            size="small"
                            @click="resendInvitation(invitation)"
                            :disabled="isResending"
                        >
                            <font-awesome-icon :icon="faRedo" />
                            Resend
                        </Button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, onMounted, reactive, ref} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faEnvelope,
    faLock,
    faInfoCircle,
    faClock,
    faRefresh,
    faCalendar,
    faTimes,
    faRedo
} from '@fortawesome/free-solid-svg-icons';
import Button from '@/components/common/Button.vue';
import {projectMemberService} from '@/services/project';
import {useToast} from '@/composables/useToast';
import {useConfirm} from '@/composables/useConfirm';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/services/auth/permissions.types';
import {AppLogger} from '@/core/logger/logger';
import type {InviteMemberRequest, ProjectMember} from '@/services/project/projectMember.types';
import {ProjectRole} from '@/services/project/project.types';

const logger = AppLogger.createComponentLogger('InvitationsSection');

interface Props {
    projectId: number;
}

const props = defineProps<Props>();

const { showToast } = useToast();
const { showConfirm } = useConfirm();
const { handleError } = useErrorHandler();
const { hasProjectPermission } = usePermissions();

// State
const pendingInvitations = ref<ProjectMember[]>([]);
const isLoading = ref(true);
const isSending = ref(false);
const isRevoking = ref(false);
const isResending = ref(false);

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
    { value: ProjectRole.REVIEWER, label: 'Reviewer' },
    { value: ProjectRole.ANNOTATOR, label: 'Annotator' },
    { value: ProjectRole.VIEWER, label: 'Viewer' }
];

// Computed
const canInviteMembers = computed(() => 
    hasProjectPermission(PERMISSIONS.PROJECT_MEMBER.INVITE)
);

const canRevokeInvitations = computed(() => 
    hasProjectPermission(PERMISSIONS.PROJECT_MEMBER.REMOVE)
);

const isInviteFormValid = computed(() => {
    return inviteForm.email.trim() && 
        inviteForm.role && 
        !inviteErrors.email && 
        !inviteErrors.role;
});

// Methods
const loadInvitations = async () => {
    isLoading.value = true;
    try {
        const members = await projectMemberService.getProjectMembers(props.projectId);
        
        // Filter only pending invitations (no joinedAt date)
        pendingInvitations.value = members.filter(m => !m.joinedAt);
        
        logger.info(`Loaded ${pendingInvitations.value.length} pending invitations`);
    } catch (error) {
        logger.error('Failed to load invitations', error);
        handleError(error, 'Failed to load pending invitations');
    } finally {
        isLoading.value = false;
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
    
    // Check if user is already invited
    const email = inviteForm.email.trim().toLowerCase();
    const isAlreadyInvited = pendingInvitations.value.some(m => 
        m.email?.toLowerCase() === email
    );
    
    if (isAlreadyInvited) {
        inviteErrors.email = 'This user has already been invited';
        return false;
    }
    
    return true;
};

const handleSendInvite = async () => {
    if (!validateInviteForm() || !canInviteMembers.value) {
        return;
    }
    
    isSending.value = true;
    try {
        const inviteData: InviteMemberRequest = {
            email: inviteForm.email.trim(),
            role: inviteForm.role as ProjectRole
        };
        
        await projectMemberService.inviteMember(props.projectId, inviteData);
        
        // Reset form
        inviteForm.email = '';
        inviteForm.role = '';
        
        // Reload invitations to show the new one
        await loadInvitations();
        
        logger.info(`Sent invitation to ${inviteData.email} with role ${inviteData.role}`);
        showToast('Success', `Invitation sent to ${inviteData.email}`, 'success');
    } catch (error) {
        logger.error('Failed to send invitation', error);
        handleError(error, 'Failed to send invitation');
    } finally {
        isSending.value = false;
    }
};

const handleRevokeInvitation = async (invitation: ProjectMember) => {
    const confirmed = await showConfirm(
        'Revoke Invitation', 
        `Are you sure you want to revoke the invitation for ${invitation.email}? This action cannot be undone.`
    );
    
    if (!confirmed) return;
    
    isRevoking.value = true;
    try {
        await projectMemberService.removeMember(props.projectId, invitation.email);
        pendingInvitations.value = pendingInvitations.value.filter(i => i.id !== invitation.id);
        
        logger.info(`Revoked invitation for ${invitation.email}`);
        showToast('Success', `Invitation revoked for ${invitation.email}`, 'success');
    } catch (error) {
        logger.error('Failed to revoke invitation', error);
        handleError(error, 'Failed to revoke invitation');
    } finally {
        isRevoking.value = false;
    }
};

const resendInvitation = async (invitation: ProjectMember) => {
    const confirmed = await showConfirm(
        'Resend Invitation', 
        `Do you want to resend the invitation to ${invitation.email}?`
    );
    
    if (!confirmed) return;
    
    isResending.value = true;
    try {
        const inviteData: InviteMemberRequest = {
            email: invitation.email,
            role: invitation.role
        };
        
        await projectMemberService.inviteMember(props.projectId, inviteData);
        
        logger.info(`Resent invitation to ${invitation.email}`);
        showToast('Success', `Invitation resent to ${invitation.email}`, 'success');
    } catch (error) {
        logger.error('Failed to resend invitation', error);
        handleError(error, 'Failed to resend invitation');
    } finally {
        isResending.value = false;
    }
};

const getRoleLabel = (role: ProjectRole): string => {
    const roleObj = availableRoles.find(r => r.value === role);
    return roleObj ? roleObj.label : role;
};

const getRoleDescription = (role: ProjectRole | ''): string => {
    if (!role) return '';
    
    const descriptions = {
        [ProjectRole.MANAGER]: 'Full access to project settings, data, and member management',
        [ProjectRole.REVIEWER]: 'Can review and approve annotations, view all project data',
        [ProjectRole.ANNOTATOR]: 'Can create and edit annotations on assigned tasks',
        [ProjectRole.VIEWER]: 'Read-only access to project data and annotations'
    };
    
    return descriptions[role] || '';
};

const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffInDays === 0) return 'today';
    if (diffInDays === 1) return 'yesterday';
    if (diffInDays < 7) return `${diffInDays} days ago`;
    
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
};

// Lifecycle
onMounted(() => {
    loadInvitations();
});
</script>

<style lang="scss" scoped>
.invitations-section {
    padding: 2rem;
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

.section-header {
    h2 {
        font-size: 1.5rem;
        font-weight: 600;
        color: var(--color-gray-900);
        margin-bottom: 0.5rem;
    }
    
    p {
        color: var(--color-gray-600);
        line-height: 1.5;
    }
}

.invite-form-card, .invitations-list-card {
    background-color: var(--color-gray-50);
    border-radius: 8px;
    padding: 1.5rem;
    border: 1px solid var(--color-gray-200);
    
    h3 {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-gray-800);
        margin-bottom: 1rem;
        
        svg {
            color: var(--color-primary);
        }
    }
}

.invite-form {
    .form-grid {
        display: grid;
        grid-template-columns: 5fr 2fr 1fr;
        grid-template-rows: auto 1fr;
        gap: 0.5rem 1rem;
        align-items: end;
        margin-bottom: 1rem;
        
        @media (max-width: 768px) {
            grid-template-columns: 1fr;
            grid-template-rows: repeat(6, auto);
        }
        
        label {
            font-weight: 500;
            color: var(--color-gray-700);
            font-size: 0.875rem;
            margin-bottom: 0.25rem;
            align-self: end;
        }
        
        input, select {
            padding: 0.75rem;
            border: 1px solid var(--color-gray-300);
            border-radius: 4px;
            font-size: 1rem;
            background-color: var(--color-white);
            box-sizing: border-box;
            
            &:focus {
                outline: none;
                border-color: var(--color-primary);
                box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
            }
            
            &:disabled {
                background-color: var(--color-gray-100);
                cursor: not-allowed;
            }
        }
        
        button {
            font-size: 0.875rem;
            padding: 0.75rem 1.25rem;
            border-radius: 6px;
            transition: all 0.2s ease;
            white-space: nowrap;
            height: 100%;
            width: 100%;
            
            &:hover:not(:disabled) {
                transform: translateY(-1px);
                box-shadow: 0 2px 8px rgba(var(--color-primary-rgb), 0.3);
            }
            
            &:disabled {
                border: 1px solid var(--color-gray-300);
                background-color: var(--color-gray-100);
                color: var(--color-gray-500);
                cursor: not-allowed;
            }
        }
    }
    
    .form-errors {
        display: grid;
        grid-template-columns: 5fr 2fr 1fr;
        gap: 0 1rem;
        
        @media (max-width: 768px) {
            grid-template-columns: 1fr;
        }
    }
}

.role-description {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem;
    background-color: var(--color-blue-50);
    border-left: 4px solid var(--color-blue-400);
    border-radius: 4px;
    color: var(--color-blue-800);
    font-size: 0.875rem;
    line-height: 1.4;
    
    svg {
        color: var(--color-blue-500);
        flex-shrink: 0;
    }
}

.no-permission {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1.5rem;
    background-color: var(--color-yellow-50);
    border: 1px solid var(--color-yellow-200);
    border-radius: 8px;
    color: var(--color-yellow-800);
    
    svg {
        color: var(--color-yellow-600);
        font-size: 1.25rem;
    }
    
    p {
        margin: 0;
        font-weight: 500;
    }
}

.list-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    
    h3 {
        margin-bottom: 0;
        
        .count-badge {
            background-color: var(--color-primary);
            color: white;
            padding: 0.25rem 0.5rem;
            border-radius: 12px;
            font-size: 0.75rem;
            font-weight: 600;
            margin-left: 0.5rem;
        }
    }
}

.loading-state, .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--color-gray-600);
    text-align: center;
    
    .loading-spinner {
        width: 2rem;
        height: 2rem;
        border: 3px solid var(--color-gray-300);
        border-top: 3px solid var(--color-primary);
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }
    
    .empty-icon {
        font-size: 3rem;
        color: var(--color-gray-400);
        margin-bottom: 1rem;
    }
    
    h4 {
        font-size: 1.125rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
        color: var(--color-gray-700);
    }
    
    p {
        margin: 0;
        line-height: 1.5;
    }
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.invitations-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.invitation-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    background-color: var(--color-white);
    border: 1px solid var(--color-gray-300);
    border-radius: 6px;
    transition: all 0.2s ease;
    
    &:hover {
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        border-color: var(--color-gray-400);
    }
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
}

.invitation-info {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex-grow: 1;
}

.invitation-main {
    display: flex;
    align-items: center;
    gap: 1rem;
    
    .invitation-email {
        font-weight: 500;
        color: var(--color-gray-900);
        font-size: 1rem;
    }
    
    .invitation-role {
        padding: 0.25rem 0.75rem;
        border-radius: 12px;
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        
        &.role-manager {
            background-color: var(--color-red-100);
            color: var(--color-red-800);
        }
        
        &.role-reviewer {
            background-color: var(--color-yellow-100);
            color: var(--color-yellow-800);
        }
        
        &.role-annotator {
            background-color: var(--color-blue-100);
            color: var(--color-blue-800);
        }
        
        &.role-viewer {
            background-color: var(--color-gray-100);
            color: var(--color-gray-800);
        }
    }
}

.invitation-meta {
    .invitation-date {
        display: flex;
        align-items: center;
        gap: 0.375rem;
        font-size: 0.875rem;
        color: var(--color-gray-600);
        
        svg {
            font-size: 0.75rem;
        }
    }
}

.invitation-actions {
    display: flex;
    gap: 0.5rem;
    
    @media (max-width: 768px) {
        justify-content: flex-end;
    }
}

.field-error {
    color: var(--color-error);
    font-size: 0.875rem;
    font-weight: 500;
}
</style>