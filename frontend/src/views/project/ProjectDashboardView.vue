<template>
    <div class="dashboard-grid">
        <div class="card widget kpi-widget">
            <h3>Annotation Progress</h3>
            <p class="placeholder-text">[Donut Chart Placeholder]</p>
            <div class="kpi-stats">
                <span><strong>Total Assets:</strong> 1,250</span>
                <span><strong>Annotated:</strong> 800</span>
                <span><strong>In Review:</strong> 150</span>
            </div>
        </div>
        <div class="card widget">
            <h3>Label Distribution</h3>
            <p class="placeholder-text">[Bar Chart Placeholder]</p>
        </div>
        <div class="card widget large-widget">
            <h3>Recent Activity</h3>
            <p class="placeholder-text">[Activity Feed Placeholder: e.g., "User X annotated Asset Y"]</p>
        </div>
        <div class="card widget">
            <h3>Quick Actions</h3>
            <div class="quick-actions-list">
                    <button class="btn btn-primary">Start Annotating</button>
                    <button class="btn btn-primary">Upload Assets</button>
            </div>
        </div>
        <div class="card widget">
            <h3>Team Members</h3>
            <p class="placeholder-text">[List of assigned members]</p>
        </div>
    </div>
</template>

<script setup lang="ts">
</script>

<style lang="scss" scoped>
@use "sass:color";
@use "@/styles/variables.scss" as vars;
@use "@/styles/mixins.scss" as mixins;

.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    grid-auto-rows: 250px;
    gap: vars.$padding-large;
}

.widget {
    &.large-widget {
        grid-column: span 2;
    }

    h3 {
        font-size: vars.$font_size_large;
        margin-bottom: vars.$padding-medium;
    }

    .placeholder-text {
        height: 60%;
        color: vars.$theme-text-light;
        font-style: italic;
        border: 2px dashed vars.$theme-border;
        border-radius: vars.$border-radius-standard;
        @include mixins.flex-center;
    }

    .kpi-stats {
        margin-top: vars.$padding-medium;
        display: flex;
        justify-content: space-between;
    }

    .quick-actions-list {
        @include mixins.flex-column($gap: vars.$padding-small);
    }
}
</style>