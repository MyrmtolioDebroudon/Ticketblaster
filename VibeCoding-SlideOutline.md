# Vibe Coding - Slide Deck Outline
## Visual Companion for 15-Minute Lesson

---

### Slide 1: Title Slide
**Vibe Coding: AI-Assisted Development Etiquette**
- Subtitle: "Flow State Programming for Senior Engineers"
- Image: Split screen - Traditional coding vs AI-assisted coding
- Duration: On screen during intro

---

### Slide 2: The Mindset Shift
**Visual**: Side-by-side comparison

| Traditional Approach | Vibe Coding Approach |
|---------------------|---------------------|
| "I need to write this service layer" | "Hey Claude, let's build a service layer that handles ticket inventory with real-time updates" |
| 🧑‍💻 Solo coding | 🧑‍💻🤖 Pair programming |
| 4 hours | 45 minutes |

---

### Slide 3: The Vibe Flow
**Visual**: Circular flow diagram
```
Describe Intent → Generate → Review → Refine → Ship
       ↑                                      ↓
       ←←←←←←←← Learn & Iterate ←←←←←←←←←←←←
```

---

### Slide 4: Bad Vibe vs Good Vibe
**Visual**: Two chat bubbles

❌ **Bad Vibe**:
```
"Write a function that loops through 
events and checks if the date is 
greater than now"
```

✅ **Good Vibe**:
```
"I need to get upcoming events for 
the homepage. They should be sorted 
by date and include only published 
events"
```

**Bottom text**: "Intent > Implementation"

---

### Slide 5: Real Conversation Flow
**Visual**: Chat interface mockup showing actual TicketBlaster example

```
👤 "Create a payment service for Stripe integration"
🤖 [Shows generated basic service]
👤 "This looks good, but add webhook handling and idempotency"
🤖 [Shows enhanced version]
👤 "Perfect. Now make the webhook handler resilient to duplicate events"
🤖 [Shows final robust version]
```

---

### Slide 6: The Five Commandments
**Visual**: Mountain tablets design

1. 🔍 **Zoom Out Before You Zoom In**
2. 👑 **Context is King**
3. 🔄 **Embrace the Refactor**
4. 🎯 **Pattern Recognition Over Perfection**
5. 🤖 **Background Agents for the Boring Stuff**

---

### Slide 7: Evolution of Code
**Visual**: Three stages of code evolution

```csharp
// Stage 1: Basic ❌
TotalAmount = items.Sum(i => i.Price)

// Stage 2: Better ⚠️
TotalAmount = items.Sum(i => i.Price * i.Quantity)

// Stage 3: Production-Ready ✅
var subtotal = items.Sum(i => i.Price * i.Quantity);
var discountTotal = await CalculateDiscounts(items);
TotalAmount = subtotal - discountTotal;
```

---

### Slide 8: The Spike & Refine Timeline
**Visual**: Timeline graphic

```
0-5 min    | 5-15 min  | 15-30 min | 30-40 min
-----------|-----------|-----------|------------
🚀 Spike   | 👀 Review | 🔧 Refine | ✨ Polish
Generate   | Identify  | Iterate   | Team
prototype  | gaps      | improve   | standards
```

---

### Slide 9: Anti-Patterns Gallery
**Visual**: Four warning signs

1. 📚 **The Code Dump**
   - 500 lines → blind commit

2. 📝 **The Perfectionist Prompt**
   - One massive prompt → frustration

3. 🧠 **Context Amnesia**
   - Isolated requests → repetitive work

4. 😳 **AI Shame**
   - Hidden assistance → missed learning

---

### Slide 10: Power Moves
**Visual**: Chess pieces representing strategies

♔ **Architecture First**
```
"Design the system" → "Now implement"
```

♕ **Explain & Enhance**
```
"How does this handle X?" → "Now add Y"
```

♗ **Cross-Pollination**
```
"Use Pattern A from Project B"
```

♘ **What's Missing?**
```
"Edge cases? Security? Production issues?"
```

---

### Slide 11: Your Mental Model
**Visual**: Film set analogy

```
    👤 You                    🤖 Claude
    Director                  CGI Department
    
    "I need an epic battle"   *Creates 1000 warriors*
    "Make it at sunset"       *Adjusts lighting*
    "More emotional impact"   *Adds close-ups*
```

**Caption**: "Direct the vision, they handle the rendering"

---

### Slide 12: Real Metrics
**Visual**: Bar chart comparison

| Metric | Traditional | Vibe Coding | Improvement |
|--------|------------|-------------|-------------|
| Time to Feature | 3 days | 1 day | 3x faster |
| Lines Written | 1000 | 1200 | More comprehensive |
| Test Coverage | 60% | 85% | Better testing |
| Documentation | Minimal | Complete | Auto-documented |

---

### Slide 13: One-Page Cheat Sheet
**Visual**: Clean reference card design

```yaml
🚀 Vibe Coding Quick Reference

Start:
  ✓ Intent over implementation
  ✓ Context is everything
  ✓ Embrace iteration

During:
  ✓ Zoom out → Zoom in
  ✓ Generate → Review → Refine
  ✓ Pattern match your codebase

Review:
  ✓ What's missing?
  ✓ What could fail?
  ✓ What would production need?

Ship:
  ✓ Document AI contributions
  ✓ Test the edge cases
  ✓ Share learnings with team
```

---

### Slide 14: Try This Today
**Visual**: Step-by-step checklist

- [ ] Pick a feature from your backlog
- [ ] Open Cursor
- [ ] Describe it conversationally
- [ ] Generate initial implementation
- [ ] Ask: "What would make this production-ready?"
- [ ] Iterate 2-3 times
- [ ] Ship it 3x faster

---

### Slide 15: Closing Thought
**Visual**: Balance scale

Left side: 🔨 **Traditional**
- Craftsman
- Hand-carved code
- Deep focus

Right side: 🎼 **Vibe Coding**
- Conductor
- AI symphony
- Wide vision

**Bottom text**: "Master developers know when to use each"

---

## Slide Design Tips

### Color Scheme
- Primary: #0EA5E9 (Sky blue - represents flow)
- Secondary: #10B981 (Emerald - represents growth)
- Accent: #F59E0B (Amber - represents insights)
- Background: #0F172A (Slate dark)
- Text: #F8FAFC (Off-white)

### Typography
- Headers: Inter or SF Pro Display (Bold)
- Code: JetBrains Mono or Fira Code
- Body: Inter or SF Pro Text

### Visual Elements
- Use gradients for modern feel
- Subtle animations for transitions
- Code syntax highlighting
- Emoji for visual breaks
- Clean, minimal layouts

### Animation Suggestions
1. Code examples: Typewriter effect
2. Flow diagrams: Sequential reveal
3. Comparisons: Slide in from sides
4. Metrics: Animated bar growth
5. Cheat sheet: Fade in sections

---

## Presenter Notes Format

Each slide should have notes like:
```
SLIDE X: [Title]
- Key point to emphasize
- Story or example to share
- Transition to next slide
- Time: X minutes
```

This keeps the presentation flowing smoothly and ensures all key points are covered within the 15-minute timeframe.