<template>
    <div class="data-explorer-layout">
        <slot />
        <ToastContainer />
        
        <!-- Teleport target for floating action buttons -->
        <div id="fab-container"></div>
    </div>
</template>

<script setup lang="ts">
import ToastContainer from '@/components/common/toast/ToastContainer.vue';
</script>

<style scoped>
.data-explorer-layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: var(--color-gray-50);
    color: var(--color-gray-800);
}

/* Container for teleported floating action buttons */
#fab-container {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 100;
    
    /* Allow button interactions while keeping container non-interactive */
    & > * {
        pointer-events: auto;
    }
}
</style>