<template>
    <div class="account-view">
        <div class="account-header">
            <h1>Account Settings</h1>
            <p>Manage your account information and security settings</p>
        </div>

        <div class="account-content">
            <!-- Account Navigation -->
            <div class="account-nav">
                <nav class="nav-menu">
                    <router-link 
                        to="/account" 
                        exact-active-class="active"
                        class="nav-item"
                    >
                        <span class="nav-icon">👤</span>
                        Profile
                    </router-link>
                    <router-link 
                        to="/account/change-password" 
                        exact-active-class="active"
                        class="nav-item"
                    >
                        <span class="nav-icon">🔒</span>
                        Change Password
                    </router-link>
                </nav>
            </div>

            <!-- Account Main Content -->
            <div class="account-main">
                <router-view />
                
                <!-- Default Profile View when no sub-route -->
                <div v-if="route.path === '/account'" class="profile-section">
                    <Card>
                        <template #header>
                            <h2>Profile Information</h2>
                        </template>
                        
                        <div class="profile-info">
                            <div class="info-group">
                                <label class="info-label">Username</label>
                                <div class="info-value">{{ authStore.user?.userName || 'Not available' }}</div>
                            </div>
                            
                            <div class="info-group">
                                <label class="info-label">Email</label>
                                <div class="info-value">{{ authStore.user?.email || 'Not available' }}</div>
                            </div>
                            
                            <div class="info-group">
                                <label class="info-label">Roles</label>
                                <div class="info-value">
                                    <div class="roles-container">
                                        <span 
                                            v-for="role in authStore.user?.roles" 
                                            :key="role" 
                                            class="role-badge"
                                        >
                                            {{ role }}
                                        </span>
                                        <span v-if="!authStore.user?.roles?.length" class="no-roles">
                                            No roles assigned
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </Card>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/authStore';
import { useRoute } from 'vue-router';
import Card from '@/components/common/Card.vue';

const authStore = useAuthStore();
const route = useRoute();
</script>

<style scoped>
.account-view {
    min-height: 100vh;
    background-color: var(--color-gray-50);
    padding: 2rem;
}

.account-header {
    margin-bottom: 2rem;
    text-align: center;
}

.account-header h1 {
    font-family: var(--font-family-heading);
    font-size: 2.5rem;
    font-weight: 700;
    color: var(--color-text-primary);
    margin-bottom: 0.5rem;
}

.account-header p {
    font-size: 1.1rem;
    color: var(--color-text-secondary);
}

.account-content {
    display: grid;
    grid-template-columns: 250px 1fr;
    gap: 2rem;
    max-width: 1200px;
    margin: 0 auto;
}

.account-nav {
    background: var(--color-white);
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    height: fit-content;
}

.nav-menu {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.nav-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    border-radius: 8px;
    text-decoration: none;
    color: var(--color-text-secondary);
    font-weight: 500;
    transition: all 0.2s ease;
}

.nav-item:hover {
    background-color: var(--color-gray-100);
    color: var(--color-text-primary);
}

.nav-item.active {
    background-color: var(--color-primary);
    color: var(--color-white);
}

.nav-icon {
    font-size: 1.2rem;
}

.account-main {
    background: var(--color-white);
    border-radius: 12px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    min-height: 400px;
}

.profile-section {
    padding: 2rem;
}

.profile-info {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.info-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.info-label {
    font-weight: 600;
    color: var(--color-text-primary);
    font-size: 0.875rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.info-value {
    font-size: 1rem;
    color: var(--color-text-secondary);
    padding: 0.75rem;
    background-color: var(--color-gray-100);
    border-radius: 8px;
    border: 1px solid var(--color-border-light);
}

.roles-container {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}

.role-badge {
    background-color: var(--color-primary);
    color: var(--color-white);
    padding: 0.25rem 0.75rem;
    border-radius: 20px;
    font-size: 0.875rem;
    font-weight: 500;
}

.no-roles {
    color: var(--color-text-muted);
    font-style: italic;
}

/* Responsive design */
@media (max-width: 768px) {
    .account-content {
        grid-template-columns: 1fr;
        gap: 1rem;
    }
    
    .account-nav {
        order: 2;
    }
    
    .account-main {
        order: 1;
    }
    
    .nav-menu {
        flex-direction: row;
        overflow-x: auto;
    }
    
    .nav-item {
        flex-shrink: 0;
    }
}
</style>
