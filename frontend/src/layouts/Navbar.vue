<template>
    <nav class="navbar">
        <div class="navbar-left">
            <div class="navbar-brand">
                <router-link to="/home">Laberis</router-link>
            </div>
            <div v-if="authStore.isAuthenticated" class="navbar-links nav-link underline-animation">
                <router-link to="/projects">Projects</router-link>
            </div>
        </div>
        <div class="navbar-auth">
            <template v-if="authStore.isAuthenticated">
                <router-link class="username-link nav-link underline-animation" to="/account">Account</router-link>
                <button @click="handleLogout" class="auth-link logout-btn">Logout</button>
            </template>
            <template v-else>
                <router-link class="auth-link nav-link underline-animation" to="/login">Login</router-link>
                <router-link class="auth-link nav-link underline-animation btn-outline" to="/register">Register
                </router-link>
            </template>
        </div>
    </nav>
</template>

<script setup lang="ts">
import {useRouter} from "vue-router";
import {useAuthStore} from "@/stores/authStore";
import {AppLogger} from "@/utils/logger";

const logger = AppLogger.createComponentLogger('Navbar');

const router = useRouter();
const authStore = useAuthStore();

const handleLogout = async () => {
    try {
        await authStore.logout();
        await router.push('/login');
    } catch (error) {
        logger.error('Logout failed:', error);
    }
};
</script>

<style scoped>
.navbar {
    display: grid;
    grid-template-columns: auto 1fr;
    background-color: var(--color-gray-800);
    padding: 1rem;
    color: var(--color-white);
    box-shadow: 0 1px 3px rgba(var(--color-black, 0.05));

    .navbar-left {
        display: flex;
        align-items: baseline;
        gap: 1.5rem;

        .navbar-brand {
            a {
                color: var(--color-white);
                text-decoration: none;
                font-weight: 700;
                font-size: 1.5rem;
            }
        }
    
        .navbar-links {
            a {
                padding: 0.25rem 0;
                color: var(--color-white);
                text-decoration: none;
                font-weight: 500;
                font-size: 1rem;
            }
        }
    }


    .navbar-auth {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 1rem;

        .auth-link {
            color: var(--color-white);
            text-decoration: none;
            font-size: 1rem;
            font-weight: 500;
        }

        .username-link {
            color: var(--color-white);
            text-decoration: none;
            font-size: 1rem;
            font-weight: 500;
        }

        .logout-btn {
            background: none;
            border: none;
            cursor: pointer;
            font-family: inherit;
            color: var(--color-white);
            font-size: 1rem;
            font-weight: 500;
        }
    }
}
</style>