import { AppLogger } from '@/utils/logger';
import type { LastProjectData } from '@/types/storage/lastProject';

const LAST_PROJECT_STORAGE_KEY = 'laberis_last_project';
const logger = AppLogger.createServiceLogger('LastProjectUtil');

/**
 * Utility for managing the user's last accessed project in localStorage
 */
export class LastProjectManager {
    /**
     * Save the current project as the user's last accessed project
     */
    static saveLastProject(projectId: number, projectName: string): void {
        try {
            const lastProjectData: LastProjectData = {
                projectId,
                projectName,
                lastAccessedAt: Date.now()
            };
            
            localStorage.setItem(LAST_PROJECT_STORAGE_KEY, JSON.stringify(lastProjectData));
            logger.info(`Saved last project: ${projectName} (ID: ${projectId})`);
        } catch (error) {
            logger.error('Failed to save last project to localStorage', error);
        }
    }

    /**
     * Get the user's last accessed project
     */
    static getLastProject(): LastProjectData | null {
        try {
            const stored = localStorage.getItem(LAST_PROJECT_STORAGE_KEY);
            if (!stored) {
                return null;
            }

            const parsed = JSON.parse(stored) as LastProjectData;
            
            // Validate the data structure
            if (!parsed.projectId || !parsed.projectName || !parsed.lastAccessedAt) {
                logger.warn('Invalid last project data found, clearing storage');
                this.clearLastProject();
                return null;
            }

            // Check if the data is too old (older than 30 days)
            const thirtyDaysAgo = Date.now() - (30 * 24 * 60 * 60 * 1000);
            if (parsed.lastAccessedAt < thirtyDaysAgo) {
                logger.info('Last project data is too old, clearing storage');
                this.clearLastProject();
                return null;
            }

            logger.info(`Retrieved last project: ${parsed.projectName} (ID: ${parsed.projectId})`);
            return parsed;
        } catch (error) {
            logger.error('Failed to retrieve last project from localStorage', error);
            this.clearLastProject();
            return null;
        }
    }

    /**
     * Clear the stored last project data
     */
    static clearLastProject(): void {
        try {
            localStorage.removeItem(LAST_PROJECT_STORAGE_KEY);
            logger.info('Cleared last project data');
        } catch (error) {
            logger.error('Failed to clear last project from localStorage', error);
        }
    }

    /**
     * Check if there's a valid last project stored
     */
    static hasLastProject(): boolean {
        return this.getLastProject() !== null;
    }
}