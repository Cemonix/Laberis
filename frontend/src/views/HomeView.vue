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
@use '@/styles/mixins' as mixins;

.home-container {
	@include mixins.flex-center($direction: column, $gap: vars.$padding-medium);
	width: 100%;
	background-color: vars.$theme-background;
	padding: vars.$padding-medium;

	h1 {
		text-align: center;
		color: vars.$theme-text;
	}
}
</style>