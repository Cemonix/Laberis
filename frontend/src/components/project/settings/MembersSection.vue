<template>
    <div class="members-section">
        <div class="section-header">
            <h2>Project Members</h2>
            <p>View and manage current project team members and their roles.</p>
        </div>

        <!-- Current Members Section -->
        <div class="members-list-section">
            <div class="list-header">
                <div class="list-title">
                    <h3>Team Members</h3>
                    <span class="member-count">{{ filteredMembers.length }} members</span>
                </div>
                <div class="member-controls">
                    <div class="search-filter">
                        <input
                            v-model="memberSearch"
                            type="text"
                            placeholder="Search members..."
                            class="search-input"
                        />
                        <select v-model="roleFilter" class="role-filter">
                            <option value="">All Roles</option>
                            <option 
                                v-for="role in availableRoles" 
                                :key="role.value" 
                                :value="role.value"
                            >
                                {{ role.label }}
                            </option>
                        </select>
                    </div>
                    <div v-if="filteredMembers.length > 1" class="bulk-actions">
                        <Button
                            variant="secondary"
                            size="small"
                            @click="toggleBulkMode"
                        >
                            {{ isBulkMode ? 'Cancel Bulk' : 'Bulk Actions' }}
                        </Button>
                    </div>
                </div>
            </div>
            
            <!-- Bulk Actions Bar -->
            <div v-if="isBulkMode" class="bulk-action-bar">
                <div class="bulk-selection">
                    <label class="select-all">
                        <input
                            type="checkbox"
                            :checked="isAllSelected"
                            :indeterminate="isSomeSelected"
                            @change="handleSelectAll"
                        />
                        <span>Select All ({{ selectedMembers.size }} selected)</span>
                    </label>
                </div>
                <div v-if="selectedMembers.size > 0" class="bulk-operations">
                    <select v-model="bulkRole" class="bulk-role-select">
                        <option value="">Change Role To...</option>
                        <option 
                            v-for="role in availableRoles" 
                            :key="role.value" 
                            :value="role.value"
                        >
                            {{ role.label }}
                        </option>
                    </select>
                    <Button
                        variant="primary"
                        size="small"
                        @click="handleBulkRoleUpdate"
                        :disabled="!bulkRole || isUpdatingRole"
                    >
                        Apply Role
                    </Button>
                    <Button
                        variant="secondary"
                        size="small"
                        @click="handleBulkRemove"
                        :disabled="isRemoving"
                        class="bulk-remove-btn"
                    >
                        Remove Selected
                    </Button>
                </div>
            </div>
            
            <div v-if="isLoadingMembers" class="loading-state">
                Loading members...
            </div>
            <div v-else-if="currentMembers.length === 0" class="empty-state">
                No members found.
            </div>
            <div v-else-if="filteredMembers.length === 0" class="empty-state">
                No members match your search criteria.
            </div>
            <div v-else class="members-list">
                <div 
                    v-for="member in filteredMembers" 
                    :key="member.id"
                    class="member-item"
                    :class="{ 'bulk-mode': isBulkMode }"
                >
                    <div v-if="isBulkMode" class="member-checkbox">
                        <input
                            type="checkbox"
                            :checked="selectedMembers.has(member.id)"
                            @change="handleMemberSelect(member.id, ($event.target as HTMLInputElement).checked)"
                            :disabled="member.email === currentUserEmail"
                        />
                    </div>
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
                            <span v-if="(member as any).lastActivityAt" class="member-activity">
                                Active {{ formatRelativeTime((member as any).lastActivityAt) }}
                            </span>
                            <span v-else class="member-activity inactive">
                                No recent activity
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
    </div>
</template>

<script setup lang="ts">
// TODO: REFACTOR THIS FILE

import {computed, onMounted, ref} from 'vue';
import Button from '@/components/common/Button.vue';
import {projectMemberService} from '@/services/project';
import {useToast} from '@/composables/useToast';
import {useConfirm} from '@/composables/useConfirm';
import {useErrorHandler} from '@/composables/useErrorHandler';
import {useAuthStore} from '@/stores/authStore';
import {AppLogger} from '@/core/logger/logger';
import type {ProjectMember} from '@/services/project/projectMember.types';
import {ProjectRole} from '@/services/project/project.types';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const logger = AppLogger.createComponentLogger('MembersSection');

