# Building a Modern Ticketing Platform with Claude Code & Background Agents in Cursor
## Lesson Plan & Step-by-Step Guide

### Course Overview
This lesson demonstrates how to build a production-ready ticketing platform using AI-powered development with Claude Code and background agents in Cursor IDE. Students will learn modern development practices, AI-assisted coding, and rapid prototyping techniques.

---

## 📚 Module 1: Environment Setup & Prerequisites

### 1.1 Setting Up Your Development Environment

#### Prerequisites Installation
1. **Install Cursor IDE**
   - Download from https://cursor.sh
   - Install with default settings
   - Sign in with GitHub/email

2. **Install .NET 8.0 SDK**
   ```bash
   # Windows (winget)
   winget install Microsoft.DotNet.SDK.8
   
   # macOS
   brew install --cask dotnet-sdk
   
   # Linux
   sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
   ```

3. **Install SQL Server/LocalDB**
   - Download SQL Server Express or use LocalDB
   - Install SQL Server Management Studio (SSMS)

4. **Install Docker Desktop** (for Keycloak)
   - Required for running Keycloak authentication server
   - Enable WSL2 backend on Windows

#### Cursor Configuration
1. **Enable AI Features**
   - Open Cursor Settings (Ctrl+,)
   - Navigate to AI section
   - Enable "Claude Code" and "Background Agents"
   - Select Claude-3-Opus model for best results

2. **Configure Extensions**
   - C# Dev Kit
   - Entity Framework Core Tools
   - Docker
   - GitLens

### 1.2 Project Planning with Claude

#### Initial Conversation with Claude
```
User: "I want to build a modern ticketing platform similar to Ticketmaster 
using .NET 8, Blazor, and the Oqtane framework. It should have:
- Event management
- Ticket sales with multiple ticket types
- Keycloak authentication
- Stripe payment processing
- Real-time updates with SignalR
- Admin analytics dashboard"

Claude: [Provides architecture overview and suggests project structure]
```

---

## 📐 Module 2: Building the Foundation with AI Assistance

### 2.1 Creating the Project Structure

#### Step 1: Initialize Solution with Background Agent
1. Open Cursor and create new workspace
2. Start a background agent conversation:
   ```
   "Create a complete project structure for TicketBlaster - a ticketing platform 
   using Oqtane 6.1.3+, ASP.NET Core 8, with proper separation of concerns"
   ```

3. Background agent creates:
   - Solution file structure
   - Project references
   - Initial folder hierarchy
   - README with architecture documentation

#### Step 2: Setting Up Database Models
Using Claude Code inline:
```
@Claude: Create Entity Framework models for a ticketing system with:
- Events (with categories, organizers)
- Ticket types (with pricing, availability)
- Orders and order items
- Users with Keycloak integration
- Payment records
```

**Result**: Claude generates complete model classes with:
- Proper data annotations
- Navigation properties
- Validation attributes
- Audit fields (CreatedBy, UpdatedOn, etc.)

### 2.2 Database Configuration

#### Background Agent Task:
```
"Set up Entity Framework Core with SQL Server, create DbContext, 
configure relationships, and generate initial migrations"
```

The agent:
1. Creates `TicketBlasterDbContext`
2. Configures entity relationships
3. Sets up connection strings
4. Generates migration commands
5. Creates seed data

---

## 🔧 Module 3: Core Services Implementation

### 3.1 Service Layer Architecture

#### Using Claude for Service Interfaces
```
@Claude: Design service interfaces for:
- Event management (CRUD, search, filtering)
- Ticket inventory (availability, reservations)
- Order processing (cart, checkout, confirmation)
- Payment processing (Stripe integration)
- Email notifications
```

### 3.2 Real-time Features with SignalR

#### Background Agent Implementation:
```
"Implement SignalR hubs for:
1. Real-time ticket inventory updates
2. Order status notifications
3. Live event capacity tracking"
```

**Generated Code Highlights:**
- `TicketInventoryHub` for real-time availability
- `OrderStatusHub` for order updates
- Client-side JavaScript integration
- Automatic reconnection handling

---

## 🔐 Module 4: Authentication & Security

### 4.1 Keycloak Integration

#### Step-by-Step with Claude:
1. **Docker Compose Setup**
   ```
   @Claude: Create docker-compose.yml for Keycloak with 
   proper realm configuration for TicketBlaster
   ```

2. **Authentication Module**
   ```
   Background Agent: "Create an Oqtane module for Keycloak 
   authentication with OAuth 2.0/OpenID Connect"
   ```

3. **Authorization Policies**
   - Role-based access (Admin, Organizer, Customer)
   - Policy-based authorization
   - JWT token validation

### 4.2 Security Best Practices
Claude implements:
- Input validation
- CSRF protection
- SQL injection prevention
- XSS protection
- Secure password policies

---

## 💳 Module 5: Payment Processing

### 5.1 Stripe Integration

#### Implementation Process:
1. **Service Layer**
   ```
   @Claude: Implement Stripe payment service with:
   - Payment intent creation
   - Webhook handling
   - Refund processing
   - PCI compliance
   ```

2. **Frontend Components**
   - Stripe Elements integration
   - Payment form validation
   - 3D Secure handling

