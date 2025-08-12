import { useToast } from '@/composables/useToast';
import { useRouter } from 'vue-router';
import { AppLogger } from '@/utils/logger';

const logger = AppLogger.createServiceLogger('PermissionErrorHandler');

export function usePermissionErrorHandler() {
    const { showError, showWarning, showInfo, showSuccess } = useToast();
    const router = useRouter();

    /**
     * Handles permission-related errors with appropriate user feedback
     */
    const handlePermissionError = (
        error: any,
        context: string,
        redirectOnError = true
    ): void => {
        logger.error(`Permission error in ${context}`, error);

        // Check if it's a 403 Forbidden error
        if (error.response?.status === 403) {
            showError(
                'Permission Denied',
                'You do not have permission to perform this action.',
                { duration: 5000 }
            );

            if (redirectOnError) {
                router.push({ name: 'Error', params: { type: 'forbidden' } });
            }
            return;
        }

        // Check if it's a 401 Unauthorized error
        if (error.response?.status === 401) {
            showError(
                'Session Expired',
                'Your session has expired. Please log in again.',
                { duration: 5000 }
            );

            if (redirectOnError) {
                router.push({ name: 'Login' });
            }
            return;
        }

        // Generic permission error
        showError(
            'Permission Error',
            'An error occurred while checking permissions. Please try again.',
            { duration: 5000 }
        );

        logger.warn(`Unhandled permission error in ${context}:`, error);
    };

    /**
     * Shows a user-friendly message when a permission check fails
     */
    const showPermissionDeniedMessage = (
        action: string,
        permission?: string
    ): void => {
        const message = permission 
            ? `You need the '${permission}' permission to ${action}.`
            : `You do not have permission to ${action}.`;

        showWarning(
            'Permission Required',
            message,
            { duration: 4000 }
        );

        logger.info(`Permission denied for action: ${action}${permission ? ` (required: ${permission})` : ''}`);
    };

    /**
     * Shows a message when user is not a member of a project
     */
    const showProjectMembershipError = (projectId: number): void => {
        showError(
            'Access Denied',
            `You are not a member of this project (ID: ${projectId}).`,
            { duration: 5000 }
        );

        logger.warn(`User is not a member of project ${projectId}`);
        router.push({ name: 'Error', params: { type: 'unauthorized' } });
    };

    /**
     * Shows a loading message while permissions are being loaded
     */
    const showPermissionLoadingMessage = (): void => {
        showInfo(
            'Loading',
            'Loading permissions...',
            { duration: 2000 }
        );
    };

    /**
     * Shows a success message when permissions are loaded
     */
    const showPermissionLoadedMessage = (permissionCount: number, projectCount: number): void => {
        showSuccess(
            'Permissions Loaded',
            `Loaded ${permissionCount} permissions across ${projectCount} projects.`,
            { duration: 3000 }
        );
    };

    /**
     * Handles failed permission loading with retry option
     */
    const handlePermissionLoadError = (
        error: any,
        retryCallback?: () => Promise<void>
    ): void => {
        logger.error('Failed to load user permissions', error);

        const message = retryCallback 
            ? 'Failed to load permissions. Please try again or refresh the page.'
            : 'Failed to load permissions. Please refresh the page.';

        showError(
            'Permission Load Failed',
            message,
            { duration: 8000 }
        );
    };

    return {
        handlePermissionError,
        showPermissionDeniedMessage,
        showProjectMembershipError,
        showPermissionLoadingMessage,
        showPermissionLoadedMessage,
        handlePermissionLoadError,
    };
}