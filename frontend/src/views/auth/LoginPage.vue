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
                <button type="submit">Login</button>
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
@use "@/styles/variables.scss" as vars;

.login-container {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
}

.login-box {
    min-width: 400px;
    padding: vars.$padding-large;
    border: 1px solid vars.$medium-grey-border;
    border-radius: vars.$border-radius-standard;
    background-color: vars.$light-grey-bg;

    h1 {
        text-align: center;
        margin-bottom: vars.$padding-large;
        color: vars.$text_color;
    }
}

.form-group {
    margin-bottom: vars.$padding-medium;

    label {
        display: block;
        margin-bottom: vars.$padding-small;
    }

    input {
        width: 100%;
        padding: vars.$padding-small;
        border: 1px solid vars.$medium-grey-border;
        border-radius: vars.$border-radius-input;
    }
}

button {
    width: 100%;
    padding: 0.75rem;
    background-color: vars.$primary-blue;
    color: white;
    border: none;
    border-radius: vars.$border-radius-input;
    cursor: pointer;
    font-size: 1rem;

    &:hover {
        background-color: vars.$primary-blue-hover;
    }

    &:focus {
        outline: 2px solid vars.$primary-blue-hover;
        outline-offset: 2px;
    }
}
</style>
