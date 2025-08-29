import { defineStore } from "pinia";
import { permissionService } from "@/services/auth";
import type { UserPermissionContext } from "@/services/auth/permissions.types";
import { AppLogger } from "@/core/logger/logger";

const logger = AppLogger.createStoreLogger('PermissionStore');

export const usePermissionStore = defineStore("permissions", {
    state: () => ({
        userContext: null as UserPermissionContext | null,
        isLoading: false,
        isInitialized: false,
        error: null as string | null,
    }),
    
    getters: {
        /**
         * Gets all permissions for the current user (global + all project permissions)
         */
        allPermissions(state): Set<string> {
            if (!state.userContext) return new Set();
            return new Set(state.userContext.permissions);
        },

        /**
         * Gets global permissions (available regardless of project context)
         */
        globalPermissions(state): Set<string> {
            if (!state.userContext) return new Set();
            return new Set(state.userContext.globalPermissions);
        },

        /**
         * Gets permissions for a specific project
         */
        getProjectPermissions: (state) => (projectId: number): Set<string> => {
            if (!state.userContext || !projectId) return new Set();
            const projectPermissions = state.userContext.projectPermissions[projectId];
            return new Set(projectPermissions || []);
        },

        /**
         * Checks if user has a specific permission globally
         */
        hasGlobalPermission: (state) => (permission: string): boolean => {
            return state.userContext?.globalPermissions.includes(permission) || false;
        },

        /**
         * Checks if user has a specific permission in any project
         */
        hasAnyPermission: (state) => (permission: string): boolean => {
            return state.userContext?.permissions.includes(permission) || false;
        },

        /**
         * Checks if user has a specific permission in a specific project
         */
        hasProjectPermission: (state) => (permission: string, projectId: number): boolean => {
            if (!state.userContext || !projectId) return false;
            const projectPermissions = state.userContext.projectPermissions[projectId];
            return projectPermissions?.includes(permission) || false;
        },

        /**
         * Checks if user has any of the specified permissions globally
         */
        hasAnyGlobalPermission: (state) => (permissions: string[]): boolean => {
            if (!state.userContext) return false;
            return permissions.some(permission => 
                state.userContext!.globalPermissions.includes(permission)
            );
        },

        /**
         * Checks if user has all of the specified permissions globally
         */
        hasAllGlobalPermissions: (state) => (permissions: string[]): boolean => {
            if (!state.userContext) return false;
            return permissions.every(permission => 
                state.userContext!.globalPermissions.includes(permission)
            );
        },

        /**
         * Checks if user has any of the specified permissions in a project
         */
        hasAnyProjectPermission: (state) => (permissions: string[], projectId: number): boolean => {
            if (!state.userContext || !projectId) return false;
            const projectPermissions = state.userContext.projectPermissions[projectId] || [];
            return permissions.some(permission => projectPermissions.includes(permission));
        },

        /**
         * Checks if user has all of the specified permissions in a project
         */
        hasAllProjectPermissions: (state) => (permissions: string[], projectId: number): boolean => {
            if (!state.userContext || !projectId) return false;
            const projectPermissions = state.userContext.projectPermissions[projectId] || [];
            return permissions.every(permission => projectPermissions.includes(permission));
        },

        /**
         * Gets all project IDs where user has membership
         */
        projectIds(state): number[] {
            if (!state.userContext) return [];
            return Object.keys(state.userContext.projectPermissions).map(id => parseInt(id));
        },

        /**
         * Checks if user is a member of a specific project
         */
        isProjectMember: (state) => (projectId: number): boolean => {
            if (!state.userContext || !projectId) return false;
            return Object.prototype.hasOwnProperty.call(state.userContext.projectPermissions, projectId);
        },
    },

    actions: {
        /**
         * Loads the full user permission context from the backend
         */
        async loadUserPermissions(): Promise<void> {
            if (this.isLoading) return;
            
            this.isLoading = true;
            this.error = null;
            
            try {
                logger.info("Loading user permission context");
                this.userContext = await permissionService.getUserPermissionContext();
                this.isInitialized = true;
                
                const permissionCount = this.userContext.permissions.length;
                const projectCount = this.projectIds.length;
                
                logger.info(`Loaded ${permissionCount} total permissions for ${projectCount} projects`);
            } catch (error) {
                this.error = "Failed to load user permissions";
                logger.error("Failed to load user permission context", error);
                throw error;
            } finally {
                this.isLoading = false;
            }
        },

        /**
         * Gets page-specific permissions (hybrid approach)
         */
        async getPagePermissions(page: string, projectId?: number): Promise<string[]> {
            try {
                logger.debug(`Getting page permissions for: ${page}${projectId ? ` (project: ${projectId})` : ''}`);
                return await permissionService.getPagePermissions(page, projectId);
            } catch (error) {
                logger.error(`Failed to get page permissions for ${page}`, error);
                return [];
            }
        },

        /**
         * Clears all permission data (for logout)
         */
        clearPermissions(): void {
            this.userContext = null;
            this.isInitialized = false;
            this.error = null;
            logger.info("Cleared user permissions");
        },

        /**
         * Refreshes permission data from the backend
         */
        async refreshPermissions(): Promise<void> {
            this.isInitialized = false;
            await this.loadUserPermissions();
        },

        /**
         * Admin-only: Reload permission configuration from file
         */
        async reloadConfiguration(): Promise<void> {
            try {
                await permissionService.reloadPermissionConfiguration();
                // Refresh user permissions after configuration reload
                await this.refreshPermissions();
                logger.info("Permission configuration reloaded successfully");
            } catch (error) {
                logger.error("Failed to reload permission configuration", error);
                throw error;
            }
        },
    },
});