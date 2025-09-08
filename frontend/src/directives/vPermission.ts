import type { App, DirectiveBinding } from 'vue';
import { watch } from 'vue';
import { useProjectStore } from '@/stores/projectStore';
import { authorizationService } from '@/services/auth';
import { AppLogger } from '@/core/logger/logger';
import type { PermissionDirectiveBinding } from '@/services/auth/permissions.types';

const logger = AppLogger.createServiceLogger('vPermission');

export const vPermission = {
    mounted(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        const projectStore = useProjectStore();
        
        // Initial check (async)
        void checkPermission(el, binding);
        
        // Watch for project changes to re-evaluate
        
        const unwatchProject = watch(
            () => projectStore.currentProjectId,
            () => {
                void checkPermission(el, binding);
            }
        );
        
        // Store cleanup functions on the element for unmount
        (el as any)._permissionWatchers = [unwatchProject];
    },
    updated(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
        void checkPermission(el, binding);
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

async function checkPermission(el: HTMLElement, binding: DirectiveBinding<PermissionDirectiveBinding>) {
    const projectStore = useProjectStore();

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

    // TODO: Optimistically hide while checking - should show a spinner
    hideElement(el);

    if (global) {
        if (mode === 'all') {
            const { results } = await authorizationService.authorizeBatch(
                permissionsToCheck.map(p => ({ permission: p }))
            );
            hasPermission = results.every(Boolean);
        } else {
            const { results } = await authorizationService.authorizeBatch(
                permissionsToCheck.map(p => ({ permission: p }))
            );
            hasPermission = results.some(Boolean);
        }
    } else {
        const projectId = project || projectStore.currentProject?.id;
        if (!projectId) {
            logger.debug('No project ID available for permission check (project still loading)');
            return; // remain hidden
        }
        if (mode === 'all') {
            const { results } = await authorizationService.authorizeBatch(
                permissionsToCheck.map(p => ({ permission: p, context: { projectId } }))
            );
            hasPermission = results.every(Boolean);
        } else {
            const { results } = await authorizationService.authorizeBatch(
                permissionsToCheck.map(p => ({ permission: p, context: { projectId } }))
            );
            hasPermission = results.some(Boolean);
        }
    }

    if (hasPermission) showElement(el);
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
