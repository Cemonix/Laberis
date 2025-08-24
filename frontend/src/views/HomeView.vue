<template>
    <div class="home-container">
        <!-- Authenticated User Home -->
        <div v-if="authStore.isAuthenticated" class="authenticated-home">
            <section class="welcome-section">
                <div class="welcome-content">
                    <h1 class="welcome-title">Welcome back!</h1>
                    <p class="welcome-subtitle">Continue working on your annotation projects</p>
                </div>
            </section>
        </div>

        <!-- Non-authenticated User Home (Landing) -->
        <div v-else class="landing-home">
            <!-- Hero Section -->
            <section class="hero-section">
                <div class="hero-content">
                    <div class="hero-text">
                        <h1 class="hero-title">
                            Professional Data Annotation
                            <span class="hero-highlight">Made Simple</span>
                        </h1>
                        <p class="hero-subtitle">
                            Streamline your machine learning workflows with collaborative annotation tools, 
                            customizable pipelines, and secure data handling.
                        </p>
                        <div class="hero-actions">
                            <Button 
                                @click="navigateToLogin" 
                                variant="primary" 
                                class="hero-btn-primary"
                            >
                                Get Started
                            </Button>
                            <Button 
                                @click="navigateToRegister" 
                                variant="secondary" 
                                class="hero-btn-secondary"
                            >
                                Sign Up
                            </Button>
                        </div>
                    </div>
                    <div class="hero-visual">
                        <div class="hero-graphic">
                            <div class="annotation-preview">
                                <div class="canvas-mockup">
                                    <div class="bbox bbox-1"></div>
                                    <div class="bbox bbox-2"></div>
                                    <div class="point point-1"></div>
                                    <div class="point point-2"></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Features Section -->
            <section class="features-section">
                <div class="section-header">
                    <h2>Why Choose Laberis?</h2>
                    <p>Everything you need for professional data annotation workflows</p>
                </div>
                <div class="features-grid">
                    <Card class="feature-card">
                        <div class="feature-icon workflow"></div>
                        <h3>Workflow Engine</h3>
                        <p>Custom 2-3 stage workflows with task assignments, quality control, and progress tracking</p>
                    </Card>
                    <Card class="feature-card">
                        <div class="feature-icon collaboration"></div>
                        <h3>Team Collaboration</h3>
                        <p>Role-based permissions with Viewer, Annotator, Reviewer, and Manager roles</p>
                    </Card>
                    <Card class="feature-card">
                        <div class="feature-icon tools"></div>
                        <h3>Annotation Tools</h3>
                        <p>Points, bounding boxes, polylines, and polygons with precision drawing controls</p>
                    </Card>
                    <Card class="feature-card">
                        <div class="feature-icon security"></div>
                        <h3>Secure & Private</h3>
                        <p>On-premise deployment keeps your data in your control</p>
                    </Card>
                    <Card class="feature-card">
                        <div class="feature-icon storage"></div>
                        <h3>Scalable Storage</h3>
                        <p>S3-compatible object storage with automatic asset organization and metadata</p>
                    </Card>
                    <Card class="feature-card">
                        <div class="feature-icon analytics"></div>
                        <h3>Progress Analytics</h3>
                        <p>Real-time dashboards, quality metrics, and performance tracking</p>
                    </Card>
                </div>
            </section>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import Button from '@/components/common/Button.vue';
import Card from '@/components/common/Card.vue';

const router = useRouter();
const authStore = useAuthStore();

const navigateToLogin = () => {
    router.push('/login');
};

const navigateToRegister = () => {
    router.push('/register');
};
</script>

<style scoped>
.home-container {
    min-height: 100vh;
    background: linear-gradient(135deg, var(--color-gray-50) 0%, var(--color-gray-100) 100%);
}

/* Authenticated User Home */
.authenticated-home {
    max-width: 1200px;
    margin: 0 auto;
    padding: 2rem;
}

.welcome-section {
    text-align: center;
    padding: 3rem 0;
}

.welcome-content {
    max-width: 600px;
    margin: 0 auto;
}

.welcome-title {
    font-family: var(--font-family-heading);
    font-size: 2.5rem;
    font-weight: 600;
    margin-bottom: 1rem;
    color: var(--color-gray-900);
}

.welcome-subtitle {
    font-size: 1.2rem;
    color: var(--color-gray-600);
    margin-bottom: 2rem;
}

/* Hero Section */
.hero-section {
    padding: 4rem 2rem;
    max-width: 1200px;
    margin: 0 auto;
}

.hero-content {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 4rem;
    align-items: center;
}

.hero-text {
    max-width: 500px;
}

.hero-title {
    font-family: var(--font-family-heading);
    font-size: 3rem;
    font-weight: 700;
    line-height: 1.1;
    margin-bottom: 1.5rem;
    color: var(--color-gray-900);
}

.hero-highlight {
    color: var(--color-primary);
    display: block;
}

.hero-subtitle {
    font-size: 1.25rem;
    line-height: 1.6;
    color: var(--color-gray-700);
    margin-bottom: 2rem;
}

.hero-actions {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
}

.hero-btn-primary,
.hero-btn-secondary {
    padding: 0.75rem 2rem;
    font-size: 1.1rem;
    font-weight: 600;
}

/* Hero Visual */
.hero-visual {
    display: flex;
    justify-content: center;
    align-items: center;
}

