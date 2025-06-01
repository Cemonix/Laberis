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

<style scoped>
.login-container {
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
}

.login-box {
    min-width: 400px;
    padding: 2rem;
    border: 1px solid #ccc;
    border-radius: 8px;
    background-color: #f9f9f9;
}

.login-container h1 {
    text-align: center;
    margin-bottom: 1.5rem;
    color: #333;
}

.form-group {
    margin-bottom: 1rem;
}

.form-group label {
    display: block;
    margin-bottom: 0.5rem;
}

.form-group input {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #ccc;
    border-radius: 4px;
}

button {
    width: 100%;
    padding: 0.75rem;
    background-color: #007bff;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 1rem;
}

button:hover {
    background-color: #0056b3;
}

button:focus {
    outline: 2px solid #0056b3;
    outline-offset: 2px;
}
</style>