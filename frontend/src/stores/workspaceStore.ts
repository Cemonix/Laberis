import { defineStore } from "pinia";

import type { ImageDimensions } from "@/types/image/imageDimensions";
import type { WorkspaceState } from "@/types/workspace/workspaceState";

export const useWorkspaceStore = defineStore("workspace", {
    state: (): WorkspaceState => ({
        currentProjectId: null,
        currentAssetId: null,
        currentImageUrl: null,
        imageNaturalDimensions: null,
        canvasDisplayDimensions: null,
    }),

    getters: {
        getCurrentImageAspectRatio(): number | null {
            if (this.imageNaturalDimensions) {
                return (
                    this.imageNaturalDimensions.width /
                    this.imageNaturalDimensions.height
                );
            }
            return null;
        },
    },

    actions: {
        // Action to simulate loading asset information
        // Later, this will involve API calls
        async loadAsset(projectId: string, assetId: string) {
            this.currentProjectId = projectId;
            this.currentAssetId = assetId;

            // TODO: Replace with actual API call or more sophisticated logic
            this.currentImageUrl = `https://picsum.photos/800/600?random=${Math.random()}`;
            console.log(
                `[Store] Loaded asset: P:<span class="math-inline">\{projectId\}, A\:</span>{assetId}, URL: ${this.currentImageUrl}`
            );

            this.imageNaturalDimensions = null;
            // this.annotations = []; // Clear annotations for the new asset
        },

        setImageNaturalDimensions(dimensions: ImageDimensions) {
            this.imageNaturalDimensions = dimensions;
            console.log("[Store] Set image natural dimensions:", dimensions);
        },

        setCanvasDisplayDimensions(dimensions: ImageDimensions) {
            this.canvasDisplayDimensions = dimensions;
            console.log("[Store] Set canvas display dimensions:", dimensions);
        },
    },
});
