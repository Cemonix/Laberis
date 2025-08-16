<template>
    <div class="recent-activities-widget">
        <div v-if="!data?.length" class="no-data">
            <i class="icon-info"></i>
            <span>No recent activities</span>
        </div>

        <div v-else class="activities-content">
            <!-- Activity Feed -->
            <div class="activity-feed">
                <div
                    v-for="(activity, index) in data"
                    :key="index"
                    class="activity-item"
                >
                    <div
                        class="activity-icon"
                        :class="getActivityClass(activity.activityType)"
                    >
                        <i :class="getActivityIcon(activity.activityType)"></i>
                    </div>

                    <div class="activity-content">
                        <div class="activity-description">
                            {{ activity.description }}
                        </div>
                        <div class="activity-meta">
                            <span class="activity-user">{{
                                activity.userName
                            }}</span>
                            <span
                                v-if="activity.assetName"
                                class="activity-asset"
                            >
                                <i class="icon-file"></i>
                                {{ activity.assetName }}
                            </span>
                            <span
                                v-if="activity.workflowName"
                                class="activity-workflow"
                            >
                                <i class="icon-flow"></i>
                                {{ activity.workflowName }}
                            </span>
                            <span
                                v-if="activity.stageName"
                                class="activity-stage"
                            >
                                <i class="icon-layers"></i>
                                {{ activity.stageName }}
                            </span>
                        </div>
                    </div>

                    <div class="activity-time">
                        <span class="time-text">{{
                            formatTimeAgo(activity.timestamp)
                        }}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { RecentActivityDto } from "@/types/dashboard/dashboard";

interface Props {
    data?: RecentActivityDto[];
    loading?: boolean;
    error?: string | null;
}

defineProps<Props>();

// Methods
const getActivityClass = (activityType: string): string => {
    const classMap: Record<string, string> = {
        task_completed: "completed",
        task_created: "created",
        task_assigned: "assigned",
        annotation_created: "annotation",
        annotation_updated: "annotation",
        annotation_deleted: "annotation",
        review_completed: "review",
        review_rejected: "rejected",
        asset_uploaded: "upload",
        workflow_stage_completed: "completed",
        user_joined: "user",
        project_updated: "project",
    };
    return classMap[activityType] || "default";
};

const getActivityIcon = (activityType: string): string => {
    const iconMap: Record<string, string> = {
        task_completed: "icon-check-circle",
        task_created: "icon-plus-circle",
        task_assigned: "icon-user-check",
        annotation_created: "icon-edit",
        annotation_updated: "icon-edit-2",
        annotation_deleted: "icon-trash",
        review_completed: "icon-eye-check",
        review_rejected: "icon-x-circle",
        asset_uploaded: "icon-upload",
        workflow_stage_completed: "icon-check-square",
        user_joined: "icon-user-plus",
        project_updated: "icon-settings",
    };
    return iconMap[activityType] || "icon-activity";
};

const formatTimeAgo = (timestamp: Date): string => {
    const now = new Date();
    const diffMs = now.getTime() - new Date(timestamp).getTime();
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffSecs < 30) return "just now";
    if (diffSecs < 60) return `${diffSecs}s ago`;
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return new Date(timestamp).toLocaleDateString();
};
</script>

<style scoped>
.recent-activities-widget {
    height: auto;
    display: flex;
    flex-direction: column;
}

.no-data {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: auto;
    min-height: 200px;
    color: var(--color-gray-600);
    gap: 0.5rem;
}

.activities-content {
    height: auto;
    overflow: visible;
}

.activity-feed {
    height: auto;
    overflow: visible;
    padding-right: 0.25rem;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.activity-item {
    display: flex;
    gap: 0.75rem;
    padding: 0.75rem;
    background: var(--color-gray-100);
    border-radius: 8px;
    transition: all 0.2s ease;
}

.activity-item:hover {
    background: var(--color-surface-container);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* Activity Icon */
.activity-icon {
    flex-shrink: 0;
    width: 32px;
    height: 32px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.875rem;
    color: white;
}

.activity-icon.completed {
    background: var(--color-success);
}

.activity-icon.created {
    background: var(--color-primary);
}

.activity-icon.assigned {
    background: var(--color-secondary);
}

.activity-icon.annotation {
    background: var(--color-warning);
}

.activity-icon.review {
    background: var(--color-info);
}

.activity-icon.rejected {
    background: var(--color-error);
}

.activity-icon.upload {
    background: var(--color-primary-light);
}

.activity-icon.user {
    background: var(--color-secondary-light);
}

.activity-icon.project {
    background: var(--color-gray-300);
}

.activity-icon.default {
    background: var(--color-outline-variant);
}

/* Activity Content */
.activity-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.activity-description {
    font-size: 0.75rem;
    color: var(--color-gray-800);
    line-height: 1.4;
    overflow: hidden;
    display: -webkit-box;
    line-clamp: 2;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
}

.activity-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    align-items: center;
    font-size: 0.625rem;
    color: var(--color-gray-600);
}

.activity-user {
    font-weight: 500;
    color: var(--color-primary);
}

.activity-asset,
.activity-workflow,
.activity-stage {
    display: flex;
    align-items: center;
    gap: 0.125rem;
    background: var(--color-white);
    padding: 0.125rem 0.25rem;
    border-radius: 3px;
}

.activity-asset {
    color: var(--color-warning);
}

.activity-workflow {
    color: var(--color-info);
}

.activity-stage {
    color: var(--color-secondary);
}

/* Activity Time */
.activity-time {
    flex-shrink: 0;
    display: flex;
    align-items: flex-start;
    padding-top: 0.125rem;
}

.time-text {
    font-size: 0.575rem;
    color: var(--color-gray-600);
    white-space: nowrap;
}

/* Scrollbar styling */
.activity-feed::-webkit-scrollbar {
    width: 4px;
}

.activity-feed::-webkit-scrollbar-track {
    background: var(--color-gray-100);
    border-radius: 2px;
}

.activity-feed::-webkit-scrollbar-thumb {
    background: var(--color-gray-300);
    border-radius: 2px;
}

.activity-feed::-webkit-scrollbar-thumb:hover {
    background: var(--color-outline-variant);
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .activity-item {
        padding: 0.5rem;
        gap: 0.5rem;
    }

    .activity-icon {
        width: 28px;
        height: 28px;
        font-size: 0.75rem;
    }

    .activity-description {
        font-size: 0.6875rem;
    }

    .activity-meta {
        font-size: 0.5625rem;
        gap: 0.25rem;
    }

    .time-text {
        font-size: 0.5rem;
    }

    .activity-asset,
    .activity-workflow,
    .activity-stage {
        padding: 1px 3px;
    }
}

/* Animation for new activities */
@keyframes slideInFade {
    0% {
        opacity: 0;
        transform: translateY(-10px);
    }
    100% {
        opacity: 1;
        transform: translateY(0);
    }
}

.activity-item:first-child {
    animation: slideInFade 0.3s ease-out;
}
</style>
