import type { ImageDimensions } from "@/types/image/imageDimensions";

export interface WorkspaceState {
    currentProjectId: string | null;
    currentAssetId: string | null;
    currentImageUrl: string | null;
    imageNaturalDimensions: ImageDimensions | null;
    canvasDisplayDimensions: ImageDimensions | null;
}
