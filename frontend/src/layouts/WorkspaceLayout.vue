<template>
  <div class="workspace-layout">
    <slot /> </div>
</template>

<script setup lang="ts">

</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.workspace-layout {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  background-color: vars.$workspace-layout-bg;
  color: vars.$workspace-layout-text;
  display: flex;
  flex-direction: column;
}
</style>
