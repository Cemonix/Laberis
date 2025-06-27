<template>
    <div class="data-explorer-layout">
        <slot />
        <ToastContainer />
    </div>
</template>

<script setup lang="ts">
import ToastContainer from '@/components/common/toast/ToastContainer.vue';
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.data-explorer-layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: vars.$theme-background;
    color: vars.$theme-text;
}
</style>