<template>
    <div class="workspace-layout">
        <slot />
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
</style>
