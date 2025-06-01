<template>
    <component :is="layout">
        <router-view />
    </component>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import DefaultLayout from '@/layouts/DefaultLayout.vue';

const route = useRoute();


const layout = computed(() => {
    if (route.meta.layout && typeof route.meta.layout === 'object') {
        return route.meta.layout;
    }
    return DefaultLayout;
});
</script>

<style lang="scss">
#app {
    display: flex;
    flex-direction: column;
    height: 100vh;
}
</style>
