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
    </div>
</template>

<script setup lang="ts">
import Navbar from '@/layouts/Navbar.vue';
import Footer from '@/layouts/Footer.vue';
import AlertModal from '@/components/common/modal/AlertModal.vue';
import ConfirmModal from '@/components/common/modal/ConfirmModal.vue';
import ToastContainer from '@/components/common/toast/ToastContainer.vue';
import { useAlert } from '@/composables/useAlert';
import { useConfirm } from '@/composables/useConfirm';

const { isAlertOpen, alertTitle, alertMessage, handleAlertConfirm } = useAlert();
const { isConfirmOpen, confirmTitle, confirmMessage, handleConfirm, handleCancel } = useConfirm();

</script>

<style lang="scss" scoped>
@use "@/styles/variables" as vars;
@use "@/styles/variables/theme" as theme;

.default-layout {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    background-color: vars.$theme-background;
}

.main-content {
    position: relative;
    flex-grow: 1;
    padding: vars.$padding-medium;
    background-color: vars.$color-white;

    // Override padding for auth pages to provide full-screen experience
    &:has(.auth-container) {
        padding: 0;
    }
}
</style>