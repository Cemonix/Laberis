import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { ProjectRole } from '@/types/project/project';
import { AppLogger } from '@/utils/logger';
import { env } from '@/config/env';

/**
 * Composable for checking project-level permissions
 * Provides helpers to check if the current user has specific roles in the currently loaded project
 */
export function useProjectPermissions() {
    const authStore = useAuthStore();
    const projectStore = useProjectStore();
    const logger = AppLogger.createServiceLogger('ProjectPermissions');

    /**
     * Get the current user's role in the currently loaded project
     * Only considers members who have actually joined (accepted their invitation)
     */
    const currentUserProjectRole = computed((): ProjectRole | null => {
        const currentUser = authStore.currentUser;
        const joinedMembers = projectStore.joinedMembers;
        
        if (env.IS_DEVELOPMENT) {
            logger.debug('Debug info:', {
                currentUserEmail: currentUser?.email,
                joinedMembersCount: joinedMembers?.length || 0,
                activeMembersCount: projectStore.activeMembers?.length || 0,
                joinedMembers: joinedMembers?.map(m => ({ email: m.email, role: m.role, joinedAt: m.joinedAt })) || []
            });
        }

        if (!currentUser?.email || !joinedMembers?.length) {
            logger.debug('Missing data or user not joined:', {
                hasCurrentUser: !!currentUser?.email,
                hasJoinedMembersLoaded: (joinedMembers?.length || 0) > 0,
                totalMembersCount: projectStore.activeMembers?.length || 0
            });
            return null;
        }

        const memberRecord = joinedMembers.find(member => 
            member.email.toLowerCase() === currentUser.email.toLowerCase()
        );
        
        if (env.IS_DEVELOPMENT) {
            logger.debug('Member lookup result:', {
                searchEmail: currentUser.email,
                foundJoinedMember: memberRecord ? { email: memberRecord.email, role: memberRecord.role, joinedAt: memberRecord.joinedAt } : null,
                userStatus: memberRecord ? 'joined' : 'not-joined-or-not-invited'
            });
        }
        
        return memberRecord?.role || null;
    });

    /**
     * Check if the current user has a specific role in the current project
     */
    const hasProjectRole = (role: ProjectRole): boolean => {
        return currentUserProjectRole.value === role;
    };

    /**
     * Check if the current user has any of the specified roles in the current project
     */
    const hasAnyProjectRole = (roles: ProjectRole[]): boolean => {
        const userRole = currentUserProjectRole.value;
        return userRole ? roles.includes(userRole) : false;
    };

    /**
     * Check if the current user can manage the current project
     * Only project managers can perform management actions
     */
    const canManageProject = computed(() => hasProjectRole(ProjectRole.MANAGER));

    /**
     * Check if the current user can annotate in the current project
     * Managers, annotators, and reviewers can create/edit annotations
     */
    const canAnnotate = computed(() => 
        hasAnyProjectRole([ProjectRole.MANAGER, ProjectRole.ANNOTATOR, ProjectRole.REVIEWER])
    );

    /**
     * Check if the current user can review annotations in the current project
     * Managers and reviewers can review annotations
     */
    const canReview = computed(() => 
        hasAnyProjectRole([ProjectRole.MANAGER, ProjectRole.REVIEWER])
    );

    /**
     * Check if the current user can view the current project
     * All project members can view (this should always be true if user is in project)
     */
    const canView = computed(() => 
        hasAnyProjectRole([ProjectRole.MANAGER, ProjectRole.ANNOTATOR, ProjectRole.REVIEWER, ProjectRole.VIEWER])
    );

    /**
     * Get a human-readable label for the current user's project role
     */
    const currentRoleLabel = computed((): string => {
        const role = currentUserProjectRole.value;
        if (!role) return 'No role';
        
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
                return 'Unknown';
        }
    });

    /**
     * Check if the user is a member of the current project (has joined)
     */
    const isProjectMember = computed(() => currentUserProjectRole.value !== null);

    /**
     * Check if the user has a pending invitation to the current project
     */
    const hasPendingInvitation = computed((): boolean => {
        const currentUser = authStore.currentUser;
        const pendingMembers = projectStore.pendingMembers;
        
        if (!currentUser?.email || !pendingMembers?.length) {
            return false;
        }

        return pendingMembers.some(member => 
            member.email.toLowerCase() === currentUser.email.toLowerCase()
        );
    });

    /**
     * Check if the user is associated with the project in any way (invited or joined)
     */
    const isAssociatedWithProject = computed(() => isProjectMember.value || hasPendingInvitation.value);

    /**
     * Get the user's invitation status in the project
     */
    const invitationStatus = computed((): 'not-invited' | 'pending' | 'joined' => {
        if (isProjectMember.value) return 'joined';
        if (hasPendingInvitation.value) return 'pending';
        return 'not-invited';
    });

    return {
        // Role checking
        currentUserProjectRole,
        hasProjectRole,
        hasAnyProjectRole,
        
        // Permission helpers
        canManageProject,
        canAnnotate,
        canReview,
        canView,
        
        // Membership status
        isProjectMember,
        hasPendingInvitation,
        isAssociatedWithProject,
        invitationStatus,
        
        // Utility
        currentRoleLabel
    };
}