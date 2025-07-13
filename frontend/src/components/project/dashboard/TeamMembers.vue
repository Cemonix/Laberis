<template>
    <div class="team-members">
        <div 
            v-for="member in members" 
            :key="member.id"
            class="member-item"
        >
            <div class="member-avatar">
                <img 
                    v-if="member.avatar" 
                    :src="member.avatar" 
                    :alt="member.name"
                    class="avatar-image"
                />
                <div v-else class="avatar-placeholder">
                    {{ getInitials(member.name) }}
                </div>
            </div>
            <div class="member-info">
                <div class="member-name">{{ member.name }}</div>
                <div class="member-role">{{ member.role }}</div>
                <div class="member-stats">
                    <span class="stat">{{ member.annotationsCount }} annotations</span>
                </div>
            </div>
            <div class="member-status">
                <div 
                    class="status-indicator" 
                    :class="{ 'active': member.isActive }"
                    :title="member.isActive ? 'Active' : 'Inactive'"
                ></div>
            </div>
        </div>
        <div v-if="!members.length" class="no-members">
            No team members assigned
        </div>
    </div>
</template>

<script setup lang="ts">
interface TeamMember {
    id: string;
    name: string;
    role: string;
    avatar?: string;
    annotationsCount: number;
    isActive: boolean;
}

interface Props {
    members: TeamMember[];
}

defineProps<Props>();

const getInitials = (name: string): string => {
    return name
        .split(' ')
        .map(part => part.charAt(0).toUpperCase())
        .slice(0, 2)
        .join('');
};
</script>

<style scoped>
.team-members {
    max-height: 300px;
    overflow-y: auto;
    padding: 0.5rem;
}

.member-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--color-gray-400);

    &:last-child {
        border-bottom: none;
    }
}

.member-avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    overflow: hidden;
    flex-shrink: 0;
}

.avatar-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.avatar-placeholder {
    width: 100%;
    height: 100%;
    background: linear-gradient(135deg, var(--color-primary), var(--color-secondary));
    color: var(--color-white);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 0.875rem;
}

.member-info {
    flex: 1;
    min-width: 0;
}

.member-name {
    font-weight: 600;
    margin-bottom: 0.125rem;
}

.member-role {
    font-size: 0.875rem;
    color: var(--color-gray-600);
    margin-bottom: 0.25rem;
}

.member-stats {
    font-size: 0.875rem;
    color: var(--color-gray-600);
}

.member-status {
    flex-shrink: 0;
}

.status-indicator {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background-color: var(--color-secondary);

    &.active {
        background-color: var(--color-success);
    }
}

.no-members {
    text-align: center;
    color: var(--color-gray-600);
    font-style: italic;
    padding: 1.5rem;
}
</style>
