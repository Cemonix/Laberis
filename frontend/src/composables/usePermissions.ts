import { computed, type ComputedRef } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { usePermissionStore } from '@/stores/permissionStore';
import { useProjectStore } from '@/stores/projectStore';

/**
 * Main composable for permission checking throughout the application.
 * Provides both global and project-specific permission checks.
 */
export function usePermissions() {
    const authStore = useAuthStore();
    const permissionStore = usePermissionStore();
    const projectStore = useProjectStore();

    // Permission-based checks
    
    /**
     * Checks if user has a global permission (available regardless of project)
     */
    const hasGlobalPermission = (permission: string): boolean => {
        return permissionStore.hasGlobalPermission(permission);
    };

    /**
     * Checks if user has any of the specified global permissions
     */
    const hasAnyGlobalPermission = (permissions: string[]): boolean => {
        return permissionStore.hasAnyGlobalPermission(permissions);
    };

    /**
     * Checks if user has all of the specified global permissions
     */
    const hasAllGlobalPermissions = (permissions: string[]): boolean => {
        return permissionStore.hasAllGlobalPermissions(permissions);
    };

    /**
     * Checks if user has a permission in the current project
     */
    const hasProjectPermission = (permission: string): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        return permissionStore.hasProjectPermission(permission, currentProjectId);
    };

    /**
     * Checks if user has a permission in a specific project
     */
    const hasPermissionInProject = (permission: string, projectId: number): boolean => {
        return permissionStore.hasProjectPermission(permission, projectId);
    };

    /**
     * Checks if user has any of the specified permissions in current project
     */
    const hasAnyProjectPermission = (permissions: string[]): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        return permissionStore.hasAnyProjectPermission(permissions, currentProjectId);
    };

    /**
     * Checks if user has all of the specified permissions in current project
     */
    const hasAllProjectPermissions = (permissions: string[]): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        return permissionStore.hasAllProjectPermissions(permissions, currentProjectId);
    };

    /**
     * Checks if user has a permission anywhere (global or any project)
     */
    const hasAnyPermission = (permission: string): boolean => {
        return permissionStore.hasAnyPermission(permission);
    };

    /**
     * Checks if user is a member of the current project
     */
    const isCurrentProjectMember = computed(() => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        return permissionStore.isProjectMember(currentProjectId);
    });

    /**
     * Gets all permissions for the current project
     */
    const currentProjectPermissions = computed(() => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return new Set<string>();
        return permissionStore.getProjectPermissions(currentProjectId);
    });

    // Reactive computed permission checkers

    /**
     * Reactive checker for global permissions
     */
    const canDoGlobally = (permission: string): ComputedRef<boolean> => {
        return computed(() => hasGlobalPermission(permission));
    };

    /**
     * Reactive checker for current project permissions
     */
    const canDoInProject = (permission: string): ComputedRef<boolean> => {
        return computed(() => hasProjectPermission(permission));
    };

    /**
     * Reactive checker for any permission (global or any project)
     */
    const canDoAnywhere = (permission: string): ComputedRef<boolean> => {
        return computed(() => hasAnyPermission(permission));
    };

    // Common permission computed properties
    const canManageAccount = computed(() => hasGlobalPermission('account:update'));
    const canChangePassword = computed(() => hasGlobalPermission('account:change:password'));
    
    // Project-specific computed properties
    const canViewProject = computed(() => hasProjectPermission('project:read'));
    const canUpdateProject = computed(() => hasProjectPermission('project:update'));
    const canDeleteProject = computed(() => hasProjectPermission('project:delete'));
    const canManageProjectMembers = computed(() => hasProjectPermission('projectMember:invite'));
    const canManageDataSources = computed(() => hasProjectPermission('dataSource:create'));
    const canManageWorkflows = computed(() => hasProjectPermission('workflow:create'));
    const canManageLabelSchemes = computed(() => hasProjectPermission('labelScheme:create'));
    const canCreateAnnotations = computed(() => hasProjectPermission('annotation:create'));
    const canReviewAnnotations = computed(() => hasProjectPermission('annotation:review'));
    const canAssignTasks = computed(() => hasProjectPermission('task:assign'));

    return {
        // Global permission checks
        hasGlobalPermission,
        hasAnyGlobalPermission,
        hasAllGlobalPermissions,

        // Project permission checks
        hasProjectPermission,
        hasPermissionInProject,
        hasAnyProjectPermission,
        hasAllProjectPermissions,

        // General permission checks
        hasAnyPermission,
        isCurrentProjectMember,
        currentProjectPermissions,

        // Reactive permission checkers
        canDoGlobally,
        canDoInProject,
        canDoAnywhere,

        // Common computed permissions
        canManageAccount,
        canChangePassword,
        canViewProject,
        canUpdateProject,
        canDeleteProject,
        canManageProjectMembers,
        canManageDataSources,
        canManageWorkflows,
        canManageLabelSchemes,
        canCreateAnnotations,
        canReviewAnnotations,
        canAssignTasks,

        // Store references for advanced usage
        permissionStore,
        authStore,
        projectStore,
    };
}