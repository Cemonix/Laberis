<template>
    <div class="dashboard-container">
        <!-- Loading State -->
        <div v-if="loading" class="dashboard-loading">
            <div class="loading-spinner"></div>
            <p>Loading dashboard data...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="dashboard-error">
            <div class="error-icon">⚠️</div>
            <h3>Failed to Load Dashboard</h3>
            <p>{{ error }}</p>
            <button @click="refreshData" class="retry-button">
                Try Again
            </button>
        </div>

        <!-- Dashboard Content -->
        <div v-else class="dashboard-grid">
            <!-- Project Header -->
            <Card class="widget project-header fade-in" style="animation-delay: 0.05s">
                <div class="project-info">
                    <h2 class="project-title">{{ project?.name || 'Project Dashboard' }}</h2>
                    <p class="project-description">{{ project?.description || 'Loading project details...' }}</p>
                    <div class="demo-notice">
                        <small>📊 Dashboard showing demo data - stats endpoint not yet implemented</small>
                    </div>
                </div>
                <div class="refresh-controls">
                    <button 
                        @click="refreshData" 
                        :disabled="refreshing"
                        class="refresh-button"
                        :class="{ 'refreshing': refreshing }"
                    >
                        <font-awesome-icon :icon="refreshing ? faSpinner : faSyncAlt" :spin="refreshing" />
                        {{ refreshing ? 'Refreshing...' : 'Refresh' }}
                    </button>
                </div>
            </Card>

            <!-- Quick Stats -->
            <Card class="widget stats-widget wide-widget fade-in" style="animation-delay: 0.1s">
                <h3>Quick Stats</h3>
                <div class="quick-stats-grid">
                    <div class="quick-stat">
                        <div class="stat-icon">👥</div>
                        <div class="stat-content">
                            <AnimatedCounter :value="activeAnnotators" class="stat-number" />
                            <span class="stat-text">Active Annotators</span>
                        </div>
                    </div>
                    <div class="quick-stat">
                        <div class="stat-icon">📊</div>
                        <div class="stat-content">
                            <AnimatedCounter :value="stats?.totalAnnotations || 0" class="stat-number" />
                            <span class="stat-text">Total Annotations</span>
                        </div>
                    </div>
                    <div class="quick-stat">
                        <div class="stat-icon">🏷️</div>
                        <div class="stat-content">
                            <AnimatedCounter :value="labelDistribution.length" class="stat-number" />
                            <span class="stat-text">Label Types</span>
                        </div>
                    </div>
                    <div class="quick-stat">
                        <div class="stat-icon">⚡</div>
                        <div class="stat-content">
                            <span class="stat-number">{{ Math.round(completionPercentage) }}%</span>
                            <span class="stat-text">Completed</span>
                        </div>
                    </div>
                </div>
            </Card>

            <!-- Annotation Progress -->
            <Card class="widget kpi-widget fade-in" style="animation-delay: 0.2s">
                <h3>Annotation Progress</h3>
                <div class="chart-container">
                    <DonutChart 
                        :data="annotationProgressData" 
                        :width="300" 
                        :height="220"
                        :colors="chartColors"
                    />
                </div>
                <div class="kpi-stats">
                    <div class="stat-item">
                        <span class="stat-label">Total Assets</span>
                        <AnimatedCounter :value="totalAssets" class="stat-value" />
                    </div>
                    <div class="stat-item">
                        <span class="stat-label">Annotated</span>
                        <AnimatedCounter :value="annotatedAssets" class="stat-value" />
                    </div>
                    <div class="stat-item">
                        <span class="stat-label">In Review</span>
                        <AnimatedCounter :value="reviewAssets" class="stat-value" />
                    </div>
                </div>
                <div class="completion-bar">
                    <div class="completion-label">
                        <span>Completion Progress</span>
                        <span class="percentage">{{ completionPercentage }}%</span>
                    </div>
                    <div class="progress-bar">
                        <div 
                            class="progress-fill" 
                            :style="{ width: `${completionPercentage}%` }"
                        ></div>
                    </div>
                </div>
            </Card>
            
            <!-- Label Distribution -->
            <Card class="widget chart-widget fade-in" style="animation-delay: 0.3s">
                <h3>Label Distribution</h3>
                <div class="chart-container">
                    <BarChart 
                        :data="labelDistribution" 
                        :width="350" 
                        :height="200"
                        :color="primaryColor"
                    />
                </div>
                <div class="chart-summary">
                    <p>Top {{ labelDistribution.length }} most used labels</p>
                </div>
            </Card>
            
            <!-- Recent Activity -->
            <Card class="widget large-widget fade-in" style="animation-delay: 0.4s">
                <div class="widget-header">
                    <h3>Recent Activity</h3>
                    <span class="activity-count">{{ recentActivities.length }} recent items</span>
                </div>
                <ActivityFeed :activities="recentActivities" />
            </Card>
            
            <!-- Team Members -->
            <Card class="widget fade-in" style="animation-delay: 0.5s">
                <div class="widget-header">
                    <h3>Team Members</h3>
                    <span class="member-count">{{ teamMembers.length }} members</span>
                </div>
                <TeamMembers :members="teamMembers" />
            </Card>
        </div>
    </div>
