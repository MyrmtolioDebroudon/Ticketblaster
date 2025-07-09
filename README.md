# TicketBlaster - Open-Source Ticketing Platform

A comprehensive event ticketing platform built with Oqtane 6.1.3+ and ASP.NET Core 8, featuring Keycloak authentication and Stripe payment processing.

## 🚀 Features

- **Event Management**: Create, manage, and publish events with rich content
- **Multi-Ticket Types**: Support for various ticket types with different pricing
- **Payment Processing**: Integrated Stripe and PayPal payment processing
- **Real-time Updates**: SignalR for live inventory and order status updates
- **User Authentication**: Keycloak-based OAuth 2.0/OpenID Connect authentication
- **Role-based Access**: Admin, Organizer, and Customer roles with proper authorization
- **Mobile Responsive**: PWA-ready design for all devices
- **Analytics Dashboard**: Comprehensive reporting and analytics

## 🏗️ Architecture

### Technology Stack
- **Framework**: Oqtane 6.1.3+ (Blazor-based modular framework)
- **Backend**: ASP.NET Core 8.0+
- **Database**: Entity Framework Core with SQL Server
- **Frontend**: Blazor Server/WebAssembly hybrid
- **Identity**: Keycloak (OAuth 2.0/OpenID Connect)
- **Payments**: Stripe (primary), PayPal (secondary)
- **Real-time**: SignalR
- **Hosting**: Azure App Service

### Project Structure
```
TicketBlaster.Solution/
├── TicketBlaster.Server/          # Main server application
├── TicketBlaster.Client/          # Blazor client components
├── TicketBlaster.Shared/          # Shared models and contracts
├── TicketBlaster.Database/        # Entity Framework DbContext
├── TicketBlaster.Modules/         # Custom Oqtane modules
│   ├── Events/                    # Event management module
│   ├── Ticketing/                 # Ticket sales module
│   ├── Payments/                  # Payment processing module
│   ├── Analytics/                 # Analytics dashboard module
│   └── Authentication/            # Keycloak integration module
└── TicketBlaster.Tests/           # Unit and integration tests
```

## 🛠️ Setup & Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Keycloak server
- Stripe account (for payments)

### Configuration
1. **Database**: Update connection strings in `appsettings.json`
2. **Keycloak**: Configure authentication settings
3. **Stripe**: Add payment processing keys
4. **Email**: Configure email service provider

### Running the Application
```bash
dotnet restore
dotnet build
dotnet run --project TicketBlaster.Server
```

## 📋 Development Status

### Phase 1: Foundation ✅
- [x] Project structure setup
- [x] Entity Framework models
- [x] Database configuration
- [x] Service interfaces
- [x] SignalR infrastructure

### Phase 2: Core Features (In Progress)
- [ ] Keycloak authentication module
- [ ] Event management module
- [ ] Ticket sales functionality
- [ ] Payment processing
- [ ] User management

### Phase 3: Advanced Features
- [ ] Analytics dashboard
- [ ] Email notifications
- [ ] Mobile optimization
- [ ] Performance optimization

## 🔧 Core Models

### Event Management
- **Event**: Main event entity with scheduling and venue information
- **Category**: Event categorization system
- **TicketType**: Different ticket types with pricing and availability

### Order Processing
- **Order**: Purchase transactions with customer information
- **OrderItem**: Individual ticket purchases within an order
- **Ticket**: Generated tickets with QR codes

### Payment System
- **Payment**: Payment transaction records
- **PaymentRefund**: Refund processing and tracking

### User Management
- **User**: Extended user profiles with Keycloak integration
- **Role**: Role-based access control
- **UserRole**: User-role associations

## 🔐 Security Features

- OAuth 2.0/OpenID Connect authentication
- Role-based authorization
- PCI DSS compliant payment processing
- Input validation and sanitization
- HTTPS enforcement
- CSRF protection

## 🌟 Real-time Features

- Live ticket inventory updates
- Order status notifications
- Payment processing feedback
- Event capacity monitoring

## 📊 Analytics & Reporting

- Sales dashboard
- Event performance metrics
- User engagement analytics
- Revenue tracking
- Attendee demographics

## 🚀 Deployment

The application is designed for Azure deployment with:
- Azure App Service hosting
- Azure SQL Database
- Azure CDN for static assets
- Application Insights for monitoring

## 📖 Documentation

- [Architecture Overview](docs/architecture.md)
- [API Documentation](docs/api.md)
- [Development Guide](docs/development.md)
- [Deployment Guide](docs/deployment.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📧 Contact

For questions or support, please contact the development team.

---

Built with ❤️ using Oqtane Framework and modern .NET technologies.
