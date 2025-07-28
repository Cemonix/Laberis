import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { useProjectStore } from '@/stores/projectStore';
import { ProjectRole } from '@/types/project/project';

/**
 * Composable for checking project-level permissions
 * Provides helpers to check if the current user has specific roles in the currently loaded project
 */
export function useProjectPermissions() {
    const authStore = useAuthStore();
    const projectStore = useProjectStore();

    /**
     * Get the current user's role in the currently loaded project
     */
    const currentUserProjectRole = computed((): ProjectRole | null => {
        const currentUser = authStore.currentUser;
        const projectMembers = projectStore.activeMembers;
        
        // Debug logging for development
        if (process.env.NODE_ENV === 'development') {
            console.log('[ProjectPermissions] Debug info:', {
                currentUserEmail: currentUser?.email,
                projectMembersCount: projectMembers.length,
                projectMembers: projectMembers.map(m => ({ email: m.email, role: m.role }))
            });
        }
        
        if (!currentUser?.email || !projectMembers.length) {
            console.log('[ProjectPermissions] Missing data:', {
                hasCurrentUser: !!currentUser?.email,
                hasMembersLoaded: projectMembers.length > 0
            });
            return null;
        }

        const memberRecord = projectMembers.find(member => 
            member.email.toLowerCase() === currentUser.email.toLowerCase()
        );
        
        if (process.env.NODE_ENV === 'development') {
            console.log('[ProjectPermissions] Member lookup result:', {
                searchEmail: currentUser.email,
                foundMember: memberRecord ? { email: memberRecord.email, role: memberRecord.role } : null
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
     * Check if the user is a member of the current project
     */
    const isProjectMember = computed(() => currentUserProjectRole.value !== null);

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
        
        // Utility
        currentRoleLabel,
        isProjectMember
    };
}