</template>

<script setup lang="ts">
import Card from '@/components/common/Card.vue';
import DonutChart from '@/components/common/charts/DonutChart.vue';
import BarChart from '@/components/common/charts/BarChart.vue';
import ActivityFeed from '@/components/project/dashboard/ActivityFeed.vue';
import TeamMembers from '@/components/project/dashboard/TeamMembers.vue';
import AnimatedCounter from '@/components/common/AnimatedCounter.vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faSpinner, faSyncAlt } from '@fortawesome/free-solid-svg-icons';
import { useProjectDashboard } from '@/composables/useProjectDashboard';

// Use the dashboard composable
const {
    // State
    loading,
    refreshing,
    error,
    
    // Data
    stats,
    recentActivities,
    labelDistribution,
    
    // Computed
    annotationProgressData,
    chartColors,
    primaryColor,
    teamMembers,
    
    // Statistics
    totalAssets,
    annotatedAssets,
    reviewAssets,
    completionPercentage,
    activeAnnotators,
    
    // Methods
    refreshData,
    
    // Project data
    project
} = useProjectDashboard();
</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;

.dashboard-container {
    width: 100%;
    padding: vars.$padding-large;
    min-height: 100vh;
    background: #fafafa;
}

// Loading State
.dashboard-loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 60vh;
    gap: vars.$gap-large;
    
    .loading-spinner {
        width: 40px;
        height: 40px;
        border: 3px solid #e5e7eb;
        border-top: 3px solid vars.$color-primary;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }
    
    p {
        font-size: vars.$font-size-medium;
        color: #6b7280;
        margin: 0;
    }
}

// Error State
.dashboard-error {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 60vh;
    gap: vars.$gap-medium;
    text-align: center;
    
    .error-icon {
        font-size: 3rem;
        margin-bottom: vars.$margin-medium;
    }
    
    h3 {
        font-size: vars.$font-size-large;
        color: #dc2626;
        margin: 0;
    }
    
    p {
        font-size: vars.$font-size-medium;
        color: #6b7280;
        margin: 0;
        max-width: 400px;
    }
    
    .retry-button {
        background: vars.$color-primary;
        color: white;
        border: none;
        padding: vars.$padding-small vars.$padding-medium;
        border-radius: vars.$border-radius-md;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s ease;
        
        &:hover {
            background: vars.$color-primary-hover;
            transform: translateY(-1px);
        }
    }
}

// Main Dashboard Grid
.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    grid-auto-rows: min-content;
    gap: vars.$gap-large;
    width: 100%;
    align-items: start;
}

// Widget Base Styles
.widget {
    background: white;
    border-radius: vars.$border-radius-lg;
    box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
    border: 1px solid #e5e7eb;
    display: flex;
    flex-direction: column;
    transition: all 0.2s ease;
    position: relative;
    overflow: hidden;
    
    &:hover {
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
        transform: translateY(-1px);
    }

    // Widget Header
    .widget-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: vars.$padding-medium vars.$padding-large 0 vars.$padding-large;
        
        h3 {
            margin: 0;
        }
        
        .activity-count,
        .member-count {
            font-size: vars.$font-size-small;
            color: #6b7280;
            background: #f3f4f6;
            padding: 2px vars.$padding-small;
            border-radius: vars.$border-radius-sm;
            font-weight: 500;
        }
    }

    // Large widgets (span 2 columns on larger screens)
    &.large-widget {
        grid-column: span 2;
        
        @media (max-width: 1200px) {
            grid-column: span 1;
        }
    }

    // Wide widgets (span 2 columns, good for stats)
    &.wide-widget {
        grid-column: span 2;
        
        @media (max-width: 900px) {
            grid-column: span 1;
        }
    }

    // KPI widget needs more space for charts
    &.kpi-widget {
        min-height: 450px;
        
        @media (max-width: 768px) {
            min-height: auto;
        }
    }

    // Stats widget optimized for quick stats
    &.stats-widget {
        min-height: 200px;
        
        @media (max-width: 768px) {
            min-height: auto;
        }
    }

    // Project header specific styling
    &.project-header {
        grid-column: 1 / -1;
        background: white;
        border: 1px solid #e5e7eb;
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
        min-height: 100px;
        
        .project-info {
            flex: 1;
            
            .project-title {
                font-size: vars.$font-size-xlarge;
                font-weight: 700;
                margin: 0 0 vars.$margin-small 0;
                color: #111827;
            }
            
            .project-description {
                font-size: vars.$font-size-medium;
                color: #6b7280;
                margin: 0 0 vars.$margin-small 0;
                max-width: 600px;
            }
            
            .demo-notice {
                margin-top: vars.$margin-small;
                
                small {
                    color: #9ca3af;
                    font-style: italic;
                    font-size: vars.$font-size-small;
                }
            }
        }
        
        .refresh-controls {
            .refresh-button {
                background: white;
                border: 1px solid #d1d5db;
                color: #374151;
                padding: vars.$padding-small vars.$padding-medium;
                border-radius: vars.$border-radius-md;
                cursor: pointer;
                transition: all 0.2s ease;
                display: flex;
                align-items: center;
                gap: vars.$gap-small;
                font-weight: 500;
                
                &:hover:not(:disabled) {
                    background: #f9fafb;
                    border-color: #9ca3af;
                }
                
                &:disabled {
                    opacity: 0.6;
                    cursor: not-allowed;
                }
                
                &.refreshing {
                    background: #f3f4f6;
                }
            }
        }
    }

    h3 {
        font-size: vars.$font-size-large;
        font-weight: 600;
        margin-bottom: vars.$margin-medium;
        padding: vars.$padding-medium vars.$padding-large 0 vars.$padding-large;
        color: #111827;
    }
}

