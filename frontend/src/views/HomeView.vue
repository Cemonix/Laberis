<template>
	<div class="home-container">
		<h1>Welcome to Laberis</h1>
		<p>This is the home page of your application.</p>
	</div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.home-container {
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
    gap: 1rem;
	width: 100%;
    background-color: var(--color-gray-300);
    padding: 1rem;

	h1 {
		text-align: center;
        color: var(--color-gray-800);
	}
}
</style>