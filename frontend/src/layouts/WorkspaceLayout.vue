<template>
    <div class="workspace-layout">
        <slot />
    </div>
</template>

<script setup lang="ts"></script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.workspace-layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: vars.$theme-surface;
    color: vars.$theme-text;
}
</style>