// Chart Container
.chart-container {
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 0 vars.$padding-medium;
    overflow: hidden;
    
    // Ensure charts are responsive
    :deep(svg) {
        max-width: 100%;
        height: auto;
    }
}

// Chart Widget Specific Styles
.chart-widget {
    min-height: 320px;
    
    @media (max-width: 768px) {
        min-height: auto;
    }
    
    .chart-container {
        padding: 0 vars.$padding-small;
    }
}

// KPI Widget Specific Styles
.kpi-widget {
    .kpi-stats {
        margin-top: vars.$margin-medium;
        padding: 0 vars.$padding-large vars.$padding-medium vars.$padding-large;
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: vars.$gap-medium;
        
        .stat-item {
            display: flex;
            flex-direction: column;
            align-items: center;
            text-align: center;
            padding: vars.$padding-small;
            background: #f9fafb;
            border: 1px solid #e5e7eb;
            border-radius: vars.$border-radius-md;
            
            .stat-label {
                font-size: vars.$font-size-small;
                font-weight: 500;
                color: #6b7280;
                margin-bottom: vars.$margin-xsmall;
            }
            
            .stat-value {
                font-size: vars.$font-size-large;
                font-weight: 700;
                color: vars.$color-primary;
            }
        }
    }
    
    .completion-bar {
        margin-top: vars.$margin-large;
        padding: 0 vars.$padding-large vars.$padding-large vars.$padding-large;
        
        .completion-label {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: vars.$margin-small;
            font-size: vars.$font-size-small;
            font-weight: 500;
            
            .percentage {
                color: vars.$color-primary;
                font-weight: 600;
            }
        }
        
        .progress-bar {
            height: 6px;
            background: #e5e7eb;
            border-radius: vars.$border-radius-sm;
            overflow: hidden;
            
            .progress-fill {
                height: 100%;
                background: vars.$color-primary;
                border-radius: vars.$border-radius-sm;
                transition: width 1s ease-in-out;
            }
        }
    }
}

// Chart Summary
.chart-summary {
    padding: 0 vars.$padding-large vars.$padding-medium vars.$padding-large;
    
    p {
        margin: 0;
        font-size: vars.$font-size-small;
        color: #6b7280;
        text-align: center;
        font-style: italic;
    }
}

// Quick Stats Widget
.stats-widget {
    .quick-stats-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: vars.$gap-medium;
        padding: 0 vars.$padding-large vars.$padding-large vars.$padding-large;
        
        @media (max-width: 1200px) {
            grid-template-columns: repeat(2, 1fr);
        }
        
        @media (max-width: 600px) {
            grid-template-columns: 1fr;
        }
        
        .quick-stat {
            display: flex;
            align-items: center;
            gap: vars.$gap-medium;
            padding: vars.$padding-medium;
            background: #f9fafb;
            border: 1px solid #e5e7eb;
            border-radius: vars.$border-radius-md;
            transition: all 0.2s ease;
            min-width: 0; // Allow content to shrink
            
            &:hover {
                background: #f3f4f6;
                transform: translateY(-1px);
            }
            
            .stat-icon {
                font-size: 1.5rem;
                width: 48px;
                height: 48px;
                display: flex;
                align-items: center;
                justify-content: center;
                background: vars.$color-primary;
                color: white;
                border-radius: vars.$border-radius-md;
                flex-shrink: 0;
            }
            
            .stat-content {
                display: flex;
                flex-direction: column;
                min-width: 0;
                flex: 1;
                
                .stat-number {
                    font-size: vars.$font-size-large;
                    font-weight: 700;
                    color: #111827;
                    line-height: 1.1;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                }
                
                .stat-text {
                    font-size: vars.$font-size-small;
                    color: #6b7280;
                    font-weight: 500;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                }
            }
        }
    }
}

