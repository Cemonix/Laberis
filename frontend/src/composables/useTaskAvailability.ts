import { computed } from "vue";
import { useWorkspaceStore } from "@/stores/workspaceStore";

/**
 * Composable for accessing task availability information and formatted counters
 * Uses the enhanced navigation context to provide user-friendly task status information
 */
export function useTaskAvailability() {
    const workspaceStore = useWorkspaceStore();

    /**
     * Current navigation context with availability information
     */
    const navigationContext = computed(() => workspaceStore.navigationContext);

    /**
     * Formatted task counter for "X of Y tasks" display
     */
    const taskPositionText = computed(() => {
        const context = navigationContext.value;
        if (!context) return "Loading...";

        return `${context.currentPosition} of ${context.totalTasks}`;
    });

    /**
     * Detailed task availability summary
     */
    const availabilitySummary = computed(() => {
        const context = navigationContext.value;
        if (!context) return "Loading task information...";

        const parts = [];

        if (context.availableTasks > 0) {
            parts.push(`${context.availableTasks} available`);
        }

        if (context.completedTasks > 0) {
            parts.push(`${context.completedTasks} completed`);
        }

        if (context.assignedTasks > 0) {
            parts.push(`${context.assignedTasks} assigned to others`);
        }

        return parts.length > 0 ? parts.join(", ") : "No tasks available";
    });

    /**
     * Status indicator for current task state
     */
    const currentTaskStatus = computed(() => {
        const context = navigationContext.value;
        if (!context) return { text: "Loading...", color: "gray" };

        if (context.availableTasks === 0) {
            return {
                text: "All tasks completed or assigned to others",
                color: "orange",
            };
        }

        return {
            text: `${context.availableTasks} tasks available to work on`,
            color: "green",
        };
    });

    /**
     * Progress percentage based on completed tasks
     */
    const completionPercentage = computed(() => {
        const context = navigationContext.value;
        if (!context || context.totalTasks === 0) return 0;

        return Math.round((context.completedTasks / context.totalTasks) * 100);
    });

    /**
     * Whether there are tasks available for the user to work on
     */
    const hasAvailableTasks = computed(() => {
        const context = navigationContext.value;
        return context ? context.availableTasks > 0 : false;
    });

    /**
     * Whether all tasks are either completed or assigned to others
     */
    const allTasksUnavailable = computed(() => {
        const context = navigationContext.value;
        if (!context) return false;

        return (
            context.availableTasks === 0 &&
            (context.completedTasks > 0 || context.assignedTasks > 0)
        );
    });

    return {
        // Raw data
        navigationContext,

        // Formatted strings
        taskPositionText,
        availabilitySummary,
        currentTaskStatus,

        // Computed values
        completionPercentage,
        hasAvailableTasks,
        allTasksUnavailable,
    };
}
