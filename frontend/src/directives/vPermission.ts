import type { App, DirectiveBinding } from 'vue';
import { watch } from 'vue';
import { usePermissionStore } from '@/stores/permissionStore';
import { useProjectStore } from '@/stores/projectStore';
import { AppLogger } from '@/utils/logger';
import type { PermissionDirectiveBinding } from '@/types/permissions';

const logger = AppLogger.createServiceLogger('vPermission');

export const vPermission = {
    mounted(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        const permissionStore = usePermissionStore();
        const projectStore = useProjectStore();
        
        // Initial check
        checkPermission(el, binding);
        
        // Watch for changes in permission store initialization and project ID
        const unwatchPermissions = watch(
            () => permissionStore.isInitialized,
            () => {
                checkPermission(el, binding);
            }
        );
        
        const unwatchProject = watch(
            () => projectStore.currentProjectId,
            () => {
                checkPermission(el, binding);
            }
        );
        
        // Store cleanup functions on the element for unmount
        (el as any)._permissionWatchers = [unwatchPermissions, unwatchProject];
    },
    updated(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        checkPermission(el, binding);
    },
    unmounted(el: HTMLElement) {
        // Cleanup watchers when directive is unmounted
        const watchers = (el as any)._permissionWatchers;
        if (watchers) {
            watchers.forEach((unwatch: () => void) => unwatch());
            delete (el as any)._permissionWatchers;
        }
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
            logger.debug('No project ID available for permission check (project still loading)');
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