// Animations
.fade-in {
    opacity: 0;
    transform: translateY(10px);
    animation: fadeInUp 0.4s ease-out forwards;
}

@keyframes fadeInUp {
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}

// Enhanced chart styling
:deep(.chart-legend) {
    margin-top: vars.$margin-medium;
    background: #f9fafb;
    border: 1px solid #e5e7eb;
    border-radius: vars.$border-radius-md;
    padding: vars.$padding-small vars.$padding-medium;
}

:deep(.legend-item) {
    gap: vars.$gap-small;
    font-size: vars.$font-size-small;
    color: #374151;
    font-weight: 500;
}

:deep(.legend-color) {
    border: 1px solid rgba(0, 0, 0, 0.1);
}

// Responsive Design
@media (max-width: 1400px) {
    .dashboard-grid {
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    }
    
    .widget.large-widget,
    .widget.wide-widget {
        grid-column: span 2;
    }
}

@media (max-width: 1200px) {
    .dashboard-grid {
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    }
    
    .widget.large-widget {
        grid-column: span 1;
    }
    
    .widget.wide-widget {
        grid-column: span 2;
    }
    
    .kpi-widget .chart-container :deep(svg) {
        width: 250px !important;
        height: 180px !important;
    }
    
    .chart-widget .chart-container :deep(svg) {
        width: 280px !important;
        height: 160px !important;
    }
}

@media (max-width: 1024px) {
    .dashboard-container {
        padding: vars.$padding-medium;
    }
    
    .dashboard-grid {
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: vars.$gap-medium;
    }
    
    .widget.large-widget,
    .widget.wide-widget {
        grid-column: span 1;
    }
    
    .widget.project-header {
        flex-direction: column;
        text-align: center;
        gap: vars.$gap-medium;
        
        .project-info .project-title {
            font-size: vars.$font-size-large;
        }
    }
    
    .kpi-widget .kpi-stats {
        grid-template-columns: 1fr;
        gap: vars.$gap-small;
    }
    
    .stats-widget .quick-stats-grid {
        grid-template-columns: repeat(2, 1fr);
    }
    
    // Further reduce chart sizes on tablets
    .kpi-widget .chart-container :deep(svg) {
        width: 220px !important;
        height: 160px !important;
    }
    
    .chart-widget .chart-container :deep(svg) {
        width: 250px !important;
        height: 140px !important;
    }
}

@media (max-width: 700px) {
    .dashboard-container {
        padding: vars.$padding-medium;
    }
    
    .dashboard-grid {
        grid-template-columns: 1fr;
        gap: vars.$gap-medium;
    }
    
    .widget, .widget.large-widget, .widget.wide-widget {
        grid-column: span 1;
        min-width: 0;
    }
    
    .widget h3 {
        font-size: vars.$font-size-medium;
        padding: vars.$padding-medium 0 0 vars.$padding-medium;
    }
    
    .kpi-stats,
    .quick-stats-grid {
        padding: 0 vars.$padding-medium vars.$padding-medium vars.$padding-medium;
    }
    
    .stats-widget .quick-stats-grid {
        grid-template-columns: 1fr;
    }
    
    // Mobile chart sizes
    .kpi-widget .chart-container :deep(svg) {
        width: 200px !important;
        height: 140px !important;
    }
    
    .chart-widget .chart-container :deep(svg) {
        width: 220px !important;
        height: 120px !important;
    }
    
    .chart-container {
        padding: 0 vars.$padding-small;
    }
}

// Accessibility improvements
@media (prefers-reduced-motion: reduce) {
    .fade-in {
        animation: none;
        opacity: 1;
        transform: none;
    }
    
    .widget {
        transition: none;
        
        &:hover {
            transform: none;
        }
    }
    
    .loading-spinner {
        animation: none;
    }
}

// Focus states for accessibility
.refresh-button:focus {
    outline: 2px solid vars.$color-primary;
    outline-offset: 2px;
}

.retry-button:focus {
    outline: 2px solid vars.$color-primary;
    outline-offset: 2px;
}
</style>