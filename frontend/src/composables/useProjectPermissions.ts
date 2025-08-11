import { computed, ref, type ComputedRef, type Ref } from "vue";
import { useAuthStore } from "@/stores/authStore";
import { useProjectStore } from "@/stores/projectStore";
import {
    configurationService,
    type PermissionConfiguration,
} from "@/services/api/configuration";
import type { ProjectRole } from "@/types/project/project";
import { AppLogger } from "@/utils/logger";

/**
 * Global permission configuration cache
 */
const permissionConfig: Ref<PermissionConfiguration | null> = ref(null);
const configLoading: Ref<boolean> = ref(false);
const configError: Ref<string | null> = ref(null);

const logger = AppLogger.createServiceLogger('ProjectPermissions');

/**
 * Composable for handling project-level permission checks in Vue components.
 * Loads permission rules from the backend API and provides reactive permission checking.
 */
export function useProjectPermissions() {
    const authStore = useAuthStore();
    const projectStore = useProjectStore();

    /**
     * Loads permission configuration from the backend API
     */
    const loadPermissionConfiguration = async (): Promise<void> => {
        if (permissionConfig.value || configLoading.value) {
            return; // Already loaded or currently loading
        }

        configLoading.value = true;
        configError.value = null;

        try {
            permissionConfig.value =
                await configurationService.getPermissionConfiguration();
            logger.info("Permission configuration loaded successfully");
        } catch (error) {
            configError.value = "Failed to load permission configuration";
            logger.error("Failed to load permission configuration", error);
            throw error;
        } finally {
            configLoading.value = false;
        }
    };

    /**
     * Gets the current user's role in the active project
     */
    const currentUserRole: ComputedRef<ProjectRole | null> = computed(() => {
        if (
            !authStore.isAuthenticated ||
            !authStore.user ||
            !projectStore.currentProject
        ) {
            return null;
        }

        // Find the current user's membership in the active project by email
        const currentUserMembership = projectStore.teamMembers.find(
            (member) => member.email === authStore.user?.email
        );

        return currentUserMembership?.role || null;
    });

    /**
     * Gets all permissions for the current user's role
     */
    const currentUserPermissions: ComputedRef<Set<string>> = computed(() => {
        const role = currentUserRole.value;
        if (!role) {
            return new Set();
        }

        // Auto-load configuration if not loaded
        if (!permissionConfig.value && !configLoading.value) {
            loadPermissionConfiguration().catch((error) => 
                logger.error("Failed to auto-load permission configuration", error)
            );
            return new Set();
        }

        if (!permissionConfig.value) {
            return new Set();
        }

        const permissions = permissionConfig.value.rolePermissions[role] || [];
        return new Set(permissions);
    });

    /**
     * Checks if the current user has a specific permission in the active project
     */
    const hasPermission = (permission: string): boolean => {
        return currentUserPermissions.value.has(permission);
    };

    /**
     * Checks if the current user has any of the specified permissions
     */
    const hasAnyPermission = (permissions: string[]): boolean => {
        const userPermissions = currentUserPermissions.value;
        return permissions.some((permission) =>
            userPermissions.has(permission)
        );
    };

    /**
     * Checks if the current user has all of the specified permissions
     */
    const hasAllPermissions = (permissions: string[]): boolean => {
        const userPermissions = currentUserPermissions.value;
        return permissions.every((permission) =>
            userPermissions.has(permission)
        );
    };

    /**
     * Reactive computed property for checking permissions
     * Usage: const canManageMembers = canDo('members:manage')
     */
    const canDo = (permission: string): ComputedRef<boolean> => {
        return computed(() => hasPermission(permission));
    };

    /**
     * Reactive computed property for checking multiple permissions (any)
     */
    const canDoAny = (permissions: string[]): ComputedRef<boolean> => {
        return computed(() => hasAnyPermission(permissions));
    };

    /**
     * Reactive computed property for checking multiple permissions (all)
     */
    const canDoAll = (permissions: string[]): ComputedRef<boolean> => {
        return computed(() => hasAllPermissions(permissions));
    };

    /**
     * Role-based computed properties for common use cases
     */
    const isViewer: ComputedRef<boolean> = computed(
        () => currentUserRole.value === "VIEWER"
    );
    const isAnnotator: ComputedRef<boolean> = computed(
        () => currentUserRole.value === "ANNOTATOR"
    );
    const isReviewer: ComputedRef<boolean> = computed(
        () => currentUserRole.value === "REVIEWER"
    );
    const isManager: ComputedRef<boolean> = computed(
        () => currentUserRole.value === "MANAGER"
    );

    /**
     * Hierarchical role checks (or higher)
     */
    const isAnnotatorOrHigher: ComputedRef<boolean> = computed(() => {
        const role = currentUserRole.value;
        return (
            role === "ANNOTATOR" || role === "REVIEWER" || role === "MANAGER"
        );
    });

    const isReviewerOrHigher: ComputedRef<boolean> = computed(() => {
        const role = currentUserRole.value;
        return role === "REVIEWER" || role === "MANAGER";
    });

    const isManagerOrHigher: ComputedRef<boolean> = computed(() => {
        const role = currentUserRole.value;
        return role === "MANAGER";
    });

    /**
     * Check if user is a project member (any role)
     */
    const isProjectMember: ComputedRef<boolean> = computed(() => {
        return currentUserRole.value !== null;
    });

    /**
     * Configuration loading state
     */
    const isConfigLoading: ComputedRef<boolean> = computed(
        () => configLoading.value
    );
    const hasConfigError: ComputedRef<boolean> = computed(
        () => configError.value !== null
    );
    const configErrorMessage: ComputedRef<string | null> = computed(
        () => configError.value
    );

    /**
     * Gets the flat permission map for easy access
     * Returns all permissions as a flat object: { 'members:manage': 'members:manage', ... }
     */
    const getPermissionMap = (): Record<string, string> => {
        // Auto-load configuration if not loaded
        if (!permissionConfig.value && !configLoading.value) {
            loadPermissionConfiguration().catch((error) => 
                logger.error("Failed to auto-load permission configuration for map", error)
            );
            return {};
        }

        if (!permissionConfig.value) return {};

        const flatMap: Record<string, string> = {};
        Object.values(permissionConfig.value.permissions).forEach(
            (categoryPermissions) => {
                Object.entries(categoryPermissions).forEach(
                    ([, permissionValue]) => {
                        flatMap[permissionValue] = permissionValue;
                    }
                );
            }
        );
        return flatMap;
    };

    return {
        // Configuration management
        loadPermissionConfiguration,
        isConfigLoading,
        hasConfigError,
        configErrorMessage,
        getPermissionMap,

        // Current user role
        currentUserRole,
        currentUserPermissions,

        // Permission checking functions
        hasPermission,
        hasAnyPermission,
        hasAllPermissions,

        // Reactive permission checkers
        canDo,
        canDoAny,
        canDoAll,

        // Role-based checks
        isViewer,
        isAnnotator,
        isReviewer,
        isManager,

        // Hierarchical role checks
        isAnnotatorOrHigher,
        isReviewerOrHigher,
        isManagerOrHigher,
        isProjectMember,
    };
}
