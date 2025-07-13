<template>
    <span class="animated-counter">{{ displayValue }}</span>
</template>

<script setup lang="ts">
import {onMounted, ref, watch} from 'vue';
import {animateValue, formatNumber} from '@/utils/charts';

interface Props {
    value: number;
    duration?: number;
    formatted?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
    duration: 1000,
    formatted: false
});

const displayValue = ref(0);

const animateToValue = (targetValue: number) => {
    animateValue(
        displayValue.value,
        targetValue,
        props.duration,
        (currentValue) => {
            displayValue.value = props.formatted ? 
                parseInt(formatNumber(currentValue).replace(/[^\d]/g, '')) : 
                currentValue;
        }
    );
};

onMounted(() => {
    animateToValue(props.value);
});

watch(() => props.value, (newValue) => {
    animateToValue(newValue);
});
</script>

<style scoped>
.animated-counter {
    font-weight: bold;
    transition: all 0.3s ease;
}
</style>
