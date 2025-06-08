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
        font-size: vars.$font_size_large;
        margin-bottom: vars.$margin-medium;
    }

    .placeholder-text {
        height: 60%;
        color: vars.$theme-text-light;
        font-style: italic;
        border: 2px dashed vars.$theme-border;
        border-radius: vars.$border-radius-standard;
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

    .quick-actions-list {
        display: flex;
        flex-direction: column;
        gap: vars.$gap-small;
    }
}
</style>