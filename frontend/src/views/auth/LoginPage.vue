<template>
    <div class="auth-container">
        <div class="auth-glass-box">
            <h1>Welcome Back</h1>
            <Form @submit="handleLogin" class="auth-form">
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
                        placeholder="Enter your password" 
                        required 
                    />
                </div>
                <div class="form-actions">
                    <Button type="submit" class="btn btn-primary">
                        Sign In
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
import Button from "@/components/common/Button.vue";
import Form from "@/components/common/Form.vue";

const email = ref("");
const password = ref("");

const handleLogin = () => {
    console.log("Logging in:", { email: email.value, password: password.value });
};
</script>

<style lang="scss" scoped>
@use "@/styles/auth";
</style>
