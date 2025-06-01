<template>
    <footer class="footer">
        <p>&copy; Laberis</p>
    </footer>
</template>

<script setup lang="ts"></script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.footer {
    text-align: center;
    padding: vars.$padding-small;
    background-color: vars.$footer-bg;
    border-top: 1px solid vars.$footer-border;
    width: 100%;
}
</style>
