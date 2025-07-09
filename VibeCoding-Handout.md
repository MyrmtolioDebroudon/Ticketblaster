# Vibe Coding Quick Reference Guide
### AI-Assisted Development for Senior Engineers

---

## 🧠 The Mindset
**You're the architect; Claude is your fast implementer**

Instead of: *"Write a function that..."*  
Think: *"I need to accomplish X for our users..."*

---

## 🔄 The Vibe Flow

```mermaid
Intent → Generate → Review → Refine → Ship → Learn
```

### Real Example:
```
You: "Create a payment service for our ticketing platform"
AI:  [Generates basic service]
You: "Add webhook handling and make it idempotent"
AI:  [Enhances with production features]
You: "How would this handle race conditions?"
AI:  [Explains and suggests improvements]
```

---

## 📋 The Five Commandments

### 1. 🔍 Zoom Out Before You Zoom In
```
✓ "Design an order processing system" 
✓ "Now implement inventory checks"
✓ "Add pessimistic locking"
```

### 2. 👑 Context is King
```
❌ "Create a user service"
✅ "Create a user service for our ticketing platform. Users can be 
   customers, organizers, or admins. We use Keycloak for auth."
```

### 3. 🔄 Embrace the Refactor
- Pass 1: Get it working
- Pass 2: Make it robust
- Pass 3: Make it elegant

### 4. 🎯 Pattern Match Your Codebase
```
"Follow our EventService pattern - interface first, 
Result<T> returns, comprehensive logging"
```

### 5. 🤖 Background Agents for Boring Stuff
- Test generation
- Documentation updates  
- Large refactoring
- Boilerplate creation

---

## ⚡ Power Techniques

### Architecture First
```
"Show me the component diagram for real-time inventory"
[Review design]
"Now implement the TicketInventoryHub"
```

### The "What's Missing?" Check
After every generation ask:
- What edge cases aren't handled?
- What could fail in production?
- What security concerns exist?

### Cross-Pollination
```
"Take the error handling from our Python services 
and apply it to this C# code"
```

---

## 🚫 Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Code Dump | No understanding | Review → Understand → Modify |
| Perfect Prompt | Wastes time | Start simple, iterate |
| Context Amnesia | Repetitive work | Build on previous context |
| AI Shame | Misses learning | Document AI contributions |

---

## 📊 Proven Results

- **3x faster** feature delivery
- **85%** test coverage (vs 60% manual)
- **Complete** documentation
- **Better** error handling

---

## 🎯 Try This Today

1. Pick a feature from your backlog
2. Describe it conversationally to Claude
3. Generate → Review → Ask "What's missing?"
4. Iterate 2-3 times with improvements
5. Ship faster with higher quality

---

## 💡 Remember

> **Traditional coding**: You're a craftsman hand-carving every line  
> **Vibe coding**: You're a conductor orchestrating an AI symphony
> 
> Master developers know when to use each.

---

### Quick Commands Cheat Sheet

```yaml
Starting:
  - "I'm building X. It needs to do Y for users who Z"
  - "Design the architecture for..."
  - "What's the best approach for..."

Refining:
  - "Make this more production-ready"
  - "Add proper error handling"
  - "What edge cases am I missing?"

Learning:
  - "Explain how this handles..."
  - "What security concerns should I consider?"
  - "How would this fail at scale?"

Shipping:
  - "Generate comprehensive tests for this"
  - "Document this for other developers"
  - "Create a migration plan"
```

---

**Pro Tip**: The goal isn't to code faster—it's to solve problems better. Use the time saved on boilerplate to think deeper about architecture, user experience, and edge cases.

---

*Join the discussion: #vibe-coding on Slack*