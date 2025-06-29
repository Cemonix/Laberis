<template>
    <div class="account-view">
        <h1>Account Management</h1>
        <p>Manage your account settings and preferences here.</p>
        <pre>{{ authStore.user }}</pre>
    </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<style scoped>
.account-view {
    padding: 20px;
    background-color: #f9f9f9;
    border: 1px solid #eee;
    border-radius: 4px;
}
</style>
