<template>
    <div class="workspace-layout">
        <slot />
        
        <!-- Teleport target for floating action buttons -->
        <div id="fab-container"></div>
    </div>
</template>

<script setup lang="ts"></script>

<style scoped>
.workspace-layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: var(--color-white);
    color: var(--color-gray-800);
}

#fab-container {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 100;
    
    & > * {
        pointer-events: auto;
    }
}
</style>
