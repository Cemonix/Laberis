import type { App, DirectiveBinding } from 'vue';
import { usePermissionStore } from '@/stores/permissionStore';
import { useProjectStore } from '@/stores/projectStore';
import { AppLogger } from '@/utils/logger';
import type { PermissionDirectiveBinding } from '@/types/permissions';

const logger = AppLogger.createServiceLogger('vPermission');

export const vPermission = {
    mounted(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        checkPermission(el, binding);
    },
    updated(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        checkPermission(el, binding);
    }
};

function checkPermission(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
    const permissionStore = usePermissionStore();
    const projectStore = useProjectStore();
    
    // If permission store is not initialized, hide element by default
    if (!permissionStore.isInitialized) {
        hideElement(el);
        return;
    }

    const value = binding.value || {};
    const {
        permission,
        permissions = [],
        project,
        mode = 'all',
        global = false
    } = value;

    // Build the list of permissions to check
    const permissionsToCheck: string[] = [];
    if (permission) {
        permissionsToCheck.push(permission);
    }
    if (permissions.length > 0) {
        permissionsToCheck.push(...permissions);
    }

    // If no permissions specified, show element
    if (permissionsToCheck.length === 0) {
        showElement(el);
        return;
    }

    let hasPermission = false;

    if (global) {
        // Check global permissions
        if (mode === 'all') {
            hasPermission = permissionStore.hasAllGlobalPermissions(permissionsToCheck);
        } else {
            hasPermission = permissionStore.hasAnyGlobalPermission(permissionsToCheck);
        }
    } else {
        // Check project permissions
        const projectId = project || projectStore.currentProject?.id;
        
        if (!projectId) {
            logger.warn('No project ID available for permission check');
            hideElement(el);
            return;
        }

        if (mode === 'all') {
            hasPermission = permissionStore.hasAllProjectPermissions(permissionsToCheck, projectId);
        } else {
            hasPermission = permissionStore.hasAnyProjectPermission(permissionsToCheck, projectId);
        }
    }

    if (hasPermission) {
        showElement(el);
    } else {
        hideElement(el);
    }
}

function showElement(el: HTMLElement) {
    el.style.display = '';
    el.removeAttribute('aria-hidden');
}

function hideElement(el: HTMLElement) {
    el.style.display = 'none';
    el.setAttribute('aria-hidden', 'true');
}

export function registerPermissionDirective(app: App) {
    app.directive('permission', vPermission);
}

export default vPermission;