<template>
    <div class="project-settings-page">
        <!-- Permission check for project settings (Manager only) -->
        <div v-if="!canAccessProjectSettings" class="access-denied">
            <h2>Access Denied</h2>
            <p>You don't have permission to access project settings. This section is only available to project managers.</p>
        </div>
        
        <div v-else>
            <div class="page-header">
                <h1>Project Settings</h1>
                <p>Manage project configuration, members, and permissions.</p>
            </div>
            
            <div class="settings-sections">
                <MembersSection :project-id="Number(route.params.projectId)" />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {useRoute} from 'vue-router';
import MembersSection from '@/components/project/settings/MembersSection.vue';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const route = useRoute();
const { hasProjectPermission } = usePermissions();

// Permission checks
const canAccessProjectSettings = computed(() => 
    hasProjectPermission(PERMISSIONS.PROJECT_SETTINGS.READ)
);
</script>

<style scoped>
.page-header {
    margin-bottom: 2rem;
    
    h1 {
        font-size: 1.5rem;
        margin-bottom: 0.25rem;
    }
    
    p {
        color: var(--color-gray-600);
        margin-bottom: 1rem;
        max-width: 80ch;
    }
}

.settings-sections {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.access-denied {
    text-align: center;
    padding: 3rem 2rem;
    color: var(--color-gray-600);
    
    h2 {
        color: var(--color-error);
        margin-bottom: 1rem;
    }
    
    p {
        max-width: 60ch;
        margin: 0 auto;
        line-height: 1.5;
    }
}
</style>
