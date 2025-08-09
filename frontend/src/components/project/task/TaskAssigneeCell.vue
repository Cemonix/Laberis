<template>
    <div v-if="assignedTo" class="assigned-user">
        <font-awesome-icon :icon="faUser" class="user-icon" />
        {{ assignedTo }}
    </div>
    <div v-else class="unassigned">
        <font-awesome-icon :icon="faUserSlash" class="unassigned-icon" />
        Unassigned
    </div>
</template>

<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faUser, faUserSlash } from '@fortawesome/free-solid-svg-icons';

interface Props {
    assignedTo?: string;
}

defineProps<Props>();
</script>

<style lang="scss" scoped>
.assigned-user {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    
    .user-icon {
        color: var(--color-success);
    }
}

.unassigned {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    color: var(--color-gray-600);
    font-style: italic;
    
    .unassigned-icon {
        color: var(--color-warning);
    }
}
</style>