interface Props {
    projectId: number;
}

const props = defineProps<Props>();

const { showToast } = useToast();
const { showConfirm } = useConfirm();
const { handleError } = useErrorHandler();
const authStore = useAuthStore();

// State
const currentMembers = ref<ProjectMember[]>([]);
const isLoadingMembers = ref(false);
const isRemoving = ref(false);
const isUpdatingRole = ref(false);

// Search and filtering state
const memberSearch = ref('');
const roleFilter = ref<ProjectRole | ''>('');

// Bulk operations state
const isBulkMode = ref(false);
const selectedMembers = ref(new Set<number>());
const bulkRole = ref<ProjectRole | ''>('');

// Available roles for selection
const availableRoles = [
    { value: ProjectRole.MANAGER, label: 'Manager' },
    { value: ProjectRole.ANNOTATOR, label: 'Annotator' },
    { value: ProjectRole.REVIEWER, label: 'Reviewer' },
    { value: ProjectRole.VIEWER, label: 'Viewer' }
];

// Computed
const currentUserEmail = computed(() => authStore.user?.email);

const filteredMembers = computed(() => {
    let filtered = currentMembers.value;
    
    // Filter by search term
    if (memberSearch.value.trim()) {
        const searchLower = memberSearch.value.toLowerCase();
        filtered = filtered.filter(member => 
            (member.userName?.toLowerCase().includes(searchLower)) ||
            (member.email?.toLowerCase().includes(searchLower))
        );
    }
    
    // Filter by role
    if (roleFilter.value) {
        filtered = filtered.filter(member => member.role === roleFilter.value);
    }
    
    return filtered;
});

const isAllSelected = computed(() => {
    const selectableMembers = filteredMembers.value.filter(m => m.email !== currentUserEmail.value);
    return selectableMembers.length > 0 && selectableMembers.every(m => selectedMembers.value.has(m.id));
});

const isSomeSelected = computed(() => {
    const selectableMembers = filteredMembers.value.filter(m => m.email !== currentUserEmail.value);
    return selectableMembers.some(m => selectedMembers.value.has(m.id)) && !isAllSelected.value;
});

// Methods
const loadMembers = async () => {
    isLoadingMembers.value = true;
    try {
        const members = await projectMemberService.getProjectMembers(props.projectId);
        
        // Only show current members (those who have joined)
        currentMembers.value = members.filter(m => m.joinedAt);
        
        logger.info(`Loaded ${currentMembers.value.length} members`);
    } catch (error) {
        handleError(error, 'Failed to load project members');
    } finally {
        isLoadingMembers.value = false;
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
        showToast('Success', `${member.userName || member.email}'s role has been updated`, 'success');
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
        showToast('Success', `${member.userName || member.email || 'User'} removed from project`, 'success');
    } catch (error) {
        handleError(error, 'Failed to remove member');
    } finally {
        isRemoving.value = false;
    }
};


const getRoleLabel = (role: ProjectRole): string => {
    const roleObj = availableRoles.find(r => r.value === role);
    return roleObj ? roleObj.label : role;
};


const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
};

const formatRelativeTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}d ago`;
    
    return formatDate(dateString);
};

const toggleBulkMode = () => {
    isBulkMode.value = !isBulkMode.value;
    if (!isBulkMode.value) {
        selectedMembers.value.clear();
        bulkRole.value = '';
    }
};

const handleMemberSelect = (memberId: number, checked: boolean) => {
    if (checked) {
        selectedMembers.value.add(memberId);
    } else {
        selectedMembers.value.delete(memberId);
    }
};

const handleSelectAll = () => {
    const selectableMembers = filteredMembers.value.filter(m => m.email !== currentUserEmail.value);
    
    if (isAllSelected.value) {
        // Deselect all
        selectableMembers.forEach(member => selectedMembers.value.delete(member.id));
    } else {
        // Select all
        selectableMembers.forEach(member => selectedMembers.value.add(member.id));
    }
};

const handleBulkRoleUpdate = async () => {
    if (!bulkRole.value || selectedMembers.value.size === 0) return;
    
    const memberCount = selectedMembers.value.size;
    const confirmed = await showConfirm(
        'Update Roles', 
        `Are you sure you want to change the role of ${memberCount} member${memberCount > 1 ? 's' : ''} to ${getRoleLabel(bulkRole.value)}?`
    );
    
    if (!confirmed) return;
    
    isUpdatingRole.value = true;
    let successCount = 0;
    let failureCount = 0;
    
    try {
        for (const memberId of selectedMembers.value) {
            const member = currentMembers.value.find(m => m.id === memberId);
            if (member) {
                try {
                    await projectMemberService.updateMemberRole(props.projectId, member.email, bulkRole.value as ProjectRole);
                    member.role = bulkRole.value as ProjectRole;
                    successCount++;
                } catch (error) {
                    logger.error(`Failed to update role for ${member.email}:`, error);
                    failureCount++;
                }
            }
        }
        
        if (successCount > 0) {
            showToast('Success', `Successfully updated ${successCount} member role${successCount > 1 ? 's' : ''}`, 'success');
        }
        
        if (failureCount > 0) {
            handleError(new Error(`Failed to update ${failureCount} member${failureCount > 1 ? 's' : ''}`), 'Bulk Role Update');
        }
    } finally {
        isUpdatingRole.value = false;
        selectedMembers.value.clear();
        bulkRole.value = '';
    }
};

const handleBulkRemove = async () => {
    if (selectedMembers.value.size === 0) return;
    
    const memberCount = selectedMembers.value.size;
    const confirmed = await showConfirm(
        'Remove Members', 
        `Are you sure you want to remove ${memberCount} member${memberCount > 1 ? 's' : ''} from this project? This action cannot be undone.`
    );
    
    if (!confirmed) return;
    
    isRemoving.value = true;
    let successCount = 0;
    let failureCount = 0;
    
    try {
        for (const memberId of selectedMembers.value) {
            const member = currentMembers.value.find(m => m.id === memberId);
            if (member) {
                try {
                    await projectMemberService.removeMember(props.projectId, member.email);
                    successCount++;
                } catch (error) {
                    logger.error(`Failed to remove ${member.email}:`, error);
                    failureCount++;
                }
            }
        }
        
        // Reload members to reflect changes
        await loadMembers();
        
        if (successCount > 0) {
            showToast('Success', `Successfully removed ${successCount} member${successCount > 1 ? 's' : ''}`, 'success');
        }
        
        if (failureCount > 0) {
            handleError(new Error(`Failed to remove ${failureCount} member${failureCount > 1 ? 's' : ''}`), 'Bulk Remove');
        }
    } finally {
        isRemoving.value = false;
        selectedMembers.value.clear();
        toggleBulkMode();
    }
};

// Lifecycle
onMounted(() => {
    loadMembers();
});
</script>

<style lang="scss" scoped>
.members-section {
    padding: 2rem;
    max-width: 1000px;
    margin: 0 auto;
}

.section-header {
    margin-bottom: 2rem;
    
    h2 {
        font-size: 1.75rem;
        font-weight: 600;
        color: var(--color-gray-900);
        margin-bottom: 0.5rem;
    }
    
    p {
        color: var(--color-gray-600);
        line-height: 1.5;
        font-size: 1.1rem;
    }
}

.members-list-section {
    background-color: var(--color-white);
    border-radius: 8px;
    border: 1px solid var(--color-gray-200);
    overflow: hidden;
}

.list-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    background-color: var(--color-gray-50);
    border-bottom: 1px solid var(--color-gray-200);
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
}

.list-title {
    display: flex;
    align-items: center;
    gap: 1rem;
    
    h3 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-gray-800);
        margin: 0;
    }
    
    .member-count {
        background-color: var(--color-primary);
        color: white;
        padding: 0.25rem 0.75rem;
        border-radius: 12px;
        font-size: 0.875rem;
        font-weight: 500;
    }
}

.member-controls {
    display: flex;
    align-items: center;
    gap: 1rem;
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
    
    .search-filter {
        display: flex;
        gap: 0.75rem;
        align-items: center;
        
        @media (max-width: 768px) {
            flex-direction: column;
            gap: 0.5rem;
        }
    }
}

.search-input,
.role-filter {
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    font-size: 0.875rem;
    background-color: var(--color-white);
    
    &:focus {
        outline: none;
        border-color: var(--color-primary);
    }
}

.search-input {
    min-width: 200px;
    
    @media (max-width: 768px) {
        min-width: 100%;
    }
}

.role-filter {
    min-width: 120px;
    
    @media (max-width: 768px) {
        min-width: 100%;
    }
}

.bulk-actions {
    display: flex;
    align-items: center;
    margin-left: auto;
    
    button {
        font-size: 0.875rem;
        padding: 0.5rem 1rem;
        border-radius: 6px;
        transition: all 0.2s ease;
        
        &:hover {
            transform: translateY(-1px);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }
    }
    
    @media (max-width: 768px) {
        margin-left: 0;
        justify-content: flex-end;
    }
}

.bulk-action-bar {
    background-color: var(--color-primary-light);
    border: 1px solid var(--color-primary);
    border-radius: 4px;
    padding: 1rem;
    margin-bottom: 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    
    @media (max-width: 768px) {
        flex-direction: column;
        align-items: stretch;
    }
}

.bulk-selection {
    .select-all {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        cursor: pointer;
        font-weight: 500;
        
        input[type="checkbox"] {
            margin: 0;
        }
    }
}

.bulk-operations {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    
    @media (max-width: 768px) {
        justify-content: flex-end;
    }
}

.bulk-role-select {
    min-width: 150px;
    padding: 0.5rem;
    border: 1px solid var(--color-gray-400);
    border-radius: 4px;
    font-size: 0.875rem;
    background-color: var(--color-white);
}

.bulk-remove-btn {
    background-color: var(--color-error);
    border-color: var(--color-error);
    color: var(--color-white);
    
    &:hover:not(:disabled) {
        background-color: var(--color-error-dark);
        border-color: var(--color-error-dark);
    }
}

.member-item.bulk-mode {
    .member-checkbox {
        display: flex;
        align-items: center;
        margin-right: 1rem;
        
        input[type="checkbox"] {
            margin: 0;
        }
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
    padding: 3rem 1.5rem;
    text-align: center;
    color: var(--color-gray-600);
    font-style: italic;
}

.members-list {
    display: flex;
    flex-direction: column;
}

.member-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    background-color: var(--color-white);
    border-bottom: 1px solid var(--color-gray-200);
    transition: background-color 0.2s ease;
    
    &:hover {
        background-color: var(--color-gray-25);
    }
    
    &:last-child {
        border-bottom: none;
    }
}

.member-info {
    flex-grow: 1;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.member-details {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.member-name {
    font-weight: 500;
    color: var(--color-gray-900);
    font-size: 1rem;
}

.member-email {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.member-meta {
    display: flex;
    gap: 1rem;
    align-items: center;
    flex-wrap: wrap;
}

.member-role {
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
    
    &.role-annotator {
        background-color: var(--color-blue-100);
        color: var(--color-blue-800);
    }
    
    &.role-reviewer {
        background-color: var(--color-yellow-100);
        color: var(--color-yellow-800);
    }
    
    &.role-viewer {
        background-color: var(--color-gray-100);
        color: var(--color-gray-800);
    }
}

.member-joined {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.member-activity {
    font-size: 0.875rem;
    color: var(--color-success);
    font-weight: 500;
    
    &.inactive {
        color: var(--color-gray-500);
        font-weight: normal;
    }
}

.member-actions {
    display: flex;
    gap: 0.75rem;
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

.remove-btn {
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
    .member-item {
        flex-direction: column;
        align-items: stretch;
        gap: 1rem;
    }
    
    .member-actions {
        justify-content: flex-end;
    }
}
</style>
