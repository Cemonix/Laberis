<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Create Account</h1>
            <Form @submit="handleRegister" class="auth-form">
                <div class="form-group">
                    <label for="firstName">First Name</label>
                    <input 
                        type="text" 
                        id="firstName" 
                        v-model="firstName" 
                        placeholder="Enter your first name" 
                        required 
                    />
                </div>
                <div class="form-group">
                    <label for="lastName">Last Name</label>
                    <input 
                        type="text" 
                        id="lastName" 
                        v-model="lastName" 
                        placeholder="Enter your last name" 
                        required 
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
                    />
                    <span v-if="!passwordsMatch && confirmPassword" class="error-message">
                        Passwords do not match
                    </span>
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary">
                        Create Account
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
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";

const firstName = ref("");
const lastName = ref("");
const email = ref("");
const password = ref("");
const confirmPassword = ref("");

// Form validation
const isFormValid = computed(() => {
    return firstName.value.trim() && 
           lastName.value.trim() && 
           email.value.trim() && 
           password.value.length >= 6 && 
           password.value === confirmPassword.value;
});

const passwordsMatch = computed(() => {
    return password.value === confirmPassword.value || confirmPassword.value === "";
});

const handleRegister = () => {
    if (!passwordsMatch.value) {
        alert("Passwords do not match");
        return;
    }
    
    if (!isFormValid.value) {
        alert("Please fill in all fields correctly");
        return;
    }
    
    console.log("Registering user:", { 
        firstName: firstName.value.trim(), 
        lastName: lastName.value.trim(),
        email: email.value.trim(), 
        password: password.value 
    });
    
    // TODO: Implement actual registration logic
    alert("Registration successful! (Demo)");
};
</script>

<style lang="scss" scoped>
@use "@/styles/auth";
</style>
