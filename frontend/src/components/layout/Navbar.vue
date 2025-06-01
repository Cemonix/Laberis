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

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background-color: vars.$dark-grey-nav-bg;
    padding: vars.$padding-medium vars.$padding-large;
    color: vars.$laberis-brand-text;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

    .navbar-brand {
        a {
            color: vars.$laberis-brand-text;
            text-decoration: none;
            font-weight: bold;
            font-size: 1.5rem;
        }
    }

    .navbar-links {
        display: flex;
        gap: vars.$padding-large;

        a {
            color: vars.$laberis-brand-text;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color vars.$transition-normal;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$primary-blue;
                transition: width vars.$transition-long, left vars.$transition-long;
            }

            &:hover,
            &.router-link-exact-active {
                color: vars.$primary-link-hover-color;
            }

            &:hover::after,
            &.router-link-exact-active::after {
                width: 100%;
                left: 0;
            }
        }
    }
}
</style>