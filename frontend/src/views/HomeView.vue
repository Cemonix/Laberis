<template>
	<div class="home-container">
		<h1>Welcome to Laberis</h1>
		<p>This is the home page of your application.</p>
	</div>
</template>

<script setup lang="ts">
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;
@use "@/styles/variables/theme" as theme;

.home-container {
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	gap: vars.$gap-medium;
	width: 100%;
	background-color: vars.$color-gray-300;
	padding: vars.$padding-medium;

	h1 {
		text-align: center;
		color: vars.$theme-text;
	}
}
</style>