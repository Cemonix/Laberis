<template>
    <div class="workspace-layout">
        <slot />
    </div>
</template>

<script setup lang="ts"></script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;
@use "@/styles/mixins.scss" as mixins;

.workspace-layout {
    @include mixins.flex-column;
    height: 100vh;
    width: 100vw;
    overflow: hidden;
    background-color: vars.$ws-layout-bg;
    color: vars.$ws-layout-text;
}
</style>
