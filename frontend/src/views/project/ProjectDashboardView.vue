<template>
    <div class="dashboard-grid">
        <Card class="widget kpi-widget">
            <h3>Annotation Progress</h3>
            <p class="placeholder-text">[Donut Chart Placeholder]</p>
            <div class="kpi-stats">
                <span><strong>Total Assets:</strong> 1,250</span>
                <span><strong>Annotated:</strong> 800</span>
                <span><strong>In Review:</strong> 150</span>
            </div>
        </Card>
        <Card class="widget">
            <h3>Label Distribution</h3>
            <p class="placeholder-text">[Bar Chart Placeholder]</p>
        </Card>
        <Card class="widget large-widget">
            <h3>Recent Activity</h3>
            <p class="placeholder-text">[Activity Feed Placeholder: e.g., "User X annotated Asset Y"]</p>
        </Card>
        <Card class="widget">
            <h3>Team Members</h3>
            <p class="placeholder-text">[List of assigned members]</p>
        </Card>
    </div>
</template>

<script setup lang="ts">
import Card from '@/components/common/Card.vue';
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables" as vars;
@use "@/styles/variables/theme" as theme;

.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    grid-auto-rows: 250px;
    gap: vars.$gap-large;
}

.widget {
    &.large-widget {
        grid-column: span 2;
    }

    h3 {
        font-size: vars.$font-size-large;
        margin-bottom: vars.$margin-medium;
    }

    .placeholder-text {
        height: 75%;
        color: vars.$theme-text-light;
        font-style: italic;
        border: 2px dashed vars.$theme-border;
        border-radius: vars.$border-radius-md;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
    }

    .kpi-stats {
        margin-top: vars.$margin-medium;
        display: flex;
        justify-content: space-between;
    }
}
</style>