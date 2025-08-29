export { updateAnnotationPointLocally } from './coordinateTransforms';
export {
    isPointNearHandle,
    isPointNearLine,
    isPointInBoundingBox,
    isPointNearPolyline,
    isPointInPolygon,
    clampPointToImageBounds,
    isPointInImageBounds
} from './geometry';
export { type AnnotationHitResult, checkAnnotationHit, findAnnotationAtPoint } from './hitDetection';
export {
    type CanvasDisplaySettings,
    type ImageDimensions,
    type ViewState,
    calculateCanvasDimensions,
    calculateCenterAndFitView,
    mouseToImageCoordinates,
    calculateZoomFromWheel,
    calculateZoomViewOffset
} from './viewport';