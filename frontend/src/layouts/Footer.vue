<template>
    <footer class="footer">
        <p>&copy; Laberis</p>
    </footer>
</template>

<script setup lang="ts"></script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.footer {
    text-align: center;
    padding: vars.$padding-small;
    background-color: vars.$color-gray-200;
    border-top: vars.$border-width solid vars.$theme-border;
    width: 100%;
}
</style>