### 5.2 Order Processing Workflow
Background agent creates:
- Shopping cart functionality
- Checkout process
- Order confirmation
- Email receipts
- PDF ticket generation

---

## 📊 Module 6: Building UI Components

### 6.1 Blazor Component Development

#### Event Listing Page
```
@Claude: Create a Blazor component for event listing with:
- Grid/card view toggle
- Category filtering
- Date range selection
- Search functionality
- Pagination
```

### 6.2 Admin Dashboard
Background agent task:
```
"Build an analytics dashboard module showing:
- Sales metrics
- Popular events
- Revenue charts
- User demographics
- Real-time attendance"
```

---

## 🚀 Module 7: Deployment & DevOps

### 7.1 Containerization
```
Background Agent: "Create Dockerfile and docker-compose 
for the entire TicketBlaster stack including:
- ASP.NET Core app
- SQL Server
- Keycloak
- Redis cache"
```

### 7.2 CI/CD Pipeline
Claude generates:
- GitHub Actions workflow
- Automated testing
- Docker image building
- Azure deployment scripts

---

## 📝 Module 8: Testing & Documentation

### 8.1 Automated Testing
```
@Claude: Generate comprehensive test suite:
- Unit tests for services
- Integration tests for APIs
- E2E tests for critical flows
- Load testing scripts
```

### 8.2 API Documentation
- Swagger/OpenAPI setup
- API versioning
- Request/response examples
- Authentication documentation

---

## 🎯 Key Learning Outcomes

### Technical Skills
1. **AI-Assisted Development**
   - Effective prompting techniques
   - Iterative development with Claude
   - Background agent utilization
   - Code review and refinement

2. **Modern .NET Development**
   - Clean architecture principles
   - Entity Framework Core mastery
   - Blazor component development
   - SignalR real-time features

3. **Integration Skills**
   - OAuth 2.0/OIDC authentication
   - Payment gateway integration
   - Email service configuration
   - Cloud deployment

### Soft Skills
1. **Project Management**
   - Breaking down complex requirements
   - Iterative development approach
   - Documentation practices

2. **Problem Solving**
   - Debugging AI-generated code
   - Performance optimization
   - Security considerations

---

## 💡 Tips for Success with Claude/Cursor

### 1. Effective Prompting
- Be specific about requirements
- Provide context and constraints
- Request explanations with code
- Ask for best practices

### 2. Iterative Development
- Start with MVP features
- Test frequently
- Refine based on results
- Don't accept first draft

### 3. Background Agent Best Practices
- Use for large structural changes
- Let it handle repetitive tasks
- Review generated code carefully
- Provide clear success criteria

### 4. Code Quality
- Always review AI-generated code
- Run linters and analyzers
- Write tests for critical paths
- Refactor for maintainability

---

## 📚 Additional Resources

### Documentation
- [Oqtane Framework Docs](https://docs.oqtane.org)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [Keycloak Documentation](https://www.keycloak.org/documentation)

### Sample Code Repository
- Full project code available at: [GitHub Repository Link]
- Includes all modules and examples
- Step-by-step commit history

### Video Tutorials
1. Environment Setup (20 min)
2. Building with Claude (45 min)
3. Deployment Walkthrough (30 min)

---

## 🎓 Assessment & Projects

### Hands-On Exercises
1. **Exercise 1**: Create a new Oqtane module using Claude
2. **Exercise 2**: Implement a custom payment provider
3. **Exercise 3**: Add social media authentication
4. **Exercise 4**: Build a mobile app with .NET MAUI

### Final Project Ideas
1. Add venue seating charts
2. Implement waitlist functionality
3. Create event organizer portal
4. Build recommendation engine
5. Add QR code scanning app

---

## 🔄 Continuous Learning

### Next Steps
1. Explore microservices architecture
2. Add machine learning features
3. Implement blockchain ticketing
4. Scale with Kubernetes
5. Add GraphQL API

### Community Resources
- Cursor Discord community
- Oqtane forums
- .NET community standups
- Local meetup groups

---

## 📋 Instructor Notes

### Time Allocation (8-hour workshop)
- Module 1: 45 minutes
- Module 2: 90 minutes
- Module 3: 90 minutes
- Module 4: 60 minutes
- Module 5: 60 minutes
- Module 6: 90 minutes
- Module 7: 45 minutes
- Module 8: 30 minutes
- Q&A: 30 minutes

### Common Challenges & Solutions
1. **Database Connection Issues**
   - Check SQL Server is running
   - Verify connection strings
   - Ensure proper permissions

2. **Keycloak Configuration**
   - Use provided realm export
   - Check redirect URIs
   - Verify client secrets

3. **AI Generation Issues**
   - Break down complex requests
   - Provide more context
   - Use specific examples

### Demo Preparation Checklist
- [ ] All services running
- [ ] Sample data loaded
- [ ] Payment test cards ready
- [ ] Backup environment prepared
- [ ] Screen recording software ready

---

*This lesson plan demonstrates the power of AI-assisted development in building production-ready applications. The combination of Claude's code generation capabilities and Cursor's integrated development environment enables rapid prototyping and implementation of complex systems.*