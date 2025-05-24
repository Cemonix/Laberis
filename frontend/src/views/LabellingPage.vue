<template>
    <div class="labelling-page">
        <div class="controls">
            <input type="text" v-model="imageUrl" placeholder="Enter Image URL" />
            <button @click="loadImage">Load Image</button>
            <div class="tool-selection">
                <span>Tool:</span>
                <button @click="selectTool('bbox')" :class="{ active: currentTool === 'bbox' }">Bounding Box</button>
                <button @click="selectTool('polygon')" :class="{ active: currentTool === 'polygon' }">Polygon</button>
                <button @click="selectTool('line')" :class="{ active: currentTool === 'line' }">Line</button>
                <button @click="selectTool('none')" :class="{ active: currentTool === 'none' }">None (View)</button>
            </div>
            <button @click="clearLastAnnotation" v-if="annotations.length > 0">Undo Last</button>
            <button @click="clearAllAnnotations" v-if="annotations.length > 0">Clear All</button>
        </div>
        <div class="canvas-container">
            <canvas 
                ref="canvasRef"
                @mousedown="handleMouseDown"
                @mousemove="handleMouseMove"
                @mouseup="handleMouseUp"
                @dblclick="handleDoubleClick">
            </canvas>
        </div>
        <div v-if="debugMousePosition">
            Mouse: {{ mousePosition.x }}, {{ mousePosition.y }}
        </div>
    </div>
</template>
  
