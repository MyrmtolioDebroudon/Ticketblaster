// Stripe payment integration
let stripe;
let elements;
let cardElement;
let paymentIntentClientSecret;
let dotNetReference;

window.initializeStripe = function (publishableKey, clientSecret, dotNetRef) {
    // Store references
    paymentIntentClientSecret = clientSecret;
    dotNetReference = dotNetRef;

    // Initialize Stripe
    stripe = Stripe(publishableKey);
    elements = stripe.elements();

    // Create card element
    const style = {
        base: {
            color: '#32325d',
            fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
            fontSmoothing: 'antialiased',
            fontSize: '16px',
            '::placeholder': {
                color: '#aab7c4'
            }
        },
        invalid: {
            color: '#fa755a',
            iconColor: '#fa755a'
        }
    };

    cardElement = elements.create('card', { style: style });
    cardElement.mount('#card-element');

    // Handle real-time validation errors
    cardElement.on('change', function (event) {
        const displayError = document.getElementById('card-errors');
        if (event.error) {
            displayError.textContent = event.error.message;
        } else {
            displayError.textContent = '';
        }
    });

    // Handle form submission
    const form = document.getElementById('payment-form');
    form.addEventListener('submit', handleSubmit);
};

async function handleSubmit(event) {
    event.preventDefault();

    // Notify Blazor that processing has started
    dotNetReference.invokeMethodAsync('SetProcessing', true);

    const cardholderName = document.querySelector('input[placeholder="John Doe"]').value;

    if (!cardholderName) {
        const errorElement = document.getElementById('card-errors');
        errorElement.textContent = 'Please enter the cardholder name.';
        dotNetReference.invokeMethodAsync('SetProcessing', false);
        return;
    }

    // Confirm the payment with Stripe
    const { error, paymentIntent } = await stripe.confirmCardPayment(paymentIntentClientSecret, {
        payment_method: {
            card: cardElement,
            billing_details: {
                name: cardholderName
            }
        }
    });

    if (error) {
        // Show error to customer
        console.error('Payment error:', error);
        dotNetReference.invokeMethodAsync('HandlePaymentResult', false, error.message);
    } else {
        // Payment succeeded
        if (paymentIntent.status === 'succeeded') {
            dotNetReference.invokeMethodAsync('HandlePaymentResult', true);
        } else {
            dotNetReference.invokeMethodAsync('HandlePaymentResult', false, 'Payment processing failed. Please try again.');
        }
    }
}

// Helper function to display payment result
window.displayPaymentResult = function (message, isSuccess) {
    const resultDiv = document.createElement('div');
    resultDiv.className = isSuccess ? 'alert alert-success' : 'alert alert-danger';
    resultDiv.textContent = message;
    
    const container = document.querySelector('.card-body');
    container.insertBefore(resultDiv, container.firstChild);
    
    // Auto-hide after 5 seconds
    setTimeout(() => {
        resultDiv.remove();
    }, 5000);
};