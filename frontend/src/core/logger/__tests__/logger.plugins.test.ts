import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { createApp } from 'vue';
import { createPinia, setActivePinia } from 'pinia';
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

import { logger, piniaLogger } from '../logger';

describe('Logger Vue and Pinia Plugins', () => {
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

    describe('Vue Plugin', () => {
        it('should install logger methods on Vue app', () => {
            const app = createApp({});
            
            app.use(logger);

            // Check that global properties are added
            expect(app.config.globalProperties.$logDebug).toBeDefined();
            expect(app.config.globalProperties.$logInfo).toBeDefined();
            expect(app.config.globalProperties.$logWarn).toBeDefined();
            expect(app.config.globalProperties.$logError).toBeDefined();
        });

        it('should call logger through Vue global properties', () => {
            const app = createApp({});
            app.use(logger);

            app.config.globalProperties.$logDebug('debug from vue');
            app.config.globalProperties.$logInfo('info from vue');
            app.config.globalProperties.$logWarn('warn from vue');
            app.config.globalProperties.$logError('error from vue');

            expect(consoleSpy.debug).toHaveBeenCalledWith('debug from vue');
            expect(consoleSpy.info).toHaveBeenCalledWith('info from vue');
            expect(consoleSpy.warn).toHaveBeenCalledWith('warn from vue');
            expect(consoleSpy.error).toHaveBeenCalledWith('error from vue');
        });

        it('should handle objects through Vue global properties', () => {
            const app = createApp({});
            app.use(logger);

            const testObject = { vueData: 'test' };
            app.config.globalProperties.$logInfo('vue message', testObject);

            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenNthCalledWith(1, 'vue message');
            expect(consoleSpy.info).toHaveBeenNthCalledWith(2, testObject);
        });
    });

    describe('Pinia Plugin', () => {
        it('should return logger methods from plugin function', () => {
            const pluginResult = piniaLogger();
            
            expect(pluginResult.$logDebug).toBeDefined();
            expect(pluginResult.$logInfo).toBeDefined();
            expect(pluginResult.$logWarn).toBeDefined();
            expect(pluginResult.$logError).toBeDefined();
            
            expect(typeof pluginResult.$logDebug).toBe('function');
            expect(typeof pluginResult.$logInfo).toBe('function');
            expect(typeof pluginResult.$logWarn).toBe('function');
            expect(typeof pluginResult.$logError).toBe('function');
        });

        it('should call logger through Pinia plugin methods', () => {
            const pluginResult = piniaLogger();

            pluginResult.$logDebug('debug from pinia');
            pluginResult.$logInfo('info from pinia');
            pluginResult.$logWarn('warn from pinia');
            pluginResult.$logError('error from pinia');

            expect(consoleSpy.debug).toHaveBeenCalledWith('debug from pinia');
            expect(consoleSpy.info).toHaveBeenCalledWith('info from pinia');
            expect(consoleSpy.warn).toHaveBeenCalledWith('warn from pinia');
            expect(consoleSpy.error).toHaveBeenCalledWith('error from pinia');
        });

        it('should handle objects through Pinia plugin methods', () => {
            const pluginResult = piniaLogger();
            const testObject = { piniaData: 'test' };

            pluginResult.$logWarn('pinia warning', testObject);

            expect(consoleSpy.warn).toHaveBeenCalledTimes(2);
            expect(consoleSpy.warn).toHaveBeenNthCalledWith(1, 'pinia warning');
            expect(consoleSpy.warn).toHaveBeenNthCalledWith(2, testObject);
        });

        it('should work with Pinia store setup', () => {
            const pinia = createPinia();
            pinia.use(piniaLogger);
            setActivePinia(pinia);

            // Test that the plugin can be applied to Pinia without errors
            expect(() => pinia.use(piniaLogger)).not.toThrow();
        });
    });

    describe('Plugin Integration', () => {
        it('should work consistently between Vue and Pinia', () => {
            const app = createApp({});
            app.use(logger);

            const piniaPluginResult = piniaLogger();

            // Both should use the same underlying logger
            app.config.globalProperties.$logInfo('message from vue');
            piniaPluginResult.$logInfo('message from pinia');

            expect(consoleSpy.info).toHaveBeenCalledTimes(2);
            expect(consoleSpy.info).toHaveBeenNthCalledWith(1, 'message from vue');
            expect(consoleSpy.info).toHaveBeenNthCalledWith(2, 'message from pinia');
        });

        it('should maintain consistent behavior across plugins', () => {
            const app = createApp({});
            app.use(logger);
            const piniaPluginResult = piniaLogger();

            const testObject = { shared: 'data' };

            // Test both plugins handle objects the same way
            app.config.globalProperties.$logError('vue error', testObject);
            piniaPluginResult.$logError('pinia error', testObject);

            expect(consoleSpy.error).toHaveBeenCalledTimes(4); // 2 calls with 2 parameters each
            expect(consoleSpy.error).toHaveBeenNthCalledWith(1, 'vue error');
            expect(consoleSpy.error).toHaveBeenNthCalledWith(2, testObject);
            expect(consoleSpy.error).toHaveBeenNthCalledWith(3, 'pinia error');
            expect(consoleSpy.error).toHaveBeenNthCalledWith(4, testObject);
        });
    });
});
