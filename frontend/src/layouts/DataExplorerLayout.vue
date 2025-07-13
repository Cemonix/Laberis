<template>
    <div class="data-explorer-layout">
        <slot />
        <ToastContainer />
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
</style>