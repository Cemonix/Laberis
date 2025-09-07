import { useRouter } from "vue-router";
import { useToast } from "@/composables/useToast";

/**
 * Composable for handling task loading with automatic redirect when no tasks available
 * Usage example for components that need to load tasks and handle "no tasks available" gracefully
 */
export function useTaskRedirect() {
    const router = useRouter();
    const toast = useToast();

    /**
     * Handle task operation result with automatic redirect and toast for "no tasks available"
     * @param result - The task operation result from TaskManager
     * @param fallbackRoute - Route to redirect to when no tasks available (default: task view)
     */
    const handleTaskResult = async <T>(
        result: import("@/core/workspace/task/taskManager").TaskOperationResult<T>,
        fallbackRoute: string = "/tasks"
    ) => {
        if (!result.success) {
            // Check if we should redirect to task view with toast
            if (result.shouldRedirectToTaskView && result.toastMessage) {
                // Show informative toast message
                toast.showInfo("No Tasks Available", result.toastMessage);

                // Redirect back to task view
                await router.push(fallbackRoute);
                return { redirected: true };
            } else {
                // Regular error - let the component handle it
                return { redirected: false, error: result.error };
            }
        }

        return { redirected: false, data: result.data };
    };

    return {
        handleTaskResult,
    };
}
