<template>
    <!-- Show page loader during authentication initialization OR token refresh -->
    <PageLoader
        v-if="(authStore.isLoading && !authStore.isInitialized) || authStore.isRefreshingTokens"
        :title="getAuthLoadingTitle()"
        :subtitle="getAuthLoadingSubtitle()"
        :message="getAuthLoadingMessage()"
        :show-progress="authStore.retryingAuth || authStore.isRefreshingTokens"
        :progress="getAuthProgress()"
        :transparent="false"
    />
    
    <!-- Main app content -->
    <component v-else :is="layout">
        <router-view />
    </component>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import PageLoader from '@/components/common/PageLoader.vue';
import { useAuthStore } from '@/stores/authStore';

const route = useRoute();
const authStore = useAuthStore();


const layout = computed(() => {
    if (route.meta.layout && typeof route.meta.layout === 'object') {
        return route.meta.layout;
    }
    return DefaultLayout;
});

// TODO: Move these loading message helpers to core

// Loading message helpers
const getAuthLoadingTitle = () => {
    if (authStore.retryingAuth) {
        return 'Reconnecting...';
    }
    if (authStore.isRefreshingTokens) {
        return 'Authenticating...';
    }
    if (authStore.connectionError) {
        return 'Connection Issue';
    }
    return 'Loading Laberis';
};

const getAuthLoadingSubtitle = () => {
    if (authStore.retryingAuth) {
        return 'Attempting to restore your session';
    }
    if (authStore.isRefreshingTokens) {
        return 'Verifying your credentials';
    }
    if (authStore.connectionError) {
        return 'Checking network connection';
    }
    return 'Initializing your workspace';
};

const getAuthLoadingMessage = () => {
    if (authStore.connectionError) {
        return 'Please check your internet connection and wait while we retry...';
    }
    if (authStore.retryingAuth) {
        return 'Securely refreshing your authentication tokens...';
    }
    if (authStore.isRefreshingTokens) {
        return 'Refreshing your session tokens...';
    }
    return 'Setting up your personalized annotation environment';
};

const getAuthProgress = () => {
    if (authStore.isRefreshingTokens && !authStore.retryingAuth) {
        // Simple token refresh - show indeterminate progress
        return 50;
    }
    if (!authStore.retryingAuth) return 0;
    
    // Simple progress calculation based on retry attempts
    const maxAttempts = authStore.maxRefreshAttempts || 3;
    const currentAttempt = authStore.refreshAttempts || 0;
    return Math.min((currentAttempt / maxAttempts) * 100, 90); // Max 90% until success
};
</script>

<style lang="scss">
#app {
    display: flex;
    flex-direction: column;
    height: 100vh;
}
</style>
