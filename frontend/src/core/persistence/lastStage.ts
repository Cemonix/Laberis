import { AppLogger } from '@/core/logger/logger';
import type { LastStageData } from '@/core/persistence/lastStage.types';

const LAST_STAGE_STORAGE_KEY = 'laberis_last_stage';
const logger = AppLogger.createServiceLogger('LastStageUtil');

/**
 * Utility for managing the user's last accessed workflow stage in localStorage
 */
export class LastStageManager {
    /**
     * Save the current workflow stage as the user's last accessed stage
     */
    static saveLastStage(
        projectId: number,
        workflowId: number,
        stageId: number,
        stageName: string,
        projectName: string
    ): void {
        try {
            const lastStageData: LastStageData = {
                projectId,
                workflowId,
                stageId,
                stageName,
                projectName,
                lastAccessedAt: Date.now()
            };
            
            localStorage.setItem(LAST_STAGE_STORAGE_KEY, JSON.stringify(lastStageData));
            logger.info(`Saved last stage: ${stageName} in project ${projectName} (Stage ID: ${stageId})`);
        } catch (error) {
            logger.error('Failed to save last stage to localStorage', error);
        }
    }

    /**
     * Get the user's last accessed workflow stage
     */
    static getLastStage(): LastStageData | null {
        try {
            const stored = localStorage.getItem(LAST_STAGE_STORAGE_KEY);
            if (!stored) {
                return null;
            }

            const parsed = JSON.parse(stored) as LastStageData;
            
            // Validate the data structure
            if (!parsed.projectId || !parsed.workflowId || !parsed.stageId || 
                !parsed.stageName || !parsed.projectName || !parsed.lastAccessedAt) {
                logger.warn('Invalid last stage data found, clearing storage');
                this.clearLastStage();
                return null;
            }

            // Check if the data is too old (older than 7 days for stages)
            const sevenDaysAgo = Date.now() - (7 * 24 * 60 * 60 * 1000);
            if (parsed.lastAccessedAt < sevenDaysAgo) {
                logger.info('Last stage data is too old, clearing storage');
                this.clearLastStage();
                return null;
            }

            logger.info(`Retrieved last stage: ${parsed.stageName} in project ${parsed.projectName}`);
            return parsed;
        } catch (error) {
            logger.error('Failed to retrieve last stage from localStorage', error);
            this.clearLastStage();
            return null;
        }
    }

    /**
     * Clear the stored last stage data
     */
    static clearLastStage(): void {
        try {
            localStorage.removeItem(LAST_STAGE_STORAGE_KEY);
            logger.info('Cleared last stage data');
        } catch (error) {
            logger.error('Failed to clear last stage from localStorage', error);
        }
    }

    /**
     * Check if there's a valid last stage stored
     */
    static hasLastStage(): boolean {
        return this.getLastStage() !== null;
    }

    /**
     * Check if the last stage belongs to the specified project
     */
    static isLastStageFromProject(projectId: number): boolean {
        const lastStage = this.getLastStage();
        return lastStage?.projectId === projectId;
    }
}