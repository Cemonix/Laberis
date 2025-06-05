/**
 * Draws a point (e.g., a filled circle with a border) on the canvas.
 * @param ctx The 2D rendering context of the canvas.
 * @param canvasX The x-coordinate on the canvas where the center of the point will be drawn.
 * @param canvasY The y-coordinate on the canvas where the center of the point will be drawn.
 * @param color The fill color of the point (e.g., from the label).
 * @param radius The radius of the point in pixels.
 * @param borderColor The color of the border around the point.
 */
export function drawPoint(
    ctx: CanvasRenderingContext2D,
    canvasX: number,
    canvasY: number,
    color: string = 'red',
    radius: number = 4,
    lineWidth: number = 1,
    borderColor: string = '#000000'
) {
    ctx.save();

    ctx.beginPath();
    ctx.arc(canvasX, canvasY, radius, 0, 2 * Math.PI, false);

    ctx.fillStyle = color;
    ctx.fill();

    ctx.lineWidth = lineWidth;
    ctx.strokeStyle = borderColor;
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
    color: string = 'blue'
) {
    ctx.save();

    ctx.beginPath();
    ctx.rect(x, y, width, height);
    
    ctx.lineWidth = 2;
    ctx.strokeStyle = color;
    ctx.stroke();

    ctx.restore();
}