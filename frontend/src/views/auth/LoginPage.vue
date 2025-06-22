<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Welcome Back</h1>
            <Form @submit="handleLogin" class="auth-form">
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>
                <div class="form-group">
                    <label for="email">Email</label>
                    <input 
                        type="email" 
                        id="email" 
                        v-model="email" 
                        placeholder="Enter your email" 
                        required 
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-group">
                    <label for="password">Password</label>
                    <input 
                        type="password" 
                        id="password" 
                        v-model="password" 
                        placeholder="Enter your password" 
                        required 
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary" :disabled="isLoading">
                        {{ isLoading ? 'Signing In...' : 'Sign In' }}
                    </Button>
                </div>
            </Form>
            <div class="auth-footer">
                <p>Don't have an account? <router-link to="/register" class="auth-link">Create one</router-link></p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";

const router = useRouter();
const authStore = useAuthStore();

const email = ref("");
const password = ref("");
const isLoading = ref(false);
const errorMessage = ref("");

const handleLogin = async () => {
    if (!email.value || !password.value) {
        errorMessage.value = "Please fill in all fields";
        return;
    }

    isLoading.value = true;
    errorMessage.value = "";

    try {
        await authStore.login({
            email: email.value,
            password: password.value
        });
        
        // Redirect to home page or intended destination
        router.push('/home');
    } catch (error) {
        console.error("Login failed:", error);
        errorMessage.value = error instanceof Error ? error.message : "Login failed. Please try again.";
    } finally {
        isLoading.value = false;
    }
};
</script>

<style lang="scss" scoped>
@use "@/styles/auth";
</style>
