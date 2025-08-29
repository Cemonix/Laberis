/**
 * Export metadata response
 */
export interface ExportMetadata {
    completedTasksCount: number
    annotationsCount: number
    annotatedAssetsCount: number
    categoriesCount: number
    workflowStageName: string
    projectName: string
    availableFormats: string[]
}

/**
 * COCO export request configuration
 */
export interface CocoExportRequest {
    includeGroundTruth?: boolean
    includePredictions?: boolean
    fileName?: string
    description?: string
    contributor?: string
    taskIds?: number[]
    labelIds?: number[]
}

/**
 * Export format types
 */
export enum ExportFormat {
    COCO = 'COCO'
}

/**
 * Export download response
 */
export interface ExportDownloadResponse {
    data: Blob
    filename: string
    contentType: string
}