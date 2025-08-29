import { BaseProjectService } from '../baseProjectService'
import apiClient from '@/services/apiClient'
import type { 
    ExportMetadata, 
    CocoExportRequest,
    ExportDownloadResponse
} from './export.types'

/**
 * Service for exporting annotated data in various formats
 */
export class ExportService extends BaseProjectService {
    constructor() {
        super('ExportService')
    }

    /**
     * Gets export metadata for a workflow stage
     */
    async getExportMetadata(projectId: number, workflowStageId: number): Promise<ExportMetadata> {
        const url = this.buildProjectUrl(projectId, `export/workflow-stages/${workflowStageId}/metadata`)
        return this.get<ExportMetadata>(url)
    }

    /**
     * Exports workflow stage data in COCO format (simple GET request)
     */
    async exportCoco(
        projectId: number, 
        workflowStageId: number,
        includeGroundTruth: boolean = true,
        includePredictions: boolean = false
    ): Promise<ExportDownloadResponse> {
        const url = this.buildProjectUrl(projectId, `export/workflow-stages/${workflowStageId}/coco`)
        const params = {
            includeGroundTruth: includeGroundTruth.toString(),
            includePredictions: includePredictions.toString()
        }

        try {
            const response = await apiClient.get(url, { 
                params,
                responseType: 'blob'
            })
            
            // Extract filename from Content-Disposition header
            const contentDisposition = response.headers['content-disposition'] || response.headers['Content-Disposition']
            let filename = 'coco_export.json'
            
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
                if (filenameMatch && filenameMatch[1]) {
                    filename = filenameMatch[1].replace(/['"]/g, '')
                }
            }

            return {
                data: response.data as Blob,
                filename,
                contentType: 'application/json'
            }
        } catch (error) {
            this.logger.error('Failed to export COCO data', error)
            throw error
        }
    }

    /**
     * Exports workflow stage data in COCO format with advanced options (POST request)
     */
    async exportCocoWithOptions(
        projectId: number,
        workflowStageId: number,
        exportRequest: CocoExportRequest
    ): Promise<ExportDownloadResponse> {
        const url = this.buildProjectUrl(projectId, `export/workflow-stages/${workflowStageId}/coco`)
        
        try {
            const response = await apiClient.post(url, exportRequest, {
                responseType: 'blob'
            })
            
            // Extract filename from Content-Disposition header
            const contentDisposition = response.headers['content-disposition'] || response.headers['Content-Disposition']
            let filename = exportRequest.fileName || 'coco_export.json'
            
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
                if (filenameMatch && filenameMatch[1]) {
                    filename = filenameMatch[1].replace(/['"]/g, '')
                }
            }

            if (!filename.toLowerCase().endsWith('.json')) {
                filename += '.json'
            }

            return {
                data: response.data as Blob,
                filename,
                contentType: 'application/json'
            }
        } catch (error) {
            this.logger.error('Failed to export COCO data with options', error)
            throw error
        }
    }

    /**
     * Downloads a blob as a file
     */
    private downloadBlob(blob: Blob, filename: string): void {
        const url = window.URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.download = filename
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        window.URL.revokeObjectURL(url)
    }

    /**
     * Downloads COCO export directly (convenience method)
     */
    async downloadCocoExport(
        projectId: number,
        workflowStageId: number,
        includeGroundTruth: boolean = true,
        includePredictions: boolean = false
    ): Promise<void> {
        const exportResponse = await this.exportCoco(
            projectId, 
            workflowStageId,
            includeGroundTruth,
            includePredictions
        )
        
        this.downloadBlob(exportResponse.data, exportResponse.filename)
    }

    /**
     * Downloads COCO export with options directly (convenience method)
     */
    async downloadCocoExportWithOptions(
        projectId: number,
        workflowStageId: number,
        exportRequest: CocoExportRequest
    ): Promise<void> {
        const exportResponse = await this.exportCocoWithOptions(
            projectId,
            workflowStageId,
            exportRequest
        )
        
        this.downloadBlob(exportResponse.data, exportResponse.filename)
    }
}