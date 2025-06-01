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
import Navbar from '../components/Navbar.vue';
import Footer from '../components/Footer.vue';
</script>

<style scoped>
.default-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    background-color: #f0f2f5;
}

.main-header {
    background-color: #001529;
    color: white;
    padding: 1rem;
    text-align: center;
}

.main-header nav a {
    color: #a0d911;
    margin: 0 10px;
    text-decoration: none;
}
.main-header nav a:hover {
    text-decoration: underline;
}

.main-content {
    flex-grow: 1;
    padding: 1rem;
    background-color: #ffffff;
}

.main-footer {
    background-color: #e8e8e8;
    color: #555;
    padding: 1rem;
    text-align: center;
    margin-top: auto;
}
</style>
