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

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.team-members {
    max-height: 300px;
    overflow-y: auto;
    padding: vars.$padding-small;
}

.member-item {
    display: flex;
    align-items: center;
    gap: vars.$gap-medium;
    padding: vars.$padding-small 0;
    border-bottom: 1px solid vars.$theme-border;

    &:last-child {
        border-bottom: none;
    }
}

.member-avatar {
    width: 40px;
    height: 40px;
    border-radius: vars.$border-radius-circle;
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
    background: linear-gradient(135deg, vars.$theme-primary, vars.$color-secondary);
    color: vars.$theme-surface;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: vars.$font-weight-large;
    font-size: vars.$font-size-small;
}

.member-info {
    flex: 1;
    min-width: 0;
}

.member-name {
    font-weight: vars.$font-weight-large;
    margin-bottom: vars.$margin-xxsmall;
}

.member-role {
    font-size: vars.$font-size-small;
    color: vars.$theme-text-light;
    margin-bottom: vars.$margin-xsmall;
}

.member-stats {
    font-size: vars.$font-size-small;
    color: vars.$theme-text-light;
}

.member-status {
    flex-shrink: 0;
}

.status-indicator {
    width: 12px;
    height: 12px;
    border-radius: vars.$border-radius-circle;
    background-color: vars.$color-secondary;

    &.active {
        background-color: vars.$color-success;
    }
}

.no-members {
    text-align: center;
    color: vars.$theme-text-light;
    font-style: italic;
    padding: vars.$padding-large;
}
</style>
