<template>
    <div class="project-settings-page">
        <div class="page-header">
            <h1>Project Settings</h1>
            <p>Manage project configuration, members, and permissions.</p>
        </div>
        
        <div class="settings-sections">
            <MembersSection :project-id="Number(route.params.projectId)" />
        </div>
    </div>
</template>

<script setup lang="ts">
import {useRoute} from 'vue-router';
import MembersSection from '@/components/project/settings/MembersSection.vue';

const route = useRoute();
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
</style>
