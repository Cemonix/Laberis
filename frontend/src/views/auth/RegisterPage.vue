<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Create Account</h1>
            <Form @submit="handleRegister" class="auth-form">
                <div v-if="errorMessage" class="error-banner">
                    {{ errorMessage }}
                </div>
                <div class="form-group">
                    <label for="userName">Username</label>
                    <input 
                        type="text" 
                        id="userName" 
                        v-model="userName" 
                        placeholder="Enter your username" 
                        required 
                        :disabled="isLoading"
                    />
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
                        placeholder="Enter your password (min. 6 characters)" 
                        required 
                        minlength="6"
                        :disabled="isLoading"
                    />
                </div>
                <div class="form-group">
                    <label for="confirmPassword">Confirm Password</label>
                    <input 
                        type="password" 
                        id="confirmPassword" 
                        v-model="confirmPassword" 
                        placeholder="Confirm your password" 
                        required 
                        :class="{ 'error': !passwordsMatch }"
                        :disabled="isLoading"
                    />
                    <span v-if="!passwordsMatch && confirmPassword" class="error-message">
                        Passwords do not match
                    </span>
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary" :disabled="isLoading || !isFormValid">
                        {{ isLoading ? 'Creating Account...' : 'Create Account' }}
                    </Button>
                </div>
            </Form>
            <div class="auth-footer">
                <p>Already have an account? <router-link to="/login" class="auth-link">Sign in</router-link></p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";

const router = useRouter();
const authStore = useAuthStore();

const userName = ref("");
const email = ref("");
const password = ref("");
const confirmPassword = ref("");
const isLoading = ref(false);
const errorMessage = ref("");

// Form validation
const isFormValid = computed(() => {
    return userName.value.trim() && 
           email.value.trim() && 
           password.value.length >= 6 && 
           password.value === confirmPassword.value;
});

const passwordsMatch = computed(() => {
    return password.value === confirmPassword.value || confirmPassword.value === "";
});

const handleRegister = async () => {
    if (!passwordsMatch.value) {
        errorMessage.value = "Passwords do not match";
        return;
    }
    
    if (!isFormValid.value) {
        errorMessage.value = "Please fill in all fields correctly";
        return;
    }

    isLoading.value = true;
    errorMessage.value = "";

    try {
        await authStore.register({
            email: email.value.trim(),
            userName: userName.value.trim(),
            password: password.value,
            confirmPassword: confirmPassword.value
        });
        
        // Redirect to home page after successful registration
        router.push('/home');
    } catch (error) {
        console.error("Registration failed:", error);
        errorMessage.value = error instanceof Error ? error.message : "Registration failed. Please try again.";
    } finally {
        isLoading.value = false;
    }
};
</script>

<style lang="scss" scoped>
@use "@/styles/auth";
</style>
