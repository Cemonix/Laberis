/**
 * Get display color for annotation based on selection and hover state
 */
export const getAnnotationDisplayColor = (
    baseColor: string,
    isSelected: boolean,
    isHovered: boolean
): string => {
    // Brighten color if hovered but not selected
    return isHovered && !isSelected ? '#FFFFFF' : baseColor;
};

/**
 * Separate annotations into selected and non-selected groups for layered rendering
 */
export const separateAnnotationsBySelection = (
    annotations: any[],
    selectedAnnotationId: number | null
) => {
    const selectedAnnotation = annotations.find(a => a.annotationId === selectedAnnotationId);
    const otherAnnotations = annotations.filter(a => a.annotationId !== selectedAnnotationId);
    
    return {
        selectedAnnotation,
        otherAnnotations
    };
};