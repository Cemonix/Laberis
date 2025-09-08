import { computed, reactive, type ComputedRef } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { authorizationService } from '@/services/auth';
import { useProjectStore } from '@/stores/projectStore';

/**
 * Main composable for permission checking throughout the application.
 * Provides both global and project-specific permission checks.
 */
export function usePermissions() {
    const authStore = useAuthStore();
    // Lightweight reactive cache for authorization checks
    const resultsCache = reactive<Record<string, boolean>>({});
    const pending = new Map<string, Promise<boolean>>();

    const makeKey = (perm: string, ctx?: { projectId?: number; global?: boolean }): string => {
        const pid = ctx?.projectId ?? projectStore.currentProject?.id ?? 0;
        const scope = ctx?.global ? 'g' : 'p';
        return `${scope}:${pid}:${perm}`;
    };

    const ensure = async (perm: string, ctx?: { projectId?: number; global?: boolean }): Promise<boolean> => {
        const key = makeKey(perm, ctx);
        if (key in resultsCache) return resultsCache[key];
        if (pending.has(key)) return pending.get(key)!;
        const promise = authorizationService
            .authorize({ permission: perm, context: ctx?.projectId ? { projectId: ctx.projectId } : undefined })
            .then((allow) => {
                resultsCache[key] = allow;
                pending.delete(key);
                return allow;
            })
            .catch(() => {
                resultsCache[key] = false;
                pending.delete(key);
                return false;
            });
        pending.set(key, promise);
        return promise;
    };
    const projectStore = useProjectStore();

    // Permission-based checks
    
    /**
     * Checks if user has a global permission (available regardless of project)
     */
    const hasGlobalPermission = (permission: string): boolean => {
        // Trigger async fetch; return cached value if present
        void ensure(permission, { global: true });
        const key = makeKey(permission, { global: true });
        return !!resultsCache[key];
    };

    /**
     * Checks if user has any of the specified global permissions
     */
    const hasAnyGlobalPermission = (permissions: string[]): boolean => {
        permissions.forEach(p => void ensure(p, { global: true }));
        return permissions.some(p => !!resultsCache[makeKey(p, { global: true })]);
    };

    /**
     * Checks if user has all of the specified global permissions
     */
    const hasAllGlobalPermissions = (permissions: string[]): boolean => {
        permissions.forEach(p => void ensure(p, { global: true }));
        return permissions.every(p => !!resultsCache[makeKey(p, { global: true })]);
    };

    /**
     * Checks if user has a permission in the current project (sync; warms cache asynchronously)
     */
    const hasProjectPermission = (permission: string): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        void ensure(permission, { projectId: currentProjectId });
        return !!resultsCache[makeKey(permission, { projectId: currentProjectId })];
    };

    /**
     * Asynchronously checks if user has a permission in the current project.
     * Resolves after the authoritative backend check.
     */
    const hasProjectPermissionAsync = async (permission: string): Promise<boolean> => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        return ensure(permission, { projectId: currentProjectId });
    };

    /**
     * Checks if user has a permission in a specific project
     */
    const hasPermissionInProject = (permission: string, projectId: number): boolean => {
        if (!projectId) return false;
        void ensure(permission, { projectId });
        return !!resultsCache[makeKey(permission, { projectId })];
    };

    /**
     * Prefetch a project-scoped permission to warm the cache.
     */
    const prefetchProjectPermission = (permission: string, projectId?: number) => {
        const pid = projectId ?? projectStore.currentProject?.id;
        if (!pid) return Promise.resolve(false);
        return ensure(permission, { projectId: pid });
    };

    /**
     * Wraps an action with a project-scoped permission check. If denied, logs and does nothing.
     * Usage: const onClick = guardProjectAction(PERMISSIONS.TASK.READ, () => {...})
     */
    const guardProjectAction = (permission: string, action: (...args: any[]) => any, projectId?: number) => {
        return async (...args: any[]) => {
            const pid = projectId ?? projectStore.currentProject?.id;
            if (!pid) return;
            const allowed = await ensure(permission, { projectId: pid });
            if (!allowed) {
                // TODO: soft-fail: optional place to integrate a toast/notification
                return;
            }
            return action(...args);
        };
    };

    /**
     * Checks if user has any of the specified permissions in current project
     */
    const hasAnyProjectPermission = (permissions: string[]): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        permissions.forEach(p => void ensure(p, { projectId: currentProjectId }));
        return permissions.some(p => !!resultsCache[makeKey(p, { projectId: currentProjectId })]);
    };

    /**
     * Checks if user has all of the specified permissions in current project
     */
    const hasAllProjectPermissions = (permissions: string[]): boolean => {
        const currentProjectId = projectStore.currentProject?.id;
        if (!currentProjectId) return false;
        permissions.forEach(p => void ensure(p, { projectId: currentProjectId }));
        return permissions.every(p => !!resultsCache[makeKey(p, { projectId: currentProjectId })]);
    };

    /**
     * Checks if user has a permission anywhere (global or any project)
     */
    const hasAnyPermission = (permission: string): boolean => {
        // Prefer project context when available
        const currentProjectId = projectStore.currentProject?.id;
        if (currentProjectId) {
            return hasProjectPermission(permission);
        }
        return hasGlobalPermission(permission);
    };

    // Project membership proxy: treat project:read as membership capability
    const isCurrentProjectMember = computed(() => hasProjectPermission('project:read'));

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
    const canManageDataSources = computed(() => hasProjectPermission('dataSource:update'));
    const canManageWorkflows = computed(() => hasProjectPermission('workflow:update'));
    const canManageLabelSchemes = computed(() => hasProjectPermission('labelScheme:update'));
    const canCreateAnnotations = computed(() => hasProjectPermission('annotation:create'));
    const canReviewAnnotations = computed(() => hasProjectPermission('annotation:review'));
    const canAssignTasks = computed(() => hasProjectPermission('task:assign'));

    // Manager role check: use authoritative role from backend
    const isManager = (projectId: number): boolean => {
        if (!projectId) return false;
        if (projectId === projectStore.currentProject?.id) {
            if (!projectStore.currentUserMembership) {
                // Load membership in background if missing
                void projectStore.loadCurrentUserMembership(projectId);
                return false;
            }
            return projectStore.isCurrentUserManager;
        }
        // Not the current project: require caller to load membership explicitly via store if needed
        return false;
    };

    const isManagerCurrentProject = computed(() => {
        const pid = projectStore.currentProjectId;
        return pid ? isManager(pid) : false;
    });

    return {
        // Global permission checks
        hasGlobalPermission,
        hasAnyGlobalPermission,
        hasAllGlobalPermissions,

        // Project permission checks
        hasProjectPermission,
        hasProjectPermissionAsync,
        prefetchProjectPermission,
        guardProjectAction,
        hasPermissionInProject,
        hasAnyProjectPermission,
        hasAllProjectPermissions,

        // General permission checks
        hasAnyPermission,
        isCurrentProjectMember,

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
        isManager,
        isManagerCurrentProject,

        authStore,
        projectStore,
    };
}
