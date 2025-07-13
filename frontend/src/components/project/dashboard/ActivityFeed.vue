<template>
    <div class="activity-feed">
        <div 
            v-for="(activity, index) in activities" 
            :key="index"
            class="activity-item"
        >
            <div class="activity-icon">
                <font-awesome-icon :icon="getActivityIcon(activity.type)" />
            </div>
            <div class="activity-content">
                <div class="activity-message">{{ activity.message }}</div>
                <div class="activity-time">{{ formatTime(activity.timestamp) }}</div>
            </div>
        </div>
        <div v-if="!activities.length" class="no-activities">
            No recent activities
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faCheckCircle, faInfoCircle, faTag, faTrophy, faUpload, faUserPlus} from '@fortawesome/free-solid-svg-icons';

interface Activity {
    type: 'annotation' | 'review' | 'assignment' | 'upload' | 'completion';
    message: string;
    timestamp: Date;
    user?: string;
}

interface Props {
    activities: Activity[];
    maxItems?: number;
}

const props = withDefaults(defineProps<Props>(), {
    maxItems: 10
});

const activities = computed(() => 
    props.activities
        .slice(0, props.maxItems)
        .sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime())
);

const getActivityIcon = (type: Activity['type']) => {
    switch (type) {
        case 'annotation':
            return faTag;
        case 'review':
            return faCheckCircle;
        case 'assignment':
            return faUserPlus;
        case 'upload':
            return faUpload;
        case 'completion':
            return faTrophy;
        default:
            return faInfoCircle;
    }
};

const formatTime = (timestamp: Date) => {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
};
</script>

<style scoped>
.activity-feed {
    max-height: 300px;
    overflow-y: auto;
    padding: 0.5rem;
}

.activity-item {
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--color-gray-400);

    &:last-child {
        border-bottom: none;
    }
}

.activity-icon {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background-color: var(--color-primary);
    color: var(--color-white);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    font-size: 1rem;
}

.activity-content {
    flex: 1;
    min-width: 0;
}

.activity-message {
    font-weight: 500;
    margin-bottom: 0.5rem;
    line-height: 1.4;
}

.activity-time {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.no-activities {
    text-align: center;
    color: var(--color-gray-600);
    font-style: italic;
    padding: 1.5rem;
}
</style>
