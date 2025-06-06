import type { Annotation } from '@/types/workspace/annotation';

// TODO
/**
 * Simulates saving an annotation to the backend.
 * In a real app, this would use fetch() or a library like axios to make a POST request.
 * @param annotation The annotation object to save. It will likely have a `clientId`.
 * @returns A Promise that resolves with the saved annotation, now including a real `annotationId` from the backend.
 */
export async function saveAnnotation(annotation: Annotation): Promise<Annotation> {
    console.log('[AnnotationService] Saving annotation to backend...', annotation);

    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 500));

    // Simulate a successful response from the backend
    const savedAnnotation: Annotation = {
        ...annotation,
        annotationId: Date.now(), // Generate a fake permanent ID
    };

    console.log('[AnnotationService] Annotation saved successfully!', savedAnnotation);

    // In case of a real API error, you would throw an error here:
    // if (!response.ok) { throw new Error('Failed to save annotation'); }

    return savedAnnotation;
}

/**
 * Simulates fetching all annotations for a given asset from the backend.
 * @param assetId The ID of the asset whose annotations are to be fetched.
 * @returns A Promise that resolves with an array of annotations.
 */
export async function fetchAnnotations(assetId: number | string): Promise<Annotation[]> {
    console.log(`[AnnotationService] Fetching annotations for asset ${assetId}...`);

    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 800));

    // For now, return an empty array. Later, this could return mock data.
    const annotations: Annotation[] = [];

    console.log('[AnnotationService] Fetched annotations successfully!', annotations);
    return annotations;
}