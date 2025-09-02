<template>
    <div class="project-settings-page">
        <!-- Permission check for project settings (Manager only) -->
        <div v-if="!canAccessProjectSettings" class="access-denied">
            <font-awesome-icon :icon="faLock" class="access-denied-icon" />
            <h2>Access Denied</h2>
            <p>You don't have permission to access project settings. This section is only available to project managers.</p>
        </div>
        
        <div v-else class="settings-container">
            <div class="page-header">
                <h1>Project Settings</h1>
                <p>Manage project configuration, members, and permissions.</p>
            </div>
            
            <!-- Settings Navigation -->
            <div class="settings-nav">
                <button 
                    v-for="section in settingSections" 
                    :key="section.id"
                    @click="activeSection = section.id"
                    :class="['nav-btn', { 'active': activeSection === section.id }]"
                >
                    <font-awesome-icon :icon="section.icon" />
                    {{ section.name }}
                </button>
            </div>
            
            <!-- Settings Content -->
            <div class="settings-content">
                <!-- Project Information Section -->
                <ProjectInfoSection 
                    v-if="activeSection === 'info'"
                    :project-id="projectId"
                />
                
                <!-- Invitations Section -->
                <InvitationsSection 
                    v-if="activeSection === 'invitations'"
                    :project-id="projectId"
                />
                
                <!-- Members Section -->
                <MembersSection 
                    v-if="activeSection === 'members'"
                    :project-id="projectId"
                />
                
                <!-- Danger Zone Section -->
                <DangerZoneSection 
                    v-if="activeSection === 'danger'"
                    :project-id="projectId"
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import {useRoute} from 'vue-router';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {
    faLock,
    faInfoCircle,
    faEnvelope,
    faUsers,
    faExclamationTriangle
} from '@fortawesome/free-solid-svg-icons';
import ProjectInfoSection from '@/components/project/settings/ProjectInfoSection.vue';
import InvitationsSection from '@/components/project/settings/InvitationsSection.vue';
import MembersSection from '@/components/project/settings/MembersSection.vue';
import DangerZoneSection from '@/components/project/settings/DangerZoneSection.vue';
import {usePermissions} from '@/composables/usePermissions';
import {PERMISSIONS} from '@/services/auth/permissions.types';

const route = useRoute();
const { hasProjectPermission } = usePermissions();

// State
const activeSection = ref('info');
const projectId = computed(() => Number(route.params.projectId));

// Permission checks
const canAccessProjectSettings = computed(() => 
    hasProjectPermission(PERMISSIONS.PROJECT_SETTINGS.READ)
);

// Settings sections
const settingSections = [
    {
        id: 'info',
        name: 'Project Info',
        icon: faInfoCircle
    },
    {
        id: 'invitations', 
        name: 'Invitations',
        icon: faEnvelope
    },
    {
        id: 'members',
        name: 'Members',
        icon: faUsers
    },
    {
        id: 'danger',
        name: 'Danger Zone',
        icon: faExclamationTriangle
    }
];
</script>

<style lang="scss" scoped>
.project-settings-page {
    min-height: 100vh;
    padding: 2rem;
    background-color: var(--color-gray-50);
}

.settings-container {
    max-width: 1200px;
    margin: 0 auto;
}

.page-header {
    margin-bottom: 2rem;
    
    h1 {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-gray-900);
        margin-bottom: 0.5rem;
    }
    
    p {
        color: var(--color-gray-600);
        font-size: 1.1rem;
        line-height: 1.6;
        max-width: 80ch;
    }
}

.settings-nav {
    display: flex;
    gap: 1rem;
    margin-bottom: 2rem;
    background-color: var(--color-white);
    padding: 1rem;
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    
    @media (max-width: 768px) {
        flex-wrap: wrap;
    }
}

.nav-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    background: none;
    border: none;
    border-radius: 6px;
    color: var(--color-gray-600);
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s ease;
    
    &:hover {
        background-color: var(--color-gray-100);
        color: var(--color-gray-800);
    }
    
    &.active {
        background-color: var(--color-primary);
        color: white;
        
        &:hover {
            background-color: var(--color-primary-dark);
        }
    }
    
    @media (max-width: 768px) {
        flex: 1;
        justify-content: center;
        min-width: 120px;
    }
}

.settings-content {
    background-color: var(--color-white);
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    overflow: hidden;
}

.access-denied {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 60vh;
    text-align: center;
    color: var(--color-gray-600);
    
    .access-denied-icon {
        font-size: 4rem;
        color: var(--color-gray-400);
        margin-bottom: 2rem;
    }
    
    h2 {
        color: var(--color-error);
        font-size: 1.5rem;
        margin-bottom: 1rem;
        font-weight: 600;
    }
    
    p {
        max-width: 60ch;
        margin: 0 auto;
        line-height: 1.6;
        font-size: 1.1rem;
    }
}
</style>
