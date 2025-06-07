<template>
    <div class="default-layout">
        <header class="main-header">
            <Navbar />
        </header>
        <main class="main-content">
            <slot />
        </main>
        <Footer />
    </div>
</template>

<script setup lang="ts">
import Navbar from '@/layouts/Navbar.vue';
import Footer from '@/layouts/Footer.vue';
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;
@use "@/styles/mixins.scss" as mixins;

.default-layout {
    @include mixins.flex-column;
    min-height: 100vh;
    background-color: vars.$theme-background;
}

.main-content {
    position: relative;
    flex-grow: 1;
    padding: vars.$padding-medium;
    background-color: vars.$color-white;
}
</style>