<script setup lang="ts">
    import { ref, onMounted, reactive } from 'vue';
    
    // Types for annotations
    interface Point {
        x: number;
        y: number;
    }
    
    interface BBox {
        x: number;
        y: number;
        width: number;
        height: number;
    }
    
    interface Annotation {
        id: string;
        type: 'bbox' | 'polygon' | 'line';
        data: BBox | Point[];
        color: string;
    }
    
    const canvasRef = ref<HTMLCanvasElement | null>(null);
    const ctx = ref<CanvasRenderingContext2D | null>(null);
    const imageUrl = ref<string>('https://picsum.photos/800/600');
    const loadedImage = ref<HTMLImageElement | null>(null);
    
    const currentTool = ref<'bbox' | 'polygon' | 'line' | 'none'>('bbox');
    const annotations = ref<Annotation[]>([]);
    
    const isDrawing = ref(false);
    const startPoint = ref<Point | null>(null);
    const currentPolygonPoints = ref<Point[]>([]);
    const mousePosition = reactive<Point>({ x: 0, y: 0 });
    const debugMousePosition = true;
    
    onMounted(() => {
        if (canvasRef.value) {
            ctx.value = canvasRef.value.getContext('2d');
            // loadImage(); // Optionally load default image on mount
        }
    });
    
    const getMousePos = (event: MouseEvent): Point => {
        if (!canvasRef.value) return { x: 0, y: 0 };
        const rect = canvasRef.value.getBoundingClientRect();
        return {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top,
        };
    };
    
    const loadImage = () => {
        if (!ctx.value || !canvasRef.value) return;
        const img = new Image();
        img.crossOrigin = "Anonymous"; // Important for images from other domains to avoid tainted canvas
        img.onload = () => {
            loadedImage.value = img;
            canvasRef.value!.width = img.width;
            canvasRef.value!.height = img.height;
            annotations.value = [];
            redrawCanvas();
        };
        img.onerror = () => {
            alert("Failed to load image. Check URL and CORS policy if it's an external image.");
        }
        img.src = imageUrl.value;
    };
    
    const selectTool = (tool: 'bbox' | 'polygon' | 'line' | 'none') => {
        currentTool.value = tool;
        isDrawing.value = false; // Reset drawing state when tool changes
        currentPolygonPoints.value = []; // Reset polygon points
        startPoint.value = null;
        redrawCanvas(); // Redraw to clear any previews
    };
    
    const redrawCanvas = () => {
        if (!ctx.value || !canvasRef.value) return;
        const context = ctx.value;
        context.clearRect(0, 0, canvasRef.value.width, canvasRef.value.height);
    
        // Draw image
        if (loadedImage.value) {
            context.drawImage(loadedImage.value, 0, 0);
        }
    
        // Draw existing annotations
        annotations.value.forEach(ann => {
            context.strokeStyle = ann.color;
            context.fillStyle = `${ann.color}40`; // Semi-transparent fill
            context.lineWidth = 2;
            if (ann.type === 'bbox') {
                const data = ann.data as BBox;
                context.strokeRect(data.x, data.y, data.width, data.height);
                context.fillRect(data.x, data.y, data.width, data.height);
            } else if (ann.type === 'polygon') {
                const points = ann.data as Point[];
                if (points.length > 1) {
                    context.beginPath();
                    context.moveTo(points[0].x, points[0].y);
                    for (let i = 1; i < points.length; i++) {
                        context.lineTo(points[i].x, points[i].y);
                    }
                    context.closePath();
                    context.stroke();
                    context.fill();
                }
            } else if (ann.type === 'line') {
                const points = ann.data as Point[];
                if (points.length > 1) {
                    context.beginPath();
                    context.moveTo(points[0].x, points[0].y);
                    context.lineTo(points[1].x, points[1].y);
                    context.stroke();
                }
            }
        });
    
        // Draw current drawing preview
        if (isDrawing.value && startPoint.value) {
            context.strokeStyle = 'red'; // Preview color
            context.fillStyle = '#FF000040';
            context.lineWidth = 1;
            if (currentTool.value === 'bbox') {
                const currentRect = calculateRect(startPoint.value, mousePosition);
                context.strokeRect(currentRect.x, currentRect.y, currentRect.width, currentRect.height);
            } else if (currentTool.value === 'line') {
                context.beginPath();
                context.moveTo(startPoint.value.x, startPoint.value.y);
                context.lineTo(mousePosition.x, mousePosition.y);
                context.stroke();
            }
        }
    
        // Draw current polygon preview
        if (currentTool.value === 'polygon' && currentPolygonPoints.value.length > 0) {
            context.strokeStyle = 'blue'; // Preview color for polygon
            context.fillStyle = '#0000FF40';
            context.lineWidth = 1;
            context.beginPath();
            context.moveTo(currentPolygonPoints.value[0].x, currentPolygonPoints.value[0].y);
            for (let i = 1; i < currentPolygonPoints.value.length; i++) {
                context.lineTo(currentPolygonPoints.value[i].x, currentPolygonPoints.value[i].y);
            }
            // Line to current mouse position for preview
            if (currentPolygonPoints.value.length > 0 && !isDrawing.value) { // isDrawing is false until dblclick for polygon
                context.lineTo(mousePosition.x, mousePosition.y);
            }
            context.stroke();
            if (currentPolygonPoints.value.length > 2 && !isDrawing.value) { // fill preview if more than 2 points
                const tempPoints = [...currentPolygonPoints.value, mousePosition];
                context.beginPath();
                context.moveTo(tempPoints[0].x, tempPoints[0].y);
                for (let i = 1; i < tempPoints.length; i++) {
                    context.lineTo(tempPoints[i].x, tempPoints[i].y);
                }
                context.closePath();
                context.fill();
            }
        }
    };
    
    const calculateRect = (p1: Point, p2: Point): BBox => {
        return {
            x: Math.min(p1.x, p2.x),
            y: Math.min(p1.y, p2.y),
            width: Math.abs(p1.x - p2.x),
            height: Math.abs(p1.y - p2.y),
        };
    };
    
    const handleMouseDown = (event: MouseEvent) => {
        if (!loadedImage.value || currentTool.value === 'none') return;
        const pos = getMousePos(event);
    
        if (currentTool.value === 'bbox' || currentTool.value === 'line') {
            isDrawing.value = true;
            startPoint.value = pos;
        } else if (currentTool.value === 'polygon') {
            isDrawing.value = true; // Start drawing polygon
            currentPolygonPoints.value.push(pos);
        }

        redrawCanvas(); // Initial draw to show starting point
    };
    
    const handleMouseMove = (event: MouseEvent) => {
        const pos = getMousePos(event);
        mousePosition.x = pos.x;
        mousePosition.y = pos.y;
    
        if (!loadedImage.value || currentTool.value === 'none') return;
        
        if (isDrawing.value && (currentTool.value === 'bbox' || currentTool.value === 'line')) {
            redrawCanvas(); // Redraw to show live preview
        } else if (currentTool.value === 'polygon' && currentPolygonPoints.value.length > 0) {
            redrawCanvas(); // Redraw to show line to cursor
        }
    };
    
    const handleMouseUp = (event: MouseEvent) => {
        if (!isDrawing.value || !startPoint.value || currentTool.value === 'none') return;
        const pos = getMousePos(event);
    
        if (currentTool.value === 'bbox') {
        const rect = calculateRect(startPoint.value, pos);
        if (rect.width > 0 && rect.height > 0) { // Add only if valid rect
            annotations.value.push({ id: Date.now().toString(), type: 'bbox', data: rect, color: 'green' });
        }
        } else if (currentTool.value === 'line') {
            annotations.value.push(
                { id: Date.now().toString(), type: 'line', data: [startPoint.value, pos], color: 'orange' }
            );
        }
        
        isDrawing.value = false;
        startPoint.value = null;
        redrawCanvas();
    };
    
    // We'll use dblclick for polygon completion, and simple clicks for adding points.
    // Modify mousedown/mouseup logic if you prefer click for bbox/line point placement.
    // For this example, mousedown->drag->mouseup is for bbox/line.
    // For polygon, we'll use clicks (which are essentially mousedown+mouseup without drag).
    // Let's add a click handler specifically for polygons.
    
    canvasRef.value?.addEventListener('click', (event: MouseEvent) => {
        if (currentTool.value !== 'polygon' || !loadedImage.value) return;
        
        const pos = getMousePos(event);
        currentPolygonPoints.value.push(pos);
        // isDrawing for polygon means it's finalized. So keep it false here.
        redrawCanvas();
    });
    
    
    const handleDoubleClick = (event: MouseEvent) => {
        if (currentTool.value === 'polygon' && currentPolygonPoints.value.length >= 3) {
            // Finalize polygon
            annotations.value.push({ 
                id: Date.now().toString(), 
                type: 'polygon', 
                data: [...currentPolygonPoints.value], 
                color: 'purple' 
            });
            currentPolygonPoints.value = [];
            isDrawing.value = false; // Ensure this is reset
            redrawCanvas();
        }
    };
    
    const clearLastAnnotation = () => {
        if (annotations.value.length > 0) {
            annotations.value.pop();
            redrawCanvas();
        }
    };
    
    const clearAllAnnotations = () => {
        annotations.value = [];
        currentPolygonPoints.value = []; // also clear any in-progress polygon
        redrawCanvas();
    };
  
</script>
  
<style scoped>
.labelling-page {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 20px;
    gap: 15px;
}

.controls {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    align-items: center;
    padding: 10px;
    background-color: #e9e9e9;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.controls input[type="text"] {
    padding: 8px;
    border: 1px solid #ccc;
    border-radius: 4px;
    min-width: 200px;
}

.controls button {
    padding: 8px 15px;
    border: none;
    border-radius: 4px;
    background-color: #007bff;
    color: white;
    cursor: pointer;
    transition: background-color 0.2s;
}

.controls button:hover {
    background-color: #0056b3;
}

.controls button.active {
    background-color: #28a745;
    font-weight: bold;
}

.tool-selection {
    display: flex;
    gap: 5px;
    align-items: center;
    margin-left: 10px;
    padding-left: 10px;
    border-left: 1px solid #ccc;
}

.tool-selection span {
    margin-right: 5px;
    font-weight: bold;
}

.canvas-container {
    width: fit-content;
    max-width: 100%;
    overflow: auto;
    border: 1px solid #ccc;
    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

canvas {
    display: block;
    cursor: crosshair;
}
</style>