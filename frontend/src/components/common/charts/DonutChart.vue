<template>
    <div class="donut-chart-container">
        <svg ref="svgRef" :width="width" :height="height"></svg>
        <div class="chart-legend">
            <div 
                v-for="(item, index) in data" 
                :key="item.label" 
                class="legend-item"
            >
                <div 
                    class="legend-color" 
                    :style="{ backgroundColor: colors[index] }"
                ></div>
                <span class="legend-label">{{ item.label }}: {{ item.value }}</span>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import {nextTick, onMounted, ref, watch} from 'vue';
import * as d3 from 'd3';

interface ChartData {
    label: string;
    value: number;
}

interface Props {
    data: ChartData[];
    width?: number;
    height?: number;
    innerRadius?: number;
    outerRadius?: number;
    colors?: string[];
}

const props = withDefaults(defineProps<Props>(), {
    width: 300,
    height: 250,
    innerRadius: 50,
    outerRadius: 100,
    colors: () => ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6']
});

const svgRef = ref<SVGElement>();

const drawChart = () => {
    if (!svgRef.value || !props.data.length) return;

    // Clear previous chart
    d3.select(svgRef.value).selectAll("*").remove();

    const svg = d3.select(svgRef.value);
    const g = svg.append("g")
        .attr("transform", `translate(${props.width / 2}, ${props.height / 2})`);

    const pie = d3.pie<ChartData>()
        .value(d => d.value)
        .sort(null);

    const arc = d3.arc<d3.PieArcDatum<ChartData>>()
        .innerRadius(props.innerRadius)
        .outerRadius(props.outerRadius);

    const arcs = g.selectAll(".arc")
        .data(pie(props.data))
        .enter().append("g")
        .attr("class", "arc");

    arcs.append("path")
        .attr("d", arc)
        .attr("fill", (_, i) => props.colors[i % props.colors.length])
        .attr("stroke", "#fff")
        .attr("stroke-width", 2)
        .style("transition", "all 0.3s ease")
        .on("mouseover", function() {
            d3.select(this)
                .transition()
                .duration(200)
                .attr("transform", "scale(1.05)");
        })
        .on("mouseout", function() {
            d3.select(this)
                .transition()
                .duration(200)
                .attr("transform", "scale(1)");
        });

    // Add percentage labels
    arcs.append("text")
        .attr("transform", d => `translate(${arc.centroid(d)})`)
        .attr("text-anchor", "middle")
        .attr("font-size", "14px")
        .attr("font-weight", "bold")
        .attr("fill", "#fff")
        .text(d => {
            const total = props.data.reduce((sum, item) => sum + item.value, 0);
            const percentage = Math.round((d.data.value / total) * 100);
            return percentage > 5 ? `${percentage}%` : '';
        });
};

onMounted(() => {
    nextTick(() => {
        drawChart();
    });
});

watch(() => props.data, () => {
    drawChart();
}, { deep: true });
</script>

<style scoped>
.donut-chart-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
}

.chart-legend {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    font-size: 0.875rem;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.legend-color {
    width: 16px;
    height: 16px;
    border-radius: 50%;
}

.legend-label {
    font-weight: 500;
}
</style>
