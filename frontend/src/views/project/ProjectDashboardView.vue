<template>
    <div class="dashboard-grid">
        <div class="widget kpi-widget">
            <h3>Annotation Progress</h3>
            <p class="placeholder-text">[Donut Chart Placeholder]</p>
            <div class="kpi-stats">
                <span><strong>Total Assets:</strong> 1,250</span>
                <span><strong>Annotated:</strong> 800</span>
                <span><strong>In Review:</strong> 150</span>
            </div>
        </div>
        <div class="widget">
            <h3>Label Distribution</h3>
            <p class="placeholder-text">[Bar Chart Placeholder]</p>
        </div>
        <div class="widget large-widget">
            <h3>Recent Activity</h3>
            <p class="placeholder-text">[Activity Feed Placeholder: e.g., "User X annotated Asset Y"]</p>
        </div>
        <div class="widget">
            <h3>Quick Actions</h3>
            <div class="quick-actions-list">
                    <button>Start Annotating</button>
                    <button>Upload Assets</button>
            </div>
        </div>
        <div class="widget">
            <h3>Team Members</h3>
            <p class="placeholder-text">[List of assigned members]</p>
        </div>
    </div>
</template>

<script setup lang="ts">
</script>

<style lang="scss" scoped>
@use "@/styles/variables.scss" as vars;

.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    grid-auto-rows: 250px;
    gap: vars.$padding-large;
}

.widget {
    background-color: #ffffff;
    border: 1px solid vars.$medium-grey-border;
    border-radius: vars.$border-radius-standard;
    padding: vars.$padding-medium;
    box-shadow: 0 1px 3px rgba(0,0,0,0.05);

    &.large-widget {
        grid-column: span 2;
    }

    h3 {
        font-size: vars.$font_size_large;
        margin-bottom: vars.$padding-medium;
    }
    .placeholder-text {
        display: flex;
        align-items: center;
        justify-content: center;
        height: 60%;
        color: lighten(vars.$text_color, 60%);
        font-style: italic;
        border: 2px dashed vars.$medium-grey-border;
        border-radius: vars.$border-radius-standard;
    }
    .kpi-stats {
        margin-top: vars.$padding-medium;
        display: flex;
        justify-content: space-between;
    }
    .quick-actions-list {
        display: flex;
        flex-direction: column;
        gap: vars.$padding-small;
        button {
            padding: vars.$padding-small;
            background-color: vars.$primary-blue;
            color: white;
            border: none;
            cursor: pointer;
            border-radius: vars.$border-radius-input;
            &:hover { background-color: vars.$primary-blue-hover; }
        }
    }
}
</style>