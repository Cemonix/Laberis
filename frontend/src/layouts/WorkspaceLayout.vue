<template>
  <div class="workspace-layout">
    <slot /> </div>
</template>

<script setup lang="ts">

</script>

<style scoped>
.workspace-layout {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  background-color: #2c3e50;
  color: #ecf0f1;
  display: flex;
  flex-direction: column;
}
</style>
