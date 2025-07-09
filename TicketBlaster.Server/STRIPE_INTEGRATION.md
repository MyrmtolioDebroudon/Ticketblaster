# Stripe Payment Integration Guide

## Overview
This guide covers the Stripe payment processing integration for the TicketBlaster platform.

## Configuration

### 1. Stripe Account Setup
1. Create a Stripe account at https://stripe.com
2. Get your API keys from the Stripe Dashboard:
   - Test keys: `pk_test_...` and `sk_test_...`
   - Live keys: `pk_live_...` and `sk_live_...`

### 2. Application Configuration
Update your `appsettings.json` with your Stripe keys:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_secret_key",
    "PublishableKey": "pk_test_your_publishable_key",
    "WebhookSecret": "whsec_your_webhook_secret"
  }
}
```

### 3. Webhook Configuration
1. In your Stripe Dashboard, go to Developers → Webhooks
2. Add endpoint URL: `https://yourdomain.com/api/payment/webhook`
3. Select events to listen for:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `refund.updated`
4. Copy the webhook signing secret to your configuration

## Testing

### Test Card Numbers
Use these test card numbers in development:
- Success: `4242 4242 4242 4242`
- Requires authentication: `4000 0025 0000 3155`
- Decline: `4000 0000 0000 0002`

### Test Webhook Locally
Use Stripe CLI for local webhook testing:
```bash
stripe listen --forward-to localhost:5000/api/payment/webhook
```

## API Endpoints

### Create Payment Intent
```
POST /api/payment/create-payment-intent
{
  "orderId": 123
}
```

### Confirm Payment
```
POST /api/payment/confirm-payment
{
  "paymentIntentId": "pi_1234567890"
}
```

### Process Refund
```
POST /api/payment/refund
{
  "paymentId": 123,
  "amount": 50.00,
  "reason": "RequestedByCustomer"
}
```

## Payment Flow

1. **Order Creation**: User selects tickets and creates order
2. **Payment Intent**: Server creates Stripe PaymentIntent
3. **Card Collection**: Client-side Stripe.js collects card details
4. **Payment Confirmation**: Client confirms payment with Stripe
5. **Webhook Handling**: Server receives payment confirmation via webhook
6. **Order Completion**: Order status updated and tickets generated

## Security Considerations

1. **PCI Compliance**: Card details never touch your server
2. **HTTPS Required**: Always use HTTPS in production
3. **Webhook Validation**: Always verify webhook signatures
4. **API Key Security**: Never expose secret keys in client code

## Error Handling

Common error scenarios:
- Insufficient funds
- Card declined
- Invalid card number
- Authentication required (3D Secure)
- Network timeouts

## Refund Policy

Refunds can be processed through:
1. Admin dashboard (full or partial refunds)
2. API endpoint for automated refunds
3. Stripe Dashboard directly

## Monitoring

Monitor payment activity through:
- Stripe Dashboard
- Application logs
- Payment analytics endpoint
- Email notifications for failures

## Support

For issues:
1. Check Stripe logs in Dashboard
2. Review application logs
3. Test with Stripe CLI
4. Contact Stripe support for payment issues