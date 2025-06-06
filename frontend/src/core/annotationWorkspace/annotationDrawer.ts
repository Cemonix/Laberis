import type { Point } from "@/types/common/point";

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
    lineWidth: number = 1
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
    lineWidth: number = 1
) {
    ctx.save();

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (const point of points) {
        ctx.lineTo(point.x, point.y);
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
    color: string = '#00FF00'
) {
    if (points.length < 3) return; // A polygon requires at least 3 points

    ctx.save();

    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (const point of points.slice(1)) {
        ctx.lineTo(point.x, point.y);
    }
    ctx.closePath();

    ctx.lineWidth = 2;
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
    color: string = '#00FF00'
) {
    ctx.save();

    ctx.beginPath();
    ctx.rect(x, y, width, height);
    
    ctx.lineWidth = 2;
    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}