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
                <span class="welcome-text">
                    Welcome <router-link to="/account" class="username-link">{{ authStore.currentUser?.userName }}</router-link>
                </span>
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
    display: grid;
    grid-template-columns: auto 1fr;
    align-items: baseline;
    background-color: navbar.$navbar-bg;
    padding: vars.$padding-small vars.$padding-medium;
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
                line-height: vars.$line-height-xsmall;
            }
        }
    
        .navbar-links {
            a {
                position: relative;
                padding: vars.$padding-xsmall 0;
                transition: color 0.2s ease-in-out;
                color: navbar.$navbar-text;
                text-decoration: none;
                font-weight: vars.$font-weight-medium;
                font-size: vars.$font-size-medium;
                line-height: vars.$line-height-xsmall;
    
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
    }


    .navbar-auth {
        display: flex;
        align-items: baseline;
        justify-content: flex-end;
        gap: vars.$gap-medium;

        
        .auth-link {
            color: navbar.$navbar-text;
            text-decoration: none;
            padding: vars.$padding-small vars.$padding-medium;
            border-radius: vars.$border-radius-sm;
            transition: all 0.2s ease-in-out;
            font-size: vars.$font-size-small;
            font-weight: vars.$font-weight-medium;
            line-height: vars.$line-height-xsmall;
            
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
        
        .welcome-text {
            color: navbar.$navbar-text;
            font-size: vars.$font-size-medium;
            font-weight: vars.$font-weight-medium;
            line-height: vars.$line-height-xsmall;

            .username-link {
                color: navbar.$navbar-text;
                position: relative;
                padding: vars.$padding-xsmall 0;
                text-decoration: none;
                font-weight: inherit;
                font-size: inherit;
                line-height: inherit;
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

        .logout-btn {
            background: none;
            border: none;
            cursor: pointer;
            font-size: vars.$font-size-medium;
            font-weight: vars.$font-weight-medium;
            line-height: vars.$line-height-xsmall;
            font-family: inherit;
        }
    }
}
</style>