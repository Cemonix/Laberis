<template>
    <div class="login-container">
        <div class="login-box">
            <h1>Login</h1>
            <form @submit.prevent="handleLogin">
                <div class="form-group">
                    <label for="email">Email</label>
                    <input type="email" id="email" v-model="email" placeholder="Enter your email" required />
                </div>
                <div class="form-group">
                    <label for="password">Password</label>
                    <input type="password" id="password" v-model="password" placeholder="Enter your password" required />
                </div>
                <button type="submit" class="btn btn-primary">Login</button>
            </form>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue";

const email = ref("");
const password = ref("");

const handleLogin = () => {
    console.log("Logging in:", { email: email.value, password: password.value });
};
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;
@use "@/styles/mixins" as mixins;

.login-container {
    width: 100%;
    @include mixins.flex-center;
}

.login-box {
    min-width: 400px;
    padding: vars.$padding-large;
    border: vars.$border-width solid vars.$theme-border;
    border-radius: vars.$border-radius-standard;
    background-color: vars.$color-gray-200;

    h1 {
        text-align: center;
        margin-bottom: vars.$padding-large;
        color: vars.$theme-text;
    }
}

.btn {
    width: 100%;
    margin-top: vars.$padding-small;
}
</style>
