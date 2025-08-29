<template>
    <div class="task-statistics-widget">
        <div v-if="!data" class="no-data">
            <i class="icon-info"></i>
            <span>No task statistics available</span>
        </div>

        <div v-else class="statistics-content">
            <!-- Summary Cards -->
            <div class="summary-cards">
                <div class="stat-card total">
                    <div class="stat-value">{{ data.totalTasks }}</div>
                    <div class="stat-label">Total Tasks</div>
                    <div
                        class="stat-trend"
                        :class="getTrendClass(data.productivityTrend)"
                    >
                        <i :class="getTrendIcon(data.productivityTrend)"></i>
                        <span
                            >{{
                                Math.abs(data.productivityTrend).toFixed(1)
                            }}%</span
                        >
                    </div>
                </div>

                <div class="stat-card completed">
                    <div class="stat-value">{{ data.completedTasks }}</div>
                    <div class="stat-label">Completed</div>
                    <div class="completion-rate">
                        {{ getCompletionRate() }}%
                    </div>
                </div>

                <div class="stat-card vetoed">
                    <div class="stat-value">{{ data.vetoedTasks }}</div>
                    <div class="stat-label">Returned</div>
                    <div class="veto-rate">{{ getVetoRate() }}%</div>
                </div>

                <div class="stat-card avg-time">
                    <div class="stat-value">
                        {{ data.averageCompletionTimeHours.toFixed(1) }}
                    </div>
                    <div class="stat-label">Avg Hours</div>
                    <div class="time-unit">per task</div>
                </div>
            </div>

            <!-- Status Distribution Chart -->
            <div class="distribution-section">
                <div class="section-header">
                    <h4>Task Status Distribution</h4>
                </div>
                <div
                    class="distribution-chart"
                    ref="distributionChartRef"
                ></div>
                <div class="distribution-legend">
                    <div
                        v-for="status in data.statusDistribution"
                        :key="status.status"
                        class="legend-item"
                    >
                        <div
                            class="legend-color"
                            :style="{
                                backgroundColor: getStatusColor(status.status),
                            }"
                        ></div>
                        <span class="legend-label">{{
                            formatStatusLabel(status.status)
                        }}</span>
                        <span class="legend-count">{{ status.count }}</span>
                    </div>
                </div>
            </div>

            <!-- Daily Trends Chart -->
            <div class="trends-section">
                <div class="section-header">
                    <h4>Daily Task Trends</h4>
                </div>
                <div class="trends-chart" ref="trendsChartRef"></div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from "vue";
import * as d3 from "d3";
import type { TaskStatisticsDto } from "@/services/project/dashboard/dashboard.types";

interface Props {
    data?: TaskStatisticsDto;
    loading?: boolean;
    error?: string | null;
}

const props = defineProps<Props>();

// Refs
const distributionChartRef = ref<HTMLElement>();
const trendsChartRef = ref<HTMLElement>();

// Methods
const getCompletionRate = (): number => {
    if (!props.data || props.data.totalTasks === 0) return 0;
    return Math.round(
        (props.data.completedTasks / props.data.totalTasks) * 100
    );
};

const getVetoRate = (): number => {
    if (!props.data || props.data.totalTasks === 0) return 0;
    return Math.round((props.data.vetoedTasks / props.data.totalTasks) * 100);
};

const getTrendClass = (trend: number): string => {
    if (trend > 0) return "positive";
    if (trend < 0) return "negative";
    return "neutral";
};

const getTrendIcon = (trend: number): string => {
    if (trend > 0) return "icon-trending-up";
    if (trend < 0) return "icon-trending-down";
    return "icon-minus";
};

