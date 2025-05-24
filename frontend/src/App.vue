<template>
    <div id="main-app-container">
      <nav class="app-nav">
        <router-link to="/labelling">Labelling Tool</router-link>
        <!-- Add other global navigation links here if needed -->
      </nav>
      <router-view></router-view>
    </div>
  </template>
  
  <script setup lang="ts">
  // No specific script needed for this basic App.vue for now
  </script>
  
  <style scoped>
  #main-app-container {
    width: 100%;
    min-height: 100vh;
    display: flex;
    flex-direction: column;
  }
  
  .app-nav {
    background-color: #333;
    padding: 10px 20px;
    text-align: center;
  }
  
  .app-nav a {
    color: white;
    margin: 0 15px;
    text-decoration: none;
    font-weight: bold;
  }
  
  .app-nav a.router-link-exact-active {
    color: #42b983; /* Vue green */
  }
  </style>