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
import { useRoute } from 'vue-router';
import MembersSection from '@/components/project/settings/MembersSection.vue';

const route = useRoute();
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.page-header {
    margin-bottom: vars.$margin-xlarge;
    
    h1 {
        font-size: vars.$font-size-xlarge;
        margin-bottom: vars.$padding-xsmall;
    }
    
    p {
        color: vars.$theme-text-light;
        margin-bottom: vars.$margin-medium;
        max-width: 80ch;
    }
}

.settings-sections {
    display: flex;
    flex-direction: column;
    gap: vars.$gap-large;
}
</style>
