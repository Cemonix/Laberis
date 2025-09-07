import type { Point } from "@/core/geometry/geometry.types";

/**
 * Draws a point (e.g., a filled circle with a border) on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param x The x-coordinate on the canvas where the center of the point will be drawn.
 * @param y The y-coordinate on the canvas where the center of the point will be drawn.
 * @param color The fill color of the point (e.g., from the label).
 * @param radius The radius of the point in pixels.
 * @param borderColor The color of the border around the point.
 */
export function drawPoint(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    color: string = '#00FF00',
    radius: number = 4,
    lineWidth: number = 1,
    borderColor: string = '#000000'
) {
    ctx.save();

    ctx.beginPath();
    ctx.arc(x, y, radius, 0, 2 * Math.PI, false);

    ctx.fillStyle = color;
    ctx.fill();

    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = borderColor;
    ctx.stroke();

    ctx.restore();
}

/**
 * Draws a line on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param pointFrom The starting point of the line.
 * @param pointTo The ending point of the line.
 * @param color The color of the line.
 * @param lineWidth The width of the line.
 */
export function drawLine(
    ctx: CanvasRenderingContext2D,
    pointFrom: Point,
    pointTo: Point,
    color: string = '#00FF00',
    lineWidth: number = 2
) {

    ctx.save();

    ctx.beginPath();
    ctx.moveTo(pointFrom.x, pointFrom.y);
    ctx.lineTo(pointTo.x, pointTo.y);
    ctx.lineWidth = lineWidth;

    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}

/**
 * Draws a polyline on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param points An array of points representing the vertices of the polyline.
 * @param color The color of the polyline.
 * @param lineWidth The width of the polyline.
 */
export function drawPolyline(
    ctx: CanvasRenderingContext2D,
    points: Point[],
    color: string = '#00FF00',
    lineWidth: number = 2
) {
    if (!points || points.length === 0) return;
    if (points.length < 2) return;
    
    ctx.save();

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
    }

    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}

/**
 * Draws a polygon on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param points An array of points representing the vertices of the polygon.
 * @param color The color of the polygon border.
 */
export function drawPolygon(
    ctx: CanvasRenderingContext2D,
    points: Point[],
    color: string = '#00FF00',
    lineWidth: number = 2
) {
    if (!points || points.length === 0) return;
    if (points.length < 3) return; // A polygon requires at least 3 points

    ctx.save();

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.closePath();

    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}

/**
 * Draws a bounding box on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param x The x-coordinate of the top-left corner of the bounding box.
 * @param y The y-coordinate of the top-left corner of the bounding box.
 * @param width The width of the bounding box.
 * @param height The height of the bounding box.
 * @param color The color of the bounding box border.
 */
export function drawBoundingBox(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    width: number,
    height: number,
    color: string = '#00FF00',
    lineWidth: number = 2
) {
    ctx.save();

    ctx.beginPath();
    ctx.rect(x, y, width, height);
    
    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}

/**
 * Draws an edit handle (small square) at a point for annotation editing.
 * @param ctx The 2D rendering context of the canvas.
 * @param x The x-coordinate of the handle center.
 * @param y The y-coordinate of the handle center.
 * @param size The size of the handle (default 8px).
 * @param color The color of the handle.
 * @param isHovered Whether the handle is being hovered.
 */
export function drawEditHandle(
    ctx: CanvasRenderingContext2D,
    x: number,
    y: number,
    size: number = 8,
    color: string = '#FFFFFF',
    isHovered: boolean = false
) {
    const halfSize = size / 2;
    
    ctx.save();
    
    // Draw white background
    ctx.fillStyle = '#FFFFFF';
    ctx.fillRect(x - halfSize, y - halfSize, size, size);
    
    // Draw colored border
    ctx.strokeStyle = color;
    ctx.lineWidth = isHovered ? 3 : 2;
    ctx.strokeRect(x - halfSize, y - halfSize, size, size);
    
    // Draw inner dot if hovered
    if (isHovered) {
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(x, y, 2, 0, 2 * Math.PI);
        ctx.fill();
    }
    
    ctx.restore();
}