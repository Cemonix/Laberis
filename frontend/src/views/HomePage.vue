<template>
	<div class="home-container">
		<h1>Welcome to Laberis</h1>
	</div>
</template>

<script setup lang="ts">
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.home-container {
	display: flex;
	justify-content: center;
	align-items: start;
	background-color: vars.$background_color;
	width: 100%;
	padding: vars.$padding-medium;

	h1 {
		text-align: center;
		color: vars.$text_color;
	}
}
</style>