const formatStatusLabel = (status: string): string => {
    return status
        .split("_")
        .map(
            (word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
        )
        .join(" ");
};

const getStatusColor = (status: string): string => {
    const colorMap: Record<string, string> = {
        COMPLETED: "#10b981",
        IN_PROGRESS: "#f59e0b",
        NOT_STARTED: "#6b7280",
        READY_FOR_ANNOTATION: "#3b82f6",
        READY_FOR_REVIEW: "#8b5cf6",
        READY_FOR_COMPLETION: "#06b6d4",
        SUSPENDED: "#ef4444",
        DEFERRED: "#f97316",
        CHANGES_REQUIRED: "#dc2626",
    };
    return colorMap[status] || "#6b7280";
};

// D3 Chart Creation
const createDistributionChart = () => {
    if (!distributionChartRef.value || !props.data?.statusDistribution?.length)
        return;

    // Clear previous chart
    d3.select(distributionChartRef.value).selectAll("*").remove();

    const container = distributionChartRef.value;
    const containerRect = container.getBoundingClientRect();
    const margin = { top: 10, right: 10, bottom: 10, left: 10 };
    const width = containerRect.width - margin.left - margin.right;
    const height = 120 - margin.top - margin.bottom;

    if (width <= 0 || height <= 0) return;

    const svg = d3
        .select(container)
        .append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom);

    const g = svg
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    const radius = Math.min(width, height) / 2;
    const centerX = width / 2;
    const centerY = height / 2;

    // Prepare data
    const data = props.data.statusDistribution.filter((d) => d.count > 0);

    const pie = d3
        .pie<any>()
        .value((d) => d.count)
        .sort(null);

    const arc = d3
        .arc<any>()
        .innerRadius(radius * 0.5)
        .outerRadius(radius);

    // Create donut chart
    const arcs = g
        .selectAll(".arc")
        .data(pie(data))
        .enter()
        .append("g")
        .attr("class", "arc")
        .attr("transform", `translate(${centerX},${centerY})`);

    arcs.append("path")
        .attr("d", arc)
        .style("fill", (d) => getStatusColor(d.data.status))
        .style("stroke", "var(--color-white)")
        .style("stroke-width", 2);

    // Add percentage labels
    arcs.append("text")
        .attr("transform", (d) => `translate(${arc.centroid(d)})`)
        .attr("text-anchor", "middle")
        .style("font-size", "10px")
        .style("font-weight", "bold")
        .style("fill", "white")
        .text((d) =>
            d.data.percentage >= 5 ? `${d.data.percentage.toFixed(0)}%` : ""
        );

    // Add center text
    g.append("text")
        .attr("x", centerX)
        .attr("y", centerY - 5)
        .attr("text-anchor", "middle")
        .attr("dominant-baseline", "central")
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .style("fill", "var(--color-gray-800)")
        .text("Task");

    g.append("text")
        .attr("x", centerX)
        .attr("y", centerY + 8)
        .attr("text-anchor", "middle")
        .attr("dominant-baseline", "central")
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .style("fill", "var(--color-gray-800)")
        .text("Status");
};

const createTrendsChart = () => {
    if (!trendsChartRef.value || !props.data?.dailyTaskCounts?.length) return;

    // Clear previous chart
    d3.select(trendsChartRef.value).selectAll("*").remove();

    const container = trendsChartRef.value;
    const containerRect = container.getBoundingClientRect();
    const margin = { top: 20, right: 10, bottom: 30, left: 30 };
    const width = containerRect.width - margin.left - margin.right;
    const height = 120 - margin.top - margin.bottom;

    if (width <= 0 || height <= 0) return;

    const svg = d3
        .select(container)
        .append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom);

    const g = svg
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // Prepare data
    const data = props.data.dailyTaskCounts
        .map((d) => ({
            ...d,
            date: new Date(d.date),
        }))
        .sort((a, b) => a.date.getTime() - b.date.getTime());

    // Scales
    const xScale = d3
        .scaleTime()
        .domain(d3.extent(data, (d) => d.date) as [Date, Date])
        .range([0, width]);

    const yScale = d3
        .scaleLinear()
        .domain([
            0,
            d3.max(data, (d) =>
                Math.max(d.tasksCreated, d.tasksCompleted, d.tasksVetoed)
            ) || 1,
        ])
        .range([height, 0]);

    // Lines
    const line = d3
        .line<any>()
        .x((d) => xScale(d.date))
        .y((d) => yScale(d.value))
        .curve(d3.curveMonotoneX);

    // Created tasks line
    const createdData = data.map((d) => ({
        date: d.date,
        value: d.tasksCreated,
    }));
    g.append("path")
        .datum(createdData)
        .attr("class", "line-created")
        .attr("d", line)
        .style("fill", "none")
        .style("stroke", "#3b82f6")
        .style("stroke-width", 2);

    // Completed tasks line
    const completedData = data.map((d) => ({
        date: d.date,
        value: d.tasksCompleted,
    }));
    g.append("path")
        .datum(completedData)
        .attr("class", "line-completed")
        .attr("d", line)
        .style("fill", "none")
        .style("stroke", "#10b981")
        .style("stroke-width", 2);

    // Vetoed tasks line
    const vetoedData = data.map((d) => ({
        date: d.date,
        value: d.tasksVetoed,
    }));
    g.append("path")
        .datum(vetoedData)
        .attr("class", "line-vetoed")
        .attr("d", line)
        .style("fill", "none")
        .style("stroke", "#ef4444")
        .style("stroke-width", 2);

    // X axis
    const xAxis = d3
        .axisBottom(xScale)
        .tickSize(-height)
        .tickFormat(d3.timeFormat("%m/%d") as any);

    g.append("g")
        .attr("class", "x-axis")
        .attr("transform", `translate(0,${height})`)
        .call(xAxis as any)
        .selectAll("text")
        .style("font-size", "9px")
        .style("fill", "var(--color-gray-600)");

    // Y axis
    const yAxis = d3.axisLeft(yScale).tickSize(-width).ticks(4);

    g.append("g")
        .attr("class", "y-axis")
        .call(yAxis)
        .selectAll("text")
        .style("font-size", "9px")
        .style("fill", "var(--color-gray-600)");

    // Style grid lines
    g.selectAll(".tick line")
        .style("stroke", "var(--color-gray-300)")
        .style("stroke-dasharray", "2,2");

    g.selectAll(".domain").style("stroke", "var(--color-gray-300)");
};

