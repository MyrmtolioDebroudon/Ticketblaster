// SignalR Real-time Updates Client
class TicketBlasterSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.currentEventId = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000;
        this.eventCallbacks = new Map();
    }

    // Initialize SignalR connection
    async initialize() {
        try {
            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/events")
                .configureLogging(signalR.LogLevel.Information)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                })
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            console.log('SignalR Connected successfully');
            
            // Join admin group if user is admin
            if (this.isAdmin()) {
                await this.joinAdminGroup();
            }

            // Join event group if on event page
            const eventId = this.getCurrentEventId();
            if (eventId) {
                await this.joinEventGroup(eventId);
            }

        } catch (error) {
            console.error('SignalR Connection failed:', error);
            this.isConnected = false;
            await this.attemptReconnect();
        }
    }

    // Set up event handlers
    setupEventHandlers() {
        // Connection events
        this.connection.onreconnecting(() => {
            console.log('SignalR Reconnecting...');
            this.isConnected = false;
            this.showConnectionStatus('Reconnecting...', 'warning');
        });

        this.connection.onreconnected(() => {
            console.log('SignalR Reconnected successfully');
            this.isConnected = true;
            this.showConnectionStatus('Connected', 'success');
            
            // Rejoin groups
            this.rejoinGroups();
        });

        this.connection.onclose(() => {
            console.log('SignalR Connection closed');
            this.isConnected = false;
            this.showConnectionStatus('Disconnected', 'danger');
        });

        // Event update handlers
        this.connection.on('EventUpdated', (eventData) => {
            this.handleEventUpdated(eventData);
        });

        this.connection.on('TicketAvailabilityChanged', (data) => {
            this.handleTicketAvailabilityChanged(data);
        });

        this.connection.on('NewOrder', (orderData) => {
            this.handleNewOrder(orderData);
        });

        this.connection.on('TicketPurchased', (purchaseData) => {
            this.handleTicketPurchased(purchaseData);
        });

        this.connection.on('EventStatusChanged', (statusData) => {
            this.handleEventStatusChanged(statusData);
        });

        this.connection.on('PriceChanged', (priceData) => {
            this.handlePriceChanged(priceData);
        });

        this.connection.on('AdminNotification', (message, data) => {
            this.handleAdminNotification(message, data);
        });

        this.connection.on('UserNotification', (message, data) => {
            this.handleUserNotification(message, data);
        });
    }

    // Join event group
    async joinEventGroup(eventId) {
        if (!this.isConnected) return;
        
        try {
            await this.connection.invoke('JoinEventGroup', eventId.toString());
            this.currentEventId = eventId;
            console.log(`Joined event group: ${eventId}`);
        } catch (error) {
            console.error('Failed to join event group:', error);
        }
    }

    // Leave event group
    async leaveEventGroup(eventId) {
        if (!this.isConnected) return;
        
        try {
            await this.connection.invoke('LeaveEventGroup', eventId.toString());
            if (this.currentEventId === eventId) {
                this.currentEventId = null;
            }
            console.log(`Left event group: ${eventId}`);
        } catch (error) {
            console.error('Failed to leave event group:', error);
        }
    }

    // Join admin group
    async joinAdminGroup() {
        if (!this.isConnected) return;
        
        try {
            await this.connection.invoke('JoinAdminGroup');
            console.log('Joined admin group');
        } catch (error) {
            console.error('Failed to join admin group:', error);
        }
    }

    // Event handlers
    handleEventUpdated(eventData) {
        console.log('Event updated:', eventData);
        
        // Update event details if on event page
        if (this.currentEventId === eventData.EventId) {
            this.updateEventDetails(eventData);
        }
        
        // Update event cards in listings
        this.updateEventCard(eventData);
        
        // Call registered callbacks
        this.callEventCallbacks('eventUpdated', eventData);
    }

    handleTicketAvailabilityChanged(data) {
        console.log('Ticket availability changed:', data);
        
        // Update quantity displays
        const quantityElements = document.querySelectorAll(`[data-ticket-type="${data.TicketTypeId}"] .available-quantity`);
        quantityElements.forEach(element => {
            element.textContent = data.RemainingQuantity;
            
            // Add visual indicator
            element.classList.add('quantity-updated');
            setTimeout(() => element.classList.remove('quantity-updated'), 2000);
        });

        // Update availability status
        if (data.RemainingQuantity === 0) {
            this.markTicketTypeAsSoldOut(data.TicketTypeId);
        }
        
        // Show notification
        this.showNotification(`Ticket availability updated: ${data.RemainingQuantity} remaining`, 'info');
        
        this.callEventCallbacks('ticketAvailabilityChanged', data);
    }

    handleNewOrder(orderData) {
        console.log('New order:', orderData);
        
        // Update order statistics if on dashboard
        this.updateOrderStatistics();
        
        // Show notification to admins
        if (this.isAdmin()) {
            this.showNotification(`New order #${orderData.OrderId} - $${orderData.TotalAmount}`, 'success');
        }
        
        this.callEventCallbacks('newOrder', orderData);
    }

    handleTicketPurchased(purchaseData) {
        console.log('Ticket purchased:', purchaseData);
        
        // Update viewer count
        this.updateViewerCount(purchaseData.ViewersCount);
        
        // Show purchase notification
        this.showPurchaseNotification(purchaseData);
        
        this.callEventCallbacks('ticketPurchased', purchaseData);
    }

    handleEventStatusChanged(statusData) {
        console.log('Event status changed:', statusData);
        
        // Update status display
        const statusElements = document.querySelectorAll('.event-status');
        statusElements.forEach(element => {
            element.textContent = statusData.Status;
            element.className = `event-status status-${statusData.Status.toLowerCase()}`;
        });
        
        this.callEventCallbacks('eventStatusChanged', statusData);
    }

    handlePriceChanged(priceData) {
        console.log('Price changed:', priceData);
        
        // Update price displays
        const priceElements = document.querySelectorAll(`[data-ticket-type="${priceData.TicketTypeId}"] .ticket-price`);
        priceElements.forEach(element => {
            element.textContent = `$${priceData.NewPrice.toFixed(2)}`;
            element.classList.add('price-updated');
            setTimeout(() => element.classList.remove('price-updated'), 2000);
        });
        
        // Show notification
        this.showNotification(`Price updated: $${priceData.NewPrice.toFixed(2)}`, 'info');
        
        this.callEventCallbacks('priceChanged', priceData);
    }

    handleAdminNotification(message, data) {
        console.log('Admin notification:', message, data);
        
        if (this.isAdmin()) {
            this.showNotification(message, 'warning', 10000);
        }
        
        this.callEventCallbacks('adminNotification', { message, data });
    }

    handleUserNotification(message, data) {
        console.log('User notification:', message, data);
        
        this.showNotification(message, 'info', 5000);
        
        this.callEventCallbacks('userNotification', { message, data });
    }

    // Helper methods
    async attemptReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            this.showConnectionStatus('Connection failed', 'danger');
            return;
        }

        this.reconnectAttempts++;
        console.log(`Attempting to reconnect... (${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
        
        setTimeout(async () => {
            try {
                await this.initialize();
            } catch (error) {
                console.error('Reconnection failed:', error);
                await this.attemptReconnect();
            }
        }, this.reconnectDelay * this.reconnectAttempts);
    }

    async rejoinGroups() {
        if (this.isAdmin()) {
            await this.joinAdminGroup();
        }
        
        if (this.currentEventId) {
            await this.joinEventGroup(this.currentEventId);
        }
    }

    getCurrentEventId() {
        const params = new URLSearchParams(window.location.search);
        return params.get('id');
    }

    isAdmin() {
        // Check if user has admin role (implement based on your auth system)
        return localStorage.getItem('userRole') === 'Admin' || 
               localStorage.getItem('userRole') === 'Organizer';
    }

    showConnectionStatus(message, type) {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `connection-status ${type}`;
        }
    }

    showNotification(message, type = 'info', duration = 5000) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;

        // Add to notification container
        let container = document.getElementById('notifications');
        if (!container) {
            container = document.createElement('div');
            container.id = 'notifications';
            container.className = 'notifications-container';
            document.body.appendChild(container);
        }

        container.appendChild(notification);

        // Auto-remove after duration
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, duration);
    }

    updateEventDetails(eventData) {
        // Update event title
        const titleElement = document.getElementById('eventTitle');
        if (titleElement) {
            titleElement.textContent = eventData.Title;
        }

        // Update event description
        const descriptionElement = document.getElementById('eventDescription');
        if (descriptionElement) {
            descriptionElement.textContent = eventData.Description;
        }

        // Update date/time
        const dateTimeElement = document.getElementById('eventDateTime');
        if (dateTimeElement) {
            dateTimeElement.textContent = new Date(eventData.StartDate).toLocaleString();
        }
    }

    updateEventCard(eventData) {
        const eventCards = document.querySelectorAll(`[data-event-id="${eventData.EventId}"]`);
        eventCards.forEach(card => {
            const titleElement = card.querySelector('.event-title');
            if (titleElement) {
                titleElement.textContent = eventData.Title;
            }

            const locationElement = card.querySelector('.event-location');
            if (locationElement) {
                locationElement.textContent = eventData.Location;
            }
        });
    }

    markTicketTypeAsSoldOut(ticketTypeId) {
        const ticketElements = document.querySelectorAll(`[data-ticket-type="${ticketTypeId}"]`);
        ticketElements.forEach(element => {
            element.classList.add('sold-out');
            
            const buttons = element.querySelectorAll('button');
            buttons.forEach(button => {
                button.disabled = true;
                button.textContent = 'Sold Out';
            });
        });
    }

    showPurchaseNotification(purchaseData) {
        const message = `${purchaseData.Quantity} ticket(s) just purchased!`;
        this.showNotification(message, 'success', 3000);
    }

    updateViewerCount(count) {
        const viewerElements = document.querySelectorAll('.viewer-count');
        viewerElements.forEach(element => {
            element.textContent = `${count} viewing now`;
        });
    }

    updateOrderStatistics() {
        // Reload order statistics on dashboard
        if (typeof loadOrderStatistics === 'function') {
            loadOrderStatistics();
        }
    }

    // Register event callbacks
    addEventListener(eventType, callback) {
        if (!this.eventCallbacks.has(eventType)) {
            this.eventCallbacks.set(eventType, []);
        }
        this.eventCallbacks.get(eventType).push(callback);
    }

    removeEventListener(eventType, callback) {
        if (this.eventCallbacks.has(eventType)) {
            const callbacks = this.eventCallbacks.get(eventType);
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    callEventCallbacks(eventType, data) {
        if (this.eventCallbacks.has(eventType)) {
            this.eventCallbacks.get(eventType).forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Error in event callback for ${eventType}:`, error);
                }
            });
        }
    }
}

