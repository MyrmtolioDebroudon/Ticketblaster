# Vibe Coding: AI-Assisted Development Etiquette
## A 15-Minute Practical Guide for Senior Engineers

### Duration: 15 minutes | Audience: Mid-Senior Software Engineers

---

## 🎯 The Core Concept (2 minutes)

### What is "Vibe Coding"?
It's the art of flowing with AI assistance rather than fighting against it. Think of it as pair programming with an incredibly fast junior developer who has memorized Stack Overflow but needs clear direction.

### The Mindset Shift
```
Traditional: "I need to write this service layer"
Vibe Coding: "Hey Claude, let's build a service layer that handles ticket inventory with real-time updates"
```

**Key Principle**: You're the architect and reviewer; Claude is the fast implementer.

---

## 🔄 The Vibe Coding Flow (3 minutes)

### 1. Start with Intent, Not Implementation
```
❌ Bad Vibe:
"Write a function that loops through events and checks if the date is greater than now"

✅ Good Vibe:
"I need to get upcoming events for the homepage. They should be sorted by date and include only published events"
```

### 2. Conversational Iteration
Real example from TicketBlaster:

**First Pass:**
```
Me: "Create a payment service for Stripe integration"
Claude: [Generates basic service]
Me: "This looks good, but add webhook handling and idempotency"
Claude: [Enhances with webhook support]
Me: "Perfect. Now make the webhook handler resilient to duplicate events"
Claude: [Adds idempotency keys and duplicate detection]
```

### 3. Trust but Verify
- Let Claude generate the boilerplate
- Focus your energy on business logic review
- Always check security implications

---

## 💡 The Five Commandments of Vibe Coding (5 minutes)

### 1. "Zoom Out Before You Zoom In"
Start broad, then narrow. Example from TicketBlaster:

```
Broad: "I'm building a ticketing platform. Design the order processing flow"
Narrow: "Now implement the inventory check before order confirmation"
Focused: "Add pessimistic locking to prevent overselling"
```

### 2. "Context is King"
Always provide business context, not just technical requirements:

```
❌ "Create a user service"

✅ "Create a user service for our ticketing platform. Users can be customers, 
event organizers, or admins. We're using Keycloak for auth, so the service 
should work with external user IDs"
```

### 3. "Embrace the Refactor"
Don't try to get it perfect in one shot:

```
Pass 1: Get it working
Pass 2: Add error handling
Pass 3: Optimize performance
Pass 4: Add comprehensive logging
```

Real TicketBlaster example - Order calculation evolution:
```csharp
// Initial generation - worked but basic
TotalAmount = items.Sum(i => i.Price)

// After feedback - handles quantity
TotalAmount = items.Sum(i => i.Price * i.Quantity)

// Final version - includes discounts and validation
var subtotal = items.Sum(i => i.Price * i.Quantity);
var discountTotal = await CalculateDiscounts(items);
TotalAmount = subtotal - discountTotal;
```

### 4. "Pattern Recognition Over Perfection"
Let Claude follow your established patterns:

```
"Follow the same service pattern we used for EventService - interface first, 
comprehensive logging, and return Result<T> types for error handling"
```

### 5. "Background Agents for the Boring Stuff"
Use background agents for:
- Large refactoring
- Test generation
- Documentation updates
- Migration scripts

Example:
```
Background Agent: "Add comprehensive unit tests for all service methods 
in TicketService. Use the same pattern as EventService tests."
```

---

## 🚀 Practical Workflow Integration (3 minutes)

### Morning Standup to Code in 30 Seconds
```
Team: "Today I'm implementing the waitlist feature"
You: [Opens Cursor]
You: "When events sell out, customers should be able to join a waitlist. 
When tickets become available, notify customers in order. Build this 
following our event-driven pattern."
Claude: [Generates initial implementation]
You: [Already reviewing code while others are still opening their IDE]
```

### The "Spike and Refine" Approach
1. **Spike** (5 minutes): Get Claude to generate a working prototype
2. **Review** (10 minutes): Identify what needs changing
3. **Refine** (15 minutes): Iterate with specific improvements
4. **Polish** (10 minutes): Add your team's conventions

### Code Review Etiquette
When reviewing AI-generated code in PRs:
```
"Generated the initial service structure with Claude, then added:
- Custom rate limiting logic (lines 45-67)
- Integration with our event bus (lines 78-92)
- Specific business rules for VIP customers (lines 101-125)"
```

---

## 🎭 Common Anti-Patterns (2 minutes)

### 1. The "Code Dump"
❌ Generating 500 lines and committing blindly

✅ Generate, review, understand, modify, then commit

### 2. The "Perfectionist Prompt"
❌ Trying to describe every detail in one massive prompt

✅ Start simple, iterate quickly

### 3. The "Context Amnesia"
❌ Treating each request as isolated

✅ Build on previous context: "Now add caching to the service we just created"

### 4. The "AI Shame"
❌ Hiding that you used AI assistance

✅ Be transparent: "Claude helped with boilerplate, I focused on business logic"

---

## 🏆 Power Moves for Senior Engineers (2 minutes)

### 1. Architecture First, Implementation Second
```
"Let's design the high-level architecture for a real-time inventory system. 
Show me the component diagram and data flow."
[Review and approve]
"Now implement the TicketInventoryHub based on this design"
```

### 2. The "Explain and Enhance" Pattern
```
"Explain how this PaymentService handles race conditions"
[Claude explains]
"Good. Now enhance it with distributed locking using Redis"
```

### 3. Cross-Pollination
```
"Take the error handling pattern from our Python services and implement 
it in this C# service"
```

### 4. The "What's Missing?" Technique
After generation, always ask:
```
"What edge cases is this code not handling?"
"What security concerns should we address?"
"How would this fail in production?"
```

---

## 🎯 Your First Vibe Coding Session (1 minute)

### Try This Today:
1. Pick a feature you need to implement
2. Open Cursor and describe it conversationally
3. Generate the initial implementation
4. Ask Claude: "What would make this more production-ready?"
5. Iterate 2-3 times with specific improvements
6. Ship it 3x faster than usual

### The Mental Model
```
You = Film Director
Claude = CGI Department

You direct the vision, they handle the rendering.
```

---

## 💬 Q&A Talking Points

**"Doesn't this make us worse developers?"**
> No, it makes us better architects. You spend less time on boilerplate and more time on design decisions.

**"What about code quality?"**
> AI follows patterns. Feed it good patterns, get good code. Feed it bad patterns...

**"How do I convince my team?"**
> Show, don't tell. Deliver a complex feature in 1/3 the time with equal quality.

**"What about proprietary code?"**
> Use local models or enterprise agreements. The workflow remains the same.

---

## 🚀 Closing Thought

**Traditional coding**: You're a craftsman hand-carving every line

**Vibe coding**: You're a conductor orchestrating an AI symphony

Both have their place. Master developers know when to use each.

---

### One-Page Cheat Sheet

```yaml
Vibe Coding Quick Reference:
  
  Start:
    - Intent over implementation
    - Context is everything
    - Embrace iteration
  
  During:
    - Zoom out → Zoom in
    - Generate → Review → Refine
    - Pattern match your codebase
  
  Review:
    - What's missing?
    - What could fail?
    - What would production need?
  
  Ship:
    - Document AI contributions
    - Test the edge cases
    - Share learnings with team
```

*Remember: The goal isn't to code faster—it's to solve problems better.*