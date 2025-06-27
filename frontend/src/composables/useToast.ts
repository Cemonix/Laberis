import type { Toast, ToastOptions, ToastType } from '@/types/toast';
import { ref, readonly } from 'vue';

const toasts = ref<Toast[]>([]);
let toastIdCounter = 0;

export function useToast() {
    const generateId = (): string => {
        return `toast-${++toastIdCounter}-${Date.now()}`;
    };

    const showToast = (
        title: string,
        message: string,
        type: ToastType = 'info',
        options: ToastOptions = {}
    ): string => {
        const {
            duration = type === 'error' ? 6000 : 4000,
            persistent = false
        } = options;

        const toast: Toast = {
            id: generateId(),
            title,
            message,
            type,
            duration,
            persistent,
            createdAt: Date.now()
        };

        toasts.value.push(toast);

        // Auto-remove toast after duration (unless persistent)
        if (!persistent && duration > 0) {
            setTimeout(() => {
                removeToast(toast.id);
            }, duration);
        }

        return toast.id;
    };

    const removeToast = (id: string): void => {
        const index = toasts.value.findIndex(toast => toast.id === id);
        if (index > -1) {
            toasts.value.splice(index, 1);
        }
    };

    const clearAllToasts = (): void => {
        toasts.value.splice(0);
    };

    // Convenience methods for different toast types
    const showSuccess = (title: string, message: string, options?: ToastOptions): string => {
        return showToast(title, message, 'success', options);
    };

    const showError = (title: string, message: string, options?: ToastOptions): string => {
        return showToast(title, message, 'error', options);
    };

    const showWarning = (title: string, message: string, options?: ToastOptions): string => {
        return showToast(title, message, 'warning', options);
    };

    const showInfo = (title: string, message: string, options?: ToastOptions): string => {
        return showToast(title, message, 'info', options);
    };

    // Utility methods for common scenarios
    const showApiError = (error: any, customMessage?: string): string => {
        const title = 'API Error';
        const message = customMessage || error?.message || 'An unexpected error occurred';
        return showError(title, message);
    };

    const showCreateSuccess = (itemType: string, itemName: string = 'Item'): string => {
        return showSuccess('Created', `${itemType} "${itemName}" has been created successfully.`);
    };

    const showSaveSuccess = (itemType: string, itemName: string = 'Data'): string => {
        return showSuccess('Saved', `${itemType} "${itemName}" has been saved successfully.`);
    };

    const showDeleteSuccess = (itemType: string, itemName: string = 'Item'): string => {
        return showSuccess('Deleted', `${itemType} "${itemName}" has been deleted successfully.`);
    };

    const showValidationError = (message: string = 'Please check your input and try again.'): string => {
        return showWarning('Validation Error', message);
    };

    return {
        toasts: readonly(toasts),
        showToast,
        showSuccess,
        showError,
        showWarning,
        showInfo,
        showApiError,
        showCreateSuccess,
        showSaveSuccess,
        showDeleteSuccess,
        showValidationError,
        removeToast,
        clearAllToasts
    };
}
