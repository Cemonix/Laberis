<template>
    <nav class="navbar">
        <div class="navbar-brand">
            <router-link to="/home">Laberis</router-link>
        </div>
        <div class="navbar-links">
            <router-link to="/home">Home</router-link>
            <router-link to="/projects">Projects</router-link>
            <router-link to="/workspace/project/1/asset/1">Workspace</router-link> <!-- TODO: Hardcoded link for dev -->
        </div>
    </nav>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";

useRouter();
</script>

<style scoped>
.navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background-color: #333;
    padding: 0.75rem 1.5rem;
    color: white;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.navbar-brand a {
    color: white;
    text-decoration: none;
    font-weight: bold;
    font-size: 1.5rem;
}

.navbar-links {
    display: flex;
    gap: 1.5rem;
}

.navbar-links a {
    color: white;
    text-decoration: none;
    padding: 0.5rem 0;
    position: relative;
    transition: color 0.2s ease;
}

.navbar-links a::after {
    content: "";
    position: absolute;
    width: 0;
    height: 2px;
    bottom: 0;
    left: 50%;
    background-color: #007bff;
    transition: width 0.3s ease, left 0.3s ease;
}

.navbar-links a:hover,
.navbar-links a.router-link-exact-active {
    color: #00aaff;
}

.navbar-links a:hover::after,
.navbar-links a.router-link-exact-active::after {
    width: 100%;
    left: 0;
}
</style>
