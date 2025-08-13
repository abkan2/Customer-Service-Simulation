# Documentation Media Guide

This folder contains all media assets for the project documentation.

## 📁 Folder Structure

### `/images/` - Screenshots and Static Images
- `gameplay-overview.png` - Main gameplay screenshot
- `satisfaction-slider.png` - Satisfaction gauge in action
- `npc-interaction.png` - Player interacting with NPC
- `choice-selection.png` - Response choice interface
- `report-card.png` - End-game performance report
- `metrics-tracking.png` - Real-time metrics display
- `ui-overview.png` - Complete UI layout

### `/demos/` - Video Demonstrations and GIFs
- `full-gameplay.mp4` - Complete gameplay session (2-3 minutes)
- `npc-conversation.gif` - NPC interaction loop
- `satisfaction-feedback.gif` - Satisfaction changes with choices
- `report-card-animation.gif` - Report card reveal animation
- `customer-transition.gif` - Smooth customer transitions

## 📸 Recommended Screenshots to Take

### 1. **Main Menu / Start Screen**
- File: `main-menu.png`
- Shows: Start button, welcome UI, project branding

### 2. **Customer Interaction**
- File: `customer-interaction.png` 
- Shows: NPC talking, chat interface, satisfaction slider

### 3. **Choice Selection**
- File: `choice-selection.png`
- Shows: Good vs bad response options clearly visible

### 4. **Satisfaction Feedback**
- File: `satisfaction-toast.png`
- Shows: Positive or negative satisfaction toast notification

### 5. **Report Card**
- File: `report-card.png`
- Shows: Final grade, metrics, insights with good scores

### 6. **Metrics Overview**
- File: `metrics-overview.png`
- Shows: Customer counter, satisfaction level, game progress

## 🎬 Recommended Video/GIF Demos

### 1. **Complete Interaction Loop** (30-60 seconds)
- Start interaction → NPC speaks → Player chooses → Feedback → Next customer
- File: `interaction-loop.gif`

### 2. **Report Card Reveal** (10-15 seconds)
- Show the animated report card with typewriter effect
- File: `report-card-reveal.gif`

### 3. **Satisfaction Changes** (20-30 seconds)
- Show satisfaction going up and down based on choices
- File: `satisfaction-demo.gif`

## 🔗 How to Link in README

### For Images:
```markdown
![Description](docs/images/filename.png)
```

### For GIFs:
```markdown
![Demo Description](docs/demos/filename.gif)
```

### For Videos (GitHub will show preview):
```markdown
https://user-images.githubusercontent.com/yourusername/video-id/filename.mp4
```

## 📱 Image Guidelines

### **Resolution:**
- Screenshots: 1920x1080 (Full HD) preferred
- GIFs: 800x600 maximum (for file size)
- Keep file sizes under 10MB for GitHub

### **Format:**
- Screenshots: PNG for crisp UI elements
- Demos: GIF for short loops, MP4 for longer videos
- Compress images to reduce load times

### **Quality Tips:**
- Use high contrast for UI elements
- Ensure text is readable
- Show the most impressive features
- Capture during good lighting/visual conditions

## 📝 Example README Integration

```markdown
## 🎮 Gameplay Preview

### Customer Service in Action
![Customer Interaction](docs/images/customer-interaction.png)

### Real-time Satisfaction Tracking
![Satisfaction Demo](docs/demos/satisfaction-demo.gif)

### Performance Report Card
![Report Card](docs/images/report-card.png)
```

## 🚀 Quick Upload Steps

1. **Take screenshots/record videos** in Unity
2. **Save files** to appropriate folders (`docs/images/` or `docs/demos/`)
3. **Commit and push** to repository
4. **Update README.md** with image links
5. **Test links** by viewing README on GitHub

## 📂 File Naming Convention

Use descriptive, lowercase names with hyphens:
- ✅ `customer-interaction.png`
- ✅ `report-card-animation.gif`
- ❌ `Screenshot1.PNG`
- ❌ `IMG_001.jpg`
