<template>
    <nav class="navbar">
        <div class="navbar-brand">
            <router-link to="/home">Laberis</router-link>
        </div>
        <div class="navbar-links" v-if="authStore.isAuthenticated">
            <router-link to="/home">Home</router-link>
            <router-link to="/projects">Projects</router-link>
            <router-link to="/workspace/project/1/asset/1">Workspace</router-link> <!-- TODO: Hardcoded link for dev -->
        </div>
        <div class="navbar-auth">
            <template v-if="authStore.isAuthenticated">
                <span class="welcome-text">Welcome, {{ authStore.currentUser?.userName }}!</span>
                <button @click="handleLogout" class="auth-link logout-btn">Logout</button>
            </template>
            <template v-else>
                <router-link to="/login" class="auth-link">Login</router-link>
                <router-link to="/register" class="auth-link btn-outline">Register</router-link>
            </template>
        </div>
    </nav>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";

const router = useRouter();
const authStore = useAuthStore();

const handleLogout = async () => {
    try {
        await authStore.logout();
        router.push('/login');
    } catch (error) {
        console.error('Logout failed:', error);
    }
};
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;
@use "@/styles/layout/navbar" as navbar;


.navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
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
        gap: vars.$gap-large;

        a {
            color: navbar.$navbar-text;
            text-decoration: none;
            padding: vars.$padding-small 0;
            position: relative;
            transition: color 0.2s ease-in-out;

            &::after {
                content: "";
                position: absolute;
                width: 0;
                height: 2px;
                bottom: 0;
                left: 50%;
                background-color: vars.$color-primary;
                transition: width 0.3s ease, left 0.3s ease;
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

    .navbar-auth {
        display: flex;
        gap: vars.$gap-medium;
        align-items: center;

        .welcome-text {
            color: navbar.$navbar-text;
            font-size: vars.$font-size-medium;
        }

        .logout-btn {
            background: none;
            border: none;
            cursor: pointer;
            font-size: inherit;
            font-family: inherit;
        }

        .auth-link {
            color: navbar.$navbar-text;
            text-decoration: none;
            padding: vars.$padding-small vars.$padding-medium;
            border-radius: vars.$border-radius-sm;
            transition: all 0.2s ease-in-out;

            &:hover {
                color: vars.$color-link-hover;
            }

            &.btn-outline {
                border: 1px solid vars.$color-primary;
                color: vars.$color-primary;

                &:hover {
                    background-color: vars.$color-primary;
                    color: vars.$color-white;
                }
            }
        }
    }
}
</style>