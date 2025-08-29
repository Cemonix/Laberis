import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { LogLevel } from '../logger.types';

// Mock env with basic configuration
vi.mock('@/config/env', () => ({
    env: {
        IS_PRODUCTION: false,
        LOGGING: {
            level: LogLevel.DEBUG,
            enableInProduction: false,
            enableTimestamps: false,
            enableServicePrefix: false,
        }
    }
}));

import { AppLogger } from '../logger';

describe('Logger Basic Functionality', () => {
    let consoleSpy: {
        debug: ReturnType<typeof vi.spyOn>;
        info: ReturnType<typeof vi.spyOn>;
        warn: ReturnType<typeof vi.spyOn>;
        error: ReturnType<typeof vi.spyOn>;
    };

    beforeEach(() => {
        // Mock console methods
        consoleSpy = {
            debug: vi.spyOn(console, 'debug').mockImplementation(() => {}),
            info: vi.spyOn(console, 'info').mockImplementation(() => {}),
            warn: vi.spyOn(console, 'warn').mockImplementation(() => {}),
            error: vi.spyOn(console, 'error').mockImplementation(() => {}),
        };
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    describe('Basic Logging', () => {
        it('should call debug method', () => {
            AppLogger.debug('debug message');
            expect(consoleSpy.debug).toHaveBeenCalledWith('debug message');
        });

        it('should call info method', () => {
            AppLogger.info('info message');
            expect(consoleSpy.info).toHaveBeenCalledWith('info message');
        });

        it('should call warn method', () => {
            AppLogger.warn('warn message');
            expect(consoleSpy.warn).toHaveBeenCalledWith('warn message');
        });

        it('should call error method', () => {
            AppLogger.error('error message');
            expect(consoleSpy.error).toHaveBeenCalledWith('error message');
        });
    });

    describe('Object Logging', () => {
        it('should log objects as second parameter', () => {
            const testObject = { key: 'value', number: 42 };
            
            AppLogger.info('test message', testObject);

            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenNthCalledWith(1, 'test message');
            expect(consoleSpy.info).toHaveBeenNthCalledWith(2, testObject);
        });

        it('should not log undefined objects', () => {
            AppLogger.info('test message', undefined);
            expect(consoleSpy.info).toHaveBeenCalledTimes(1);
            expect(consoleSpy.info).toHaveBeenCalledWith('test message');
        });

        it('should not log null objects', () => {
            AppLogger.info('test message', null);
            expect(consoleSpy.info).toHaveBeenCalledTimes(1);
            expect(consoleSpy.info).toHaveBeenCalledWith('test message');
        });

        it('should log falsy but valid objects like 0', () => {
            AppLogger.info('test message', 0);
            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenNthCalledWith(1, 'test message');
            expect(consoleSpy.info).toHaveBeenNthCalledWith(2, 0);
        });
    });

    describe('Service Logger', () => {
        it('should create service logger', () => {
            const serviceLogger = AppLogger.createServiceLogger('TestService');
            
            expect(serviceLogger).toBeDefined();
            expect(typeof serviceLogger.debug).toBe('function');
            expect(typeof serviceLogger.info).toBe('function');
            expect(typeof serviceLogger.warn).toBe('function');
            expect(typeof serviceLogger.error).toBe('function');
        });

        it('should use service logger methods', () => {
            const serviceLogger = AppLogger.createServiceLogger('TestService');
            
            serviceLogger.debug('debug from service');
            serviceLogger.info('info from service');
            serviceLogger.warn('warn from service');
            serviceLogger.error('error from service');

            // Since service prefix is disabled in our mock, these should just be plain messages
            expect(consoleSpy.debug).toHaveBeenCalledWith('debug from service');
            expect(consoleSpy.info).toHaveBeenCalledWith('info from service');
            expect(consoleSpy.warn).toHaveBeenCalledWith('warn from service');
            expect(consoleSpy.error).toHaveBeenCalledWith('error from service');
        });

        it('should handle objects in service logger', () => {
            const serviceLogger = AppLogger.createServiceLogger('TestService');
            const testObject = { serviceData: 'test' };

            serviceLogger.info('service message', testObject);

            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenNthCalledWith(1, 'service message');
            expect(consoleSpy.info).toHaveBeenNthCalledWith(2, testObject);
        });
    });

    describe('Edge Cases', () => {
        it('should handle empty messages', () => {
            AppLogger.info('');
            expect(consoleSpy.info).toHaveBeenCalledWith('');
        });

        it('should handle special characters', () => {
            const specialMessage = 'Message with \n newlines \t tabs and "quotes"';
            AppLogger.info(specialMessage);
            expect(consoleSpy.info).toHaveBeenCalledWith(specialMessage);
        });

        it('should handle very long messages', () => {
            const longMessage = 'A'.repeat(1000);
            AppLogger.info(longMessage);
            expect(consoleSpy.info).toHaveBeenCalledWith(longMessage);
        });

        it('should handle unicode characters', () => {
            const unicodeMessage = '🚀 Logger test with emojis 🎉 and ünicode chars';
            AppLogger.info(unicodeMessage);
            expect(consoleSpy.info).toHaveBeenCalledWith(unicodeMessage);
        });
    });

    describe('Multiple Calls', () => {
        it('should handle multiple rapid calls', () => {
            for (let i = 0; i < 10; i++) {
                AppLogger.info(`Message ${i}`);
            }

            expect(consoleSpy.info).toHaveBeenCalledTimes(10);
            for (let i = 0; i < 10; i++) {
                expect(consoleSpy.info).toHaveBeenNthCalledWith(i + 1, `Message ${i}`);
            }
        });

        it('should handle mixed log levels', () => {
            AppLogger.debug('debug 1');
            AppLogger.info('info 1');
            AppLogger.warn('warn 1');
            AppLogger.error('error 1');
            AppLogger.debug('debug 2');
            AppLogger.info('info 2');

            expect(consoleSpy.debug).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.warn).toHaveBeenCalledTimes(1);
            expect(consoleSpy.error).toHaveBeenCalledTimes(1);
        });
    });
});