// Global instance
window.ticketBlasterSignalR = new TicketBlasterSignalR();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if user is authenticated
    const token = localStorage.getItem('token');
    if (token) {
        window.ticketBlasterSignalR.initialize();
    }
});

// Initialize on login
window.addEventListener('userLoggedIn', () => {
    window.ticketBlasterSignalR.initialize();
});

// Cleanup on logout
window.addEventListener('userLoggedOut', () => {
    if (window.ticketBlasterSignalR.connection) {
        window.ticketBlasterSignalR.connection.stop();
    }
});

// Add CSS for notifications
const style = document.createElement('style');
style.textContent = `
    .notifications-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        max-width: 400px;
    }

    .notification {
        background: white;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        margin-bottom: 10px;
        border-left: 4px solid #007bff;
        animation: slideIn 0.3s ease-out;
    }

    .notification-info { border-left-color: #17a2b8; }
    .notification-success { border-left-color: #28a745; }
    .notification-warning { border-left-color: #ffc107; }
    .notification-danger { border-left-color: #dc3545; }

    .notification-content {
        padding: 15px;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .notification-message {
        flex: 1;
        font-size: 14px;
        color: #333;
    }

    .notification-close {
        background: none;
        border: none;
        font-size: 20px;
        cursor: pointer;
        color: #999;
        padding: 0;
        margin-left: 10px;
    }

    .notification-close:hover {
        color: #666;
    }

    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    .connection-status {
        position: fixed;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
        padding: 5px 15px;
        border-radius: 15px;
        font-size: 12px;
        font-weight: bold;
        z-index: 10000;
    }

    .connection-status.success {
        background: #d4edda;
        color: #155724;
        border: 1px solid #c3e6cb;
    }

    .connection-status.warning {
        background: #fff3cd;
        color: #856404;
        border: 1px solid #ffeaa7;
    }

    .connection-status.danger {
        background: #f8d7da;
        color: #721c24;
        border: 1px solid #f5c6cb;
    }

    .quantity-updated {
        animation: pulse 0.5s ease-in-out;
        color: #28a745 !important;
        font-weight: bold;
    }

    .price-updated {
        animation: pulse 0.5s ease-in-out;
        color: #007bff !important;
        font-weight: bold;
    }

    @keyframes pulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.1); }
        100% { transform: scale(1); }
    }

    .sold-out {
        opacity: 0.6;
        pointer-events: none;
    }

    .viewer-count {
        font-size: 12px;
        color: #666;
        margin-top: 5px;
    }
`;

document.head.appendChild(style);