.hero-graphic {
    width: 400px;
    height: 300px;
    position: relative;
}

.annotation-preview {
    width: 100%;
    height: 100%;
    background: var(--color-white);
    border-radius: 8px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
    overflow: hidden;
    position: relative;
}

.canvas-mockup {
    width: 100%;
    height: 100%;
    background: linear-gradient(45deg, var(--color-gray-100) 25%, transparent 25%),
                linear-gradient(-45deg, var(--color-gray-100) 25%, transparent 25%),
                linear-gradient(45deg, transparent 75%, var(--color-gray-100) 75%),
                linear-gradient(-45deg, transparent 75%, var(--color-gray-100) 75%);
    background-size: 20px 20px;
    background-position: 0 0, 0 10px, 10px -10px, -10px 0px;
    position: relative;
}

.bbox {
    position: absolute;
    border: 2px solid var(--color-primary);
    background: rgba(0, 123, 255, 0.1);
    border-radius: 2px;
}

.bbox-1 {
    top: 20%;
    left: 15%;
    width: 35%;
    height: 25%;
    animation: pulse-bbox 2s ease-in-out infinite;
}

.bbox-2 {
    top: 55%;
    left: 50%;
    width: 30%;
    height: 20%;
    animation: pulse-bbox 2s ease-in-out infinite 0.5s;
}

.point {
    position: absolute;
    width: 8px;
    height: 8px;
    background: var(--color-success);
    border: 2px solid var(--color-white);
    border-radius: 50%;
    box-shadow: 0 0 0 1px var(--color-success);
}

.point-1 {
    top: 30%;
    left: 70%;
    animation: pulse-point 1.5s ease-in-out infinite;
}

.point-2 {
    top: 75%;
    left: 25%;
    animation: pulse-point 1.5s ease-in-out infinite 0.3s;
}

@keyframes pulse-bbox {
    0%, 100% { opacity: 0.7; }
    50% { opacity: 1; }
}

@keyframes pulse-point {
    0%, 100% { transform: scale(1); }
    50% { transform: scale(1.2); }
}

/* Features Section */
.features-section {
    padding: 4rem 2rem;
    background: var(--color-white);
}

.section-header {
    text-align: center;
    max-width: 600px;
    margin: 0 auto 3rem;
}

.section-header h2 {
    font-family: var(--font-family-heading);
    font-size: 2.5rem;
    font-weight: 600;
    margin-bottom: 1rem;
    color: var(--color-gray-900);
}

.section-header p {
    font-size: 1.2rem;
    color: var(--color-gray-600);
}

.features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 2rem;
    max-width: 1200px;
    margin: 0 auto;
}

.feature-card {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.feature-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
}

.feature-icon {
    width: 60px;
    height: 60px;
    border-radius: 12px;
    margin-bottom: 1.5rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
}

.feature-icon.workflow {
    background: linear-gradient(135deg, var(--color-blue-500), var(--color-blue-600));
}

.feature-icon.workflow::before {
    content: "⚡";
    color: var(--color-white);
}

.feature-icon.collaboration {
    background: linear-gradient(135deg, var(--color-emerald-500), var(--color-emerald-600));
}

.feature-icon.collaboration::before {
    content: "👥";
    color: var(--color-white);
}

.feature-icon.tools {
    background: linear-gradient(135deg, var(--color-purple-500), var(--color-purple-600));
}

.feature-icon.tools::before {
    content: "🎯";
    color: var(--color-white);
}

.feature-icon.security {
    background: linear-gradient(135deg, var(--color-red-500), var(--color-red-600));
}

.feature-icon.security::before {
    content: "🔒";
    color: var(--color-white);
}

.feature-icon.storage {
    background: linear-gradient(135deg, var(--color-amber-500), var(--color-amber-600));
}

.feature-icon.storage::before {
    content: "💾";
    color: var(--color-white);
}

.feature-icon.analytics {
    background: linear-gradient(135deg, var(--color-turquoise-500), var(--color-turquoise-600));
}

.feature-icon.analytics::before {
    content: "📊";
    color: var(--color-white);
}

.feature-card h3 {
    font-family: var(--font-family-heading);
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: 0.75rem;
    color: var(--color-gray-900);
}

.feature-card p {
    color: var(--color-gray-600);
    line-height: 1.5;
}

/* Responsive Design */
@media (max-width: 768px) {
    .authenticated-home {
        padding: 1rem;
    }
    
    .welcome-title {
        font-size: 2rem;
    }
    
    .hero-content {
        grid-template-columns: 1fr;
        gap: 2rem;
        text-align: center;
    }
    
    .hero-title {
        font-size: 2.5rem;
    }
    
    .hero-graphic {
        width: 300px;
        height: 200px;
    }
    
    .features-grid {
        grid-template-columns: 1fr;
    }
    
    .getting-started-actions {
        flex-direction: column;
        align-items: center;
    }
}

@media (max-width: 480px) {
    .authenticated-home {
        padding: 1rem;
    }
    
    .welcome-section {
        padding: 2rem 0;
    }
    
    .welcome-title {
        font-size: 1.8rem;
    }
    
    .hero-section,
    .features-section,
    .getting-started-section {
        padding: 2rem 1rem;
    }
    
    .hero-title {
        font-size: 2rem;
    }
    
    .section-header h2 {
        font-size: 2rem;
    }
}
</style>