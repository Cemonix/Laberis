<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Welcome Back</h1>
            <Form @submit="handleLogin" class="auth-form">
                <!-- Invitation notice -->
                <div v-if="inviteToken" class="info-banner">
                    <strong>Project Invitation</strong>
                    You're logging in to accept a project invitation. After logging in, you'll be redirected to accept the invitation.
                </div>
                
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
import {onMounted, ref} from "vue";
import {useRoute, useRouter} from "vue-router";
import {useAuthStore} from "@/stores/authStore";
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";
import {AppLogger} from "@/utils/logger";

const logger = AppLogger.createComponentLogger('LoginView');

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const email = ref("");
const password = ref("");
const isLoading = ref(false);
const errorMessage = ref("");
const inviteToken = ref<string | null>(null);

// Check for invitation token in URL
onMounted(() => {
    const token = route.query.inviteToken as string;
    if (token) {
        inviteToken.value = token;
    }
});

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
        
        // Check if there's an invitation token to process
        if (inviteToken.value) {
            // Redirect to invitation acceptance page
            router.push(`/invite/accept/${inviteToken.value}`);
        } else {
            // Check if there's a redirect URL (for other cases)
            const urlParams = new URLSearchParams(window.location.search);
            const redirectUrl = urlParams.get('redirect');
            
            if (redirectUrl) {
                router.push(decodeURIComponent(redirectUrl));
            } else {
                // Use smart redirect based on last project
                const smartRedirectUrl = authStore.getPostLoginRedirectUrl();
                router.push(smartRedirectUrl);
            }
        }
    } catch (error) {
        logger.error("Login failed:", error);
        errorMessage.value = error instanceof Error ? error.message : "Login failed. Please try again.";
    } finally {
        isLoading.value = false;
    }
};
</script>

<style scoped>
@import "@/styles/auth.css";

.info-banner {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-info-light);
    border: 1px solid var(--color-info);
    border-radius: 8px;
    color: var(--color-info-dark);
    font-size: 0.875rem;
    text-align: center;
    
    strong {
        font-weight: 600;
    }
}
</style>
