<template>
  <div class="error-page">
    <div class="error-container">
      <div class="error-icon">
        <font-awesome-icon
          v-if="errorType === 'not-found'"
          :icon="faFileCircleXmark"
          class="icon"
        />
        <font-awesome-icon
          v-else-if="errorType === 'forbidden'"
          :icon="faShieldHalved"
          class="icon"
        />
        <font-awesome-icon
          v-else-if="errorType === 'server-error'"
          :icon="faServer"
          class="icon"
        />
        <font-awesome-icon
          v-else
          :icon="faTriangleExclamation"
          class="icon"
        />
      </div>
      <div class="error-content">
        <h1 class="error-title">{{ errorTitle }}</h1>
        <p class="error-message">{{ displayMessage }}</p>
        <div v-if="errorCode" class="error-code">
          Error Code: {{ errorCode }}
        </div>
        <div class="error-actions">
          <Button
            v-if="canGoBack"
            @click="goBack"
            variant="primary"
          >
            Go Back
          </Button>
          <Button
            @click="goHome"
            variant="secondary"
          >
            Go to Home
          </Button>
        </div>
        <div v-if="showDetails" class="error-details">
          <button
            @click="toggleDetails"
            class="details-toggle"
          >
            {{ showDetailContent ? 'Hide' : 'Show' }} Details
          </button>
          <div v-if="showDetailContent" class="details-content">
            <pre>{{ errorDetails }}</pre>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import {useRoute, useRouter} from 'vue-router';
import Button from '@/components/common/Button.vue';
import {FontAwesomeIcon} from '@fortawesome/vue-fontawesome';
import {faFileCircleXmark, faServer, faShieldHalved, faTriangleExclamation} from '@fortawesome/free-solid-svg-icons';

const route = useRoute();
const router = useRouter();

const showDetailContent = ref(false);

// Extract error information from route
const errorType = computed(() => {
  const path = route.path;
  if (path.includes('not-found')) return 'not-found';
  if (path.includes('forbidden')) return 'forbidden';
  if (path.includes('server-error')) return 'server-error';
  return 'general';
});

const errorCode = computed(() => route.query.code as string || '');
const customMessage = computed(() => route.query.message as string || '');
const fromPath = computed(() => route.query.from as string || '');

const errorTitle = computed(() => {
  switch (errorType.value) {
    case 'not-found':
      return 'Page Not Found';
    case 'forbidden':
      return 'Access Denied';
    case 'server-error':
      return 'Server Error';
    default:
      return 'Something Went Wrong';
  }
});

const defaultMessage = computed(() => {
  switch (errorType.value) {
    case 'not-found':
      return 'The page you are looking for could not be found.';
    case 'forbidden':
      return 'You do not have permission to access this resource.';
    case 'server-error':
      return 'The server is experiencing technical difficulties. Please try again later.';
    default:
      return 'An unexpected error occurred. Please try again.';
  }
});

const displayMessage = computed(() => customMessage.value || defaultMessage.value);

const canGoBack = computed(() => {
  return fromPath.value && fromPath.value !== route.fullPath;
});

const showDetails = computed(() => {
  // Show details in development or if there's additional error information
  return process.env.NODE_ENV === 'development' || errorCode.value;
});

const errorDetails = computed(() => {
  const details: Record<string, any> = {
    errorType: errorType.value,
    path: route.path,
    timestamp: new Date().toISOString(),
  };
  
  if (errorCode.value) details.statusCode = errorCode.value;
  if (fromPath.value) details.fromPath = fromPath.value;
  if (customMessage.value) details.message = customMessage.value;
  
  return JSON.stringify(details, null, 2);
});

const toggleDetails = () => {
  showDetailContent.value = !showDetailContent.value;
};

const goBack = () => {
  if (canGoBack.value) {
    router.push(fromPath.value);
  } else {
    router.go(-1);
  }
};

const goHome = () => {
  router.push('/home');
};
</script>

<style scoped>
.error-page {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
    background: var(--color-gray-100);
    padding: 1.5rem;
}

.error-container {
    background: var(--color-white);
    border-radius: 16px;
    box-shadow: 0 1px 3px rgba(var(--color-black), 0.15);
    padding: 2rem;
  text-align: center;
  max-width: 500px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

.error-icon {
    margin-bottom: 1.5rem;
}

.icon {
  font-size: 5rem;
    color: var(--color-error);
}

.error-content {
    color: var(--color-text-secondary);
}

.error-title {
    font-size: 2rem;
    font-weight: 700;
    margin-bottom: 1rem;
    color: var(--color-text-primary);
    font-family: var(), sans-serif;
}

.error-message {
    font-size: 1.25rem;
    line-height: 1.5;
    margin-bottom: 1.5rem;
    color: var(--color-text-secondary);
    font-family: var(--font-family-body), sans-serif;
}

.error-code {
  font-family: monospace;
    font-size: 0.875rem;
    color: var(--color-error);
    background: var(--color-error-light);
    padding: 0.5rem 1rem;
    border-radius: 2px;
    margin-bottom: 1.5rem;
  display: inline-block;
    border: 1px solid var(--color-error);
}

.error-actions {
  display: flex;
    gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
    margin-bottom: 1.5rem;
}

.error-details {
    border-top: 1px solid var(--color-border-light);
    padding-top: 1.5rem;
}

.details-toggle {
  background: none;
  border: none;
    color: var(--color-primary);
  cursor: pointer;
    font-size: 0.875rem;
  text-decoration: underline;
    padding: 0.5rem;
    font-family: var(--font-family-body), sans-serif;
}

.details-toggle:hover {
    color: var(--color-primary-hover);
}

.details-content {
    margin-top: 1rem;
    background: var(--color-gray-50);
    border-radius: 2px;
    padding: 1rem;
  text-align: left;
}

.details-content pre {
    font-size: 0.75rem;
    color: var(--color-text-primary);
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: monospace;
}

@media (max-width: 640px) {
  .error-page {
      padding: 1rem;
  }
  
  .error-container {
      padding: 1.5rem;
    max-height: 95vh;
  }
  
  .error-title {
      font-size: 1.5rem;
  }
  
  .error-actions {
    flex-direction: column;
    align-items: center;
  }
}
</style>
