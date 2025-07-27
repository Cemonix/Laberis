<template>
    <div class="default-layout">
        <header class="main-header">
            <Navbar />
        </header>
        <main class="main-content">
            <slot />
        </main>
        <Footer />

        <ConfirmModal
            :is-open="isConfirmOpen"
            :title="confirmTitle"
            :message="confirmMessage"
            @confirm="handleConfirm"
            @cancel="handleCancel"
        />

        <AlertModal
            :is-open="isAlertOpen"
            :title="alertTitle"
            :message="alertMessage"
            @confirm="handleAlertConfirm"
        />

        <ToastContainer />
        
        <!-- Teleport target for floating action buttons -->
        <div id="fab-container"></div>
    </div>
</template>

<script setup lang="ts">
import Navbar from '@/layouts/Navbar.vue';
import Footer from '@/layouts/Footer.vue';
import AlertModal from '@/components/common/modal/AlertModal.vue';
import ConfirmModal from '@/components/common/modal/ConfirmModal.vue';
import ToastContainer from '@/components/common/toast/ToastContainer.vue';
import {useAlert} from '@/composables/useAlert';
import {useConfirm} from '@/composables/useConfirm';

const { isAlertOpen, alertTitle, alertMessage, handleAlertConfirm } = useAlert();
const { isConfirmOpen, confirmTitle, confirmMessage, handleConfirm, handleCancel } = useConfirm();

</script>

<style lang="scss" scoped>
.default-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    background-color: var(--color-gray-50);
}

.main-content {
    position: relative;
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    padding: 1rem;
    background-color: var(--color-white);

    // Override padding for auth pages to provide full-screen experience
    &:has(.auth-container) {
        padding: 0;
    }
}

// Container for teleported floating action buttons
#fab-container {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 100;
    
    // Allow button interactions while keeping container non-interactive
    & > * {
        pointer-events: auto;
    }
}
</style>