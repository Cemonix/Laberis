<template>
    <nav class="navbar">
        <div class="navbar-left">
            <div class="navbar-brand">
                <router-link to="/home">Laberis</router-link>
            </div>
            <div class="navbar-links" v-if="authStore.isAuthenticated">
                <router-link to="/projects">Projects</router-link>
            </div>
        </div>
        <div class="navbar-auth">
            <template v-if="authStore.isAuthenticated">
                <router-link to="/account" class="username-link">Account</router-link>
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
@use "@/styles/mixins/underline-animation" as mixins;


.navbar {
    display: grid;
    grid-template-columns: auto 1fr;
    background-color: navbar.$navbar-bg;
    padding: vars.$padding-medium;
    color: navbar.$navbar-text;
    box-shadow: vars.$shadow-sm;

    .navbar-left {
        display: flex;
        align-items: baseline;
        gap: vars.$gap-large;

        .navbar-brand {
            a {
                color: navbar.$navbar-text;
                text-decoration: none;
                font-weight: vars.$font-weight-xlarge;
                font-size: vars.$font-size-xlarge;
            }
        }
    
        .navbar-links {
            a {
                padding: vars.$padding-xsmall 0;
                color: navbar.$navbar-text;
                text-decoration: none;
                font-weight: vars.$font-weight-medium;
                font-size: vars.$font-size-medium;

                @include mixins.underline-animation();
            }
        }
    }


    .navbar-auth {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: vars.$gap-medium;

        .auth-link {
            color: navbar.$navbar-text;
            text-decoration: none;
            font-size: vars.$font-size-medium;
            font-weight: vars.$font-weight-medium;

            @include mixins.underline-animation();
        }

        .username-link {
            color: navbar.$navbar-text;
            text-decoration: none;
            font-size: vars.$font-size-medium;
            font-weight: vars.$font-weight-medium;

            @include mixins.underline-animation();
        }

        .logout-btn {
            background: none;
            border: none;
            cursor: pointer;
            font-family: inherit;
            color: navbar.$navbar-text;
            font-size: vars.$font-size-medium;
            font-weight: vars.$font-weight-medium;

            @include mixins.underline-animation();
        }
    }
}
</style>