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
import Navbar from '@/components/layout/Navbar.vue';
import Footer from '@/components/layout/Footer.vue';
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.default-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    background-color: vars.$background_color;
}

.main-content {
    flex-grow: 1;
    padding: vars.$padding-medium;
    background-color: white;
}
</style>