<template>
    <div class="page-loader-overlay" :class="{ 'page-loader--transparent': transparent }">
        <div class="page-loader-container">
            <div class="page-loader-content">
                <!-- Animated loader -->
                <div class="loader-animation">
                    <div class="loader-spinner"></div>
                    <div class="loader-dots">
                        <span class="dot dot-1"></span>
                        <span class="dot dot-2"></span>
                        <span class="dot dot-3"></span>
                    </div>
                </div>
                
                <!-- Main message -->
                <h2 class="loader-title">{{ title }}</h2>
                
                <!-- Subtitle -->
                <p v-if="subtitle" class="loader-subtitle">{{ subtitle }}</p>
                
                <!-- Progress indicator -->
                <div v-if="showProgress" class="loader-progress">
                    <div class="progress-bar">
                        <div class="progress-fill" :style="{ width: `${progress}%` }"></div>
                    </div>
                    <span class="progress-text">{{ progress }}%</span>
                </div>
                
                <!-- Additional message -->
                <p v-if="message" class="loader-message">{{ message }}</p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
export interface Props {
    title?: string;
    subtitle?: string;
    message?: string;
    showProgress?: boolean;
    progress?: number;
    transparent?: boolean;
}

withDefaults(defineProps<Props>(), {
    title: 'Loading...',
    subtitle: '',
    message: '',
    showProgress: false,
    progress: 0,
    transparent: false
});
</script>

<style scoped>
.page-loader-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
    transition: opacity 0.3s ease;
}

.page-loader--transparent {
    background: rgba(255, 255, 255, 0.95);
    backdrop-filter: blur(8px);
}

.page-loader-container {
    text-align: center;
    max-width: 400px;
    padding: 2rem;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 16px;
    backdrop-filter: blur(20px);
    border: 1px solid rgba(255, 255, 255, 0.2);
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
}

.page-loader--transparent .page-loader-container {
    background: rgba(255, 255, 255, 0.9);
    color: var(--color-gray-800);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.page-loader-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1.5rem;
}

/* Loader Animation */
.loader-animation {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
}

.loader-spinner {
    width: 48px;
    height: 48px;
    border: 3px solid rgba(255, 255, 255, 0.3);
    border-top: 3px solid #fff;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

.page-loader--transparent .loader-spinner {
    border: 3px solid rgba(102, 126, 234, 0.3);
    border-top: 3px solid #667eea;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.loader-dots {
    display: flex;
    gap: 6px;
    align-items: center;
}

.dot {
    width: 8px;
    height: 8px;
    background: rgba(255, 255, 255, 0.8);
    border-radius: 50%;
    animation: pulse 1.5s ease-in-out infinite;
}

.page-loader--transparent .dot {
    background: #667eea;
}

.dot-1 { animation-delay: 0s; }
.dot-2 { animation-delay: 0.15s; }
.dot-3 { animation-delay: 0.3s; }

@keyframes pulse {
    0%, 80%, 100% { 
        transform: scale(0.8);
        opacity: 0.5;
    }
    40% { 
        transform: scale(1.2);
        opacity: 1;
    }
}

/* Text Styles */
.loader-title {
    font-size: 1.5rem;
    font-weight: 600;
    margin: 0;
    color: white;
    letter-spacing: -0.025em;
}

.page-loader--transparent .loader-title {
    color: var(--color-gray-800);
}

.loader-subtitle {
    font-size: 1rem;
    margin: 0;
    color: rgba(255, 255, 255, 0.9);
    font-weight: 400;
}

.page-loader--transparent .loader-subtitle {
    color: var(--color-gray-600);
}

.loader-message {
    font-size: 0.875rem;
    margin: 0;
    color: rgba(255, 255, 255, 0.8);
    line-height: 1.4;
}

.page-loader--transparent .loader-message {
    color: var(--color-gray-500);
}

/* Progress Bar */
.loader-progress {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    align-items: center;
}

.progress-bar {
    width: 100%;
    height: 4px;
    background: rgba(255, 255, 255, 0.3);
    border-radius: 2px;
    overflow: hidden;
}

.page-loader--transparent .progress-bar {
    background: rgba(102, 126, 234, 0.2);
}

.progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #fff, rgba(255, 255, 255, 0.8));
    border-radius: 2px;
    transition: width 0.3s ease;
    animation: shimmer 2s infinite;
}

.page-loader--transparent .progress-fill {
    background: linear-gradient(90deg, #667eea, #5a67d8);
}

@keyframes shimmer {
    0% { transform: translateX(-100%); }
    100% { transform: translateX(100%); }
}

.progress-text {
    font-size: 0.75rem;
    color: rgba(255, 255, 255, 0.8);
    font-weight: 500;
    min-width: 3rem;
    text-align: center;
}

.page-loader--transparent .progress-text {
    color: var(--color-gray-600);
}

/* Responsive */
@media (max-width: 480px) {
    .page-loader-container {
        margin: 1rem;
        padding: 1.5rem;
        max-width: none;
    }
    
    .loader-title {
        font-size: 1.25rem;
    }
    
    .loader-spinner {
        width: 40px;
        height: 40px;
    }
}

/* Dark mode support */
@media (prefers-color-scheme: dark) {
    .page-loader--transparent {
        background: rgba(31, 41, 55, 0.95);
    }
    
    .page-loader--transparent .page-loader-container {
        background: rgba(31, 41, 55, 0.9);
        color: var(--color-gray-100);
        border: 1px solid rgba(255, 255, 255, 0.1);
    }
    
    .page-loader--transparent .loader-title {
        color: var(--color-gray-100);
    }
    
    .page-loader--transparent .loader-subtitle {
        color: var(--color-gray-300);
    }
    
    .page-loader--transparent .loader-message {
        color: var(--color-gray-400);
    }
}

/* Accessibility */
@media (prefers-reduced-motion: reduce) {
    .loader-spinner,
    .dot,
    .progress-fill {
        animation: none;
    }
    
    .loader-spinner {
        border-top-color: transparent;
    }
}
</style>