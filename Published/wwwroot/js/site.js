/**
 * Enterprise UI - Global JavaScript
 * Central Sync Service - Box Tracking Application
 * ================================================
 */

// =========================================
// Toast Notification System
// =========================================

const Toast = {
    container: null,
    
    init() {
        // Create toast container if it doesn't exist
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.id = 'toast-container';
            this.container.className = 'fixed bottom-4 right-4 z-[60] space-y-2 pointer-events-none';
            this.container.setAttribute('aria-live', 'polite');
            this.container.setAttribute('aria-atomic', 'true');
            document.body.appendChild(this.container);
        }
    },
    
    show(options) {
        this.init();
        
        const {
            title = '',
            message = '',
            type = 'info', // success, warning, danger, info
            duration = 5000,
            dismissible = true
        } = options;
        
        // Icon SVGs for each type
        const icons = {
            success: `<svg class="h-5 w-5 text-emerald-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
            </svg>`,
            warning: `<svg class="h-5 w-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
            </svg>`,
            danger: `<svg class="h-5 w-5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
            </svg>`,
            info: `<svg class="h-5 w-5 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
            </svg>`
        };
        
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `
            pointer-events-auto flex items-start gap-3 p-4 
            bg-white rounded-lg shadow-lg border border-neutral-200 
            max-w-sm w-full transform translate-x-full opacity-0
            transition-all duration-300 ease-out
        `;
        toast.setAttribute('role', 'alert');
        
        toast.innerHTML = `
            <div class="flex-shrink-0">
                ${icons[type] || icons.info}
            </div>
            <div class="flex-1 min-w-0">
                ${title ? `<p class="text-sm font-medium text-neutral-900">${title}</p>` : ''}
                ${message ? `<p class="text-sm text-neutral-500 ${title ? 'mt-1' : ''}">${message}</p>` : ''}
            </div>
            ${dismissible ? `
                <button type="button" class="flex-shrink-0 text-neutral-400 hover:text-neutral-600 transition-colors">
                    <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                    </svg>
                </button>
            ` : ''}
        `;
        
        // Add dismiss handler
        if (dismissible) {
            const closeBtn = toast.querySelector('button');
            closeBtn.addEventListener('click', () => this.dismiss(toast));
        }
        
        // Add to container
        this.container.appendChild(toast);
        
        // Trigger animation
        requestAnimationFrame(() => {
            toast.classList.remove('translate-x-full', 'opacity-0');
            toast.classList.add('translate-x-0', 'opacity-100');
        });
        
        // Auto dismiss
        if (duration > 0) {
            setTimeout(() => this.dismiss(toast), duration);
        }
        
        return toast;
    },
    
    dismiss(toast) {
        toast.classList.add('translate-x-full', 'opacity-0');
        setTimeout(() => toast.remove(), 300);
    },
    
    success(title, message) {
        return this.show({ title, message, type: 'success' });
    },
    
    warning(title, message) {
        return this.show({ title, message, type: 'warning' });
    },
    
    error(title, message) {
        return this.show({ title, message, type: 'danger' });
    },
    
    info(title, message) {
        return this.show({ title, message, type: 'info' });
    }
};


// =========================================
// Accessible Dropdown Menu
// =========================================

class DropdownMenu {
    constructor(container) {
        this.container = container;
        this.trigger = container.querySelector('[data-dropdown-trigger]');
        this.menu = container.querySelector('[data-dropdown-menu]');
        this.items = this.menu.querySelectorAll('[data-dropdown-item]');
        this.isOpen = false;
        this.currentIndex = -1;
        
        this.init();
    }
    
    init() {
        // Click to toggle
        this.trigger.addEventListener('click', (e) => {
            e.stopPropagation();
            this.toggle();
        });
        
        // Keyboard navigation
        this.trigger.addEventListener('keydown', (e) => this.handleTriggerKeydown(e));
        this.menu.addEventListener('keydown', (e) => this.handleMenuKeydown(e));
        
        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.close();
            }
        });
        
        // Close on Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isOpen) {
                this.close();
                this.trigger.focus();
            }
        });
    }
    
    toggle() {
        this.isOpen ? this.close() : this.open();
    }
    
    open() {
        this.isOpen = true;
        this.menu.classList.remove('hidden');
        this.trigger.setAttribute('aria-expanded', 'true');
        
        // Focus first item
        this.currentIndex = 0;
        this.items[0]?.focus();
    }
    
    close() {
        this.isOpen = false;
        this.menu.classList.add('hidden');
        this.trigger.setAttribute('aria-expanded', 'false');
        this.currentIndex = -1;
    }
    
    handleTriggerKeydown(e) {
        if (['ArrowDown', 'Enter', ' '].includes(e.key)) {
            e.preventDefault();
            this.open();
        }
    }
    
    handleMenuKeydown(e) {
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.focusNext();
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.focusPrev();
                break;
            case 'Home':
                e.preventDefault();
                this.focusFirst();
                break;
            case 'End':
                e.preventDefault();
                this.focusLast();
                break;
            case 'Tab':
                this.close();
                break;
        }
    }
    
    focusNext() {
        this.currentIndex = (this.currentIndex + 1) % this.items.length;
        this.items[this.currentIndex].focus();
    }
    
    focusPrev() {
        this.currentIndex = this.currentIndex <= 0 ? this.items.length - 1 : this.currentIndex - 1;
        this.items[this.currentIndex].focus();
    }
    
    focusFirst() {
        this.currentIndex = 0;
        this.items[0].focus();
    }
    
    focusLast() {
        this.currentIndex = this.items.length - 1;
        this.items[this.currentIndex].focus();
    }
}


// =========================================
// Data Table Enhancements
// =========================================

