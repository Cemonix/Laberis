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
@use "@/styles/variables" as vars;
@use "@/styles/components/navbar" as navbar;
@use "@/styles/mixins" as mixins;

.navbar {
    @include mixins.flex-between-center;
    background-color: navbar.$navbar-bg;
    padding: vars.$padding-medium vars.$padding-large;
    color: navbar.$navbar-text;
    box-shadow: vars.$shadow-sm;

    .navbar-brand {
        a {
            color: navbar.$navbar-text;
            text-decoration: none;
            font-weight: vars.$font-weight-heading;
            font-size: 1.5rem;
        }
    }

    .navbar-links {
        display: flex;
        gap: vars.$padding-large;

        a {
            color: navbar.$navbar-text;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color vars.$transition-normal-ease-in-out;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$color-primary;
                transition: width vars.$transition-slow-ease, left vars.$transition-slow-ease;
            }

            &:hover,
            &.router-link-exact-active {
                color: vars.$color-link-hover;
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