// Watchers and lifecycle
watch(
    () => props.data,
    () => {
        if (props.data) {
            setTimeout(() => {
                createDistributionChart();
                createTrendsChart();
            }, 100);
        }
    },
    { immediate: true }
);

onMounted(() => {
    const resizeObserver = new ResizeObserver(() => {
        createDistributionChart();
        createTrendsChart();
    });

    if (distributionChartRef.value) {
        resizeObserver.observe(distributionChartRef.value);
    }

    if (trendsChartRef.value) {
        resizeObserver.observe(trendsChartRef.value);
    }

    onUnmounted(() => {
        resizeObserver.disconnect();
    });
});
</script>

<style scoped>
.task-statistics-widget {
    height: auto;
    display: flex;
    flex-direction: column;
}

.no-data {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: auto;
    min-height: 200px;
    color: var(--color-gray-600);
    gap: 0.5rem;
}

.statistics-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    height: auto;
}

/* Summary Cards */
.summary-cards {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
}

.stat-card {
    background: var(--color-gray-100);
    border-radius: 6px;
    padding: 0.75rem;
    text-align: center;
    position: relative;
}

.stat-value {
    font-size: 20px;
    font-weight: bold;
    color: var(--color-gray-800);
    margin-bottom: 0.125rem;
}

.stat-label {
    font-size: 0.6875rem;
    color: var(--color-gray-600);
    margin-bottom: 4px;
}

.stat-trend {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.125rem;
    font-size: 0.5625rem;
    font-weight: 500;
}

.stat-trend.positive {
    color: var(--color-success);
}

.stat-trend.negative {
    color: var(--color-error);
}

.stat-trend.neutral {
    color: var(--color-gray-600);
}

.completion-rate {
    font-size: 0.6875rem;
    font-weight: bold;
    color: var(--color-success);
}

.veto-rate {
    font-size: 0.6875rem;
    font-weight: bold;
    color: var(--color-error);
}

.time-unit {
    font-size: 9px;
    color: var(--color-gray-600);
}

/* Distribution Section */
.distribution-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.section-header h4 {
    margin: 0;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-gray-800);
}

.distribution-chart {
    height: 120px;
    width: 100%;
}

.distribution-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    justify-content: center;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.625rem;
}

.legend-color {
    width: 8px;
    height: 8px;
    border-radius: 2px;
    flex-shrink: 0;
}

.legend-label {
    color: var(--color-gray-600);
}

.legend-count {
    font-weight: bold;
    color: var(--color-gray-800);
}

/* Trends Section */
.trends-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

.trends-chart {
    height: 120px;
    width: 100%;
    min-height: 120px;
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .summary-cards {
        grid-template-columns: repeat(2, 1fr);
    }

    .stat-card {
        padding: 0.5rem;
    }

    .stat-value {
        font-size: 1rem;
    }

    .distribution-legend {
        gap: 0.25rem;
    }

    .legend-item {
        font-size: 0.5625rem;
    }
}
</style>