class DataTable {
    constructor(table, options = {}) {
        this.table = table;
        this.options = {
            sortable: true,
            ...options
        };
        
        if (this.options.sortable) {
            this.initSorting();
        }
    }
    
    initSorting() {
        const headers = this.table.querySelectorAll('th[data-sortable]');
        
        headers.forEach(header => {
            header.classList.add('cursor-pointer', 'select-none');
            header.setAttribute('role', 'columnheader');
            header.setAttribute('aria-sort', 'none');
            
            // Add sort icon
            const icon = document.createElement('span');
            icon.className = 'ml-1 inline-block text-neutral-400';
            icon.innerHTML = `<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"/>
            </svg>`;
            header.appendChild(icon);
            
            header.addEventListener('click', () => this.sort(header));
        });
    }
    
    sort(header) {
        const column = header.dataset.sortable;
        const tbody = this.table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        
        // Determine sort direction
        const currentSort = header.getAttribute('aria-sort');
        const newSort = currentSort === 'ascending' ? 'descending' : 'ascending';
        
        // Reset all headers
        this.table.querySelectorAll('th[data-sortable]').forEach(th => {
            th.setAttribute('aria-sort', 'none');
            th.classList.remove('bg-neutral-100');
        });
        
        // Set current header
        header.setAttribute('aria-sort', newSort);
        header.classList.add('bg-neutral-100');
        
        // Sort rows
        const columnIndex = Array.from(header.parentNode.children).indexOf(header);
        
        rows.sort((a, b) => {
            const aValue = a.cells[columnIndex]?.textContent.trim() || '';
            const bValue = b.cells[columnIndex]?.textContent.trim() || '';
            
            // Try numeric sort
            const aNum = parseFloat(aValue.replace(/[^\d.-]/g, ''));
            const bNum = parseFloat(bValue.replace(/[^\d.-]/g, ''));
            
            if (!isNaN(aNum) && !isNaN(bNum)) {
                return newSort === 'ascending' ? aNum - bNum : bNum - aNum;
            }
            
            // Fall back to string sort
            return newSort === 'ascending' 
                ? aValue.localeCompare(bValue)
                : bValue.localeCompare(aValue);
        });
        
        // Re-append sorted rows
        rows.forEach(row => tbody.appendChild(row));
    }
}


// =========================================
// Form Utilities
// =========================================

const FormUtils = {
    // Disable form during submission
    disableForm(form) {
        const elements = form.querySelectorAll('input, select, textarea, button');
        elements.forEach(el => {
            el.disabled = true;
            if (el.type === 'submit') {
                el.classList.add('btn-loading');
                el.dataset.originalText = el.textContent;
                el.textContent = 'Processing...';
            }
        });
    },
    
    // Re-enable form
    enableForm(form) {
        const elements = form.querySelectorAll('input, select, textarea, button');
        elements.forEach(el => {
            el.disabled = false;
            if (el.type === 'submit') {
                el.classList.remove('btn-loading');
                if (el.dataset.originalText) {
                    el.textContent = el.dataset.originalText;
                }
            }
        });
    },
    
    // Show inline validation error
    showError(input, message) {
        input.classList.add('is-invalid');
        
        // Remove existing error
        const existingError = input.parentNode.querySelector('.form-error');
        if (existingError) existingError.remove();
        
        // Add new error
        const error = document.createElement('p');
        error.className = 'form-error';
        error.textContent = message;
        input.parentNode.appendChild(error);
    },
    
    // Clear validation error
    clearError(input) {
        input.classList.remove('is-invalid');
        const error = input.parentNode.querySelector('.form-error');
        if (error) error.remove();
    }
};


// =========================================
// Utility Functions
// =========================================

// Debounce function for performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Format number with locale
function formatNumber(num) {
    return new Intl.NumberFormat().format(num);
}

// Format date relative
function formatRelativeTime(date) {
    const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });
    const now = new Date();
    const diff = date - now;
    
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (Math.abs(days) > 0) return rtf.format(days, 'day');
    if (Math.abs(hours) > 0) return rtf.format(hours, 'hour');
    if (Math.abs(minutes) > 0) return rtf.format(minutes, 'minute');
    return rtf.format(seconds, 'second');
}


// =========================================
// Initialize on DOM Ready
// =========================================

document.addEventListener('DOMContentLoaded', () => {
    // Initialize dropdowns
    document.querySelectorAll('[data-dropdown]').forEach(dropdown => {
        new DropdownMenu(dropdown);
    });
    
    // Initialize data tables
    document.querySelectorAll('[data-datatable]').forEach(table => {
        new DataTable(table);
    });
    
    // Form submission handling
    document.querySelectorAll('form[data-ajax]').forEach(form => {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            FormUtils.disableForm(form);
            
            try {
                const response = await fetch(form.action, {
                    method: form.method || 'POST',
                    body: new FormData(form),
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });
                
                const data = await response.json();
                
                if (data.success) {
                    Toast.success('Success', data.message || 'Operation completed');
                    if (data.redirectUrl) {
                        window.location.href = data.redirectUrl;
                    }
                } else {
                    Toast.error('Error', data.message || 'An error occurred');
                }
            } catch (error) {
                Toast.error('Error', 'Network error. Please try again.');
                console.error('Form submission error:', error);
            } finally {
                FormUtils.enableForm(form);
            }
        });
    });
});


// =========================================
// Export for global use
// =========================================

window.Toast = Toast;
window.FormUtils = FormUtils;
window.DataTable = DataTable;
window.DropdownMenu = DropdownMenu;
window.debounce = debounce;
window.formatNumber = formatNumber;
window.formatRelativeTime = formatRelativeTime;
