# Customer Service Simulator

A Unity-based customer service training simulator featuring AI-powered NPCs, dynamic conversation generation, and comprehensive performance metrics.

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?style=flat-square&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)

## 🎯 Overview

The Customer Service Simulator is an interactive training environment designed to help users practice and improve their customer service skills. Players interact with AI-powered NPCs who present realistic customer complaints and scenarios, making choices that affect customer satisfaction in real-time.

## ✨ Features

### 🤖 AI-Powered NPCs
- **ConvaiNPC Integration**: Multiple unique customer NPCs with distinct personalities and backstories
- **Dynamic Conversation**: Real-time speech recognition and natural language processing
- **Contextual Responses**: AI generates appropriate responses based on actual customer complaints
- **Conversation Flow Management**: Automatic detection of conversation endings and natural transitions

### 📊 Performance Metrics
- **Real-time Satisfaction Tracking**: Visual satisfaction slider that updates based on player choices
- **Comprehensive Analytics**: Track interaction times, success rates, and satisfaction changes
- **Letter Grading System**: A-F grading based on weighted performance metrics
- **Detailed Insights**: Actionable feedback and improvement suggestions
- **Professional Report Card**: End-of-session performance summary with color-coded results

### 🎮 Interactive Gameplay
- **Multiple Choice Responses**: Choose between good and bad response options
- **Real-time Feedback**: Immediate visual feedback through satisfaction toasts
- **Progressive Difficulty**: Handle multiple customers with varying complaint types
- **Smooth Transitions**: Elegant fade effects between customer interactions

### 🔧 Technical Features
- **API Rate Limiting**: Built-in protection against API quota exceeded errors
- **GRPC Session Management**: Safe termination and cleanup of NPC sessions
- **Transcript Capture**: Enhanced keyword detection and conversation analysis
- **Modular Architecture**: Easy to extend and customize for different scenarios

## 🛠️ Technical Requirements

### Unity Version
- **Unity 2022.3 LTS** or newer
- **TextMeshPro Package** (for UI text rendering)

### Dependencies
- **ConvaiNPC Package**: For AI-powered character interactions
- **Ready Player Me** (optional): For avatar customization
- **.NET Framework 4.7.1** or newer

### Platform Support
- **Windows**: Full support
- **macOS**: Full support
- **WebGL**: Supported with ConvaiNPC WebGL build

## 🚀 Installation

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/CustomerServiceSimulator.git
cd CustomerServiceSimulator
```

### 2. Open in Unity
1. Open Unity Hub
2. Click "Add" and select the cloned project folder
3. Open the project with Unity 2022.3 LTS or newer

### 3. Install Required Packages
The project should automatically import required packages. If not:
1. Open Window → Package Manager
2. Install TextMeshPro if prompted
3. Ensure ConvaiNPC package is properly installed

### 4. Setup ConvaiNPC
1. Follow ConvaiNPC documentation for API key setup
2. Configure your ConvaiNPC characters in the inspector
3. Assign NPCs to the CustomerServiceIntegration component

## 🎮 How to Play

### Getting Started
1. **Start the Game**: Click the start button to begin your shift
2. **Listen to Customers**: Each NPC will present their complaint or issue
3. **Choose Your Response**: Select between good and bad response options
4. **Monitor Satisfaction**: Watch the satisfaction meter and feedback toasts
5. **Complete Your Shift**: Serve all customers to see your performance report

### Gameplay Tips
- **Listen Carefully**: Pay attention to the specific complaint before choosing a response
- **Be Quick but Thoughtful**: Faster responses improve efficiency, but accuracy matters more
- **Maintain Satisfaction**: Try to keep customer satisfaction above 70%
- **Learn from Feedback**: Use the insights in your report card to improve

## 🏗️ Project Structure

```
Assets/
├── Scripts/
│   ├── ConvaiCustomerServiceIntegration.cs    # Main NPC interaction system
│   ├── ConvaiResponseGenerator.cs             # Dynamic response generation
│   ├── CustomerServiceMetrics.cs              # Performance tracking
│   ├── ReportCardUI.cs                        # End-game report display
│   ├── RushSession.cs                         # Game session management
│   ├── SatisfactionGauge.cs                   # Satisfaction tracking
│   └── DialougeController.cs                  # Choice presentation UI
├── Scenes/
│   └── MainScene.unity                        # Main game scene
├── Resources/
│   └── UI Prefabs/                            # UI component prefabs
└── Plugins/
    └── ConvaiNPC/                             # ConvaiNPC integration
```

## ⚙️ Configuration

### NPC Setup
1. **Create NPCs**: Add ConvaiNPC components to GameObjects
2. **Assign Characters**: Configure each NPC with unique character IDs
3. **Set Positions**: Use the NPC anchor point system for positioning
4. **Configure Integration**: Assign NPCs to the ConvaiCustomerServiceIntegration array

### Metrics Configuration
1. **Add CustomerServiceMetrics**: Attach component to a GameObject in the scene
2. **Setup Report Card UI**: Create UI canvas with required TextMeshPro components
3. **Configure Grading**: Adjust scoring weights in CustomerServiceMetrics.cs
4. **Customize Insights**: Modify GenerateInsights() method for custom feedback

### Performance Tuning
- **API Call Delay**: Adjust `apiCallDelay` to prevent rate limiting (default: 2 seconds)
- **Response Timeout**: Configure `responseTimeout` for NPC response waiting (default: 15 seconds)
- **Max Exchanges**: Set `maxComplaintExchanges` per customer (default: 3)

## 📈 Metrics System

### Tracked Metrics
- **Total Customers Served**: Number of customers helped during the session
- **Average Interaction Time**: Time spent per customer interaction
- **Average Satisfaction Change**: How much satisfaction improved or decreased
- **Success Rate**: Percentage of successful customer interactions
- **Player Choices Made**: Total number of response choices selected

### Grading System
The system uses a weighted scoring approach:
- **60% Success Rate**: Percentage of positive customer outcomes
- **40% Satisfaction Change**: Average improvement in customer satisfaction

**Grade Scale:**
- **A (90-100%)**: Outstanding customer service performance
- **B (80-89%)**: Strong customer service skills demonstrated  
- **C (70-79%)**: Satisfactory performance with growth potential
- **D (60-69%)**: Basic customer service skills need development
- **F (0-59%)**: Significant improvement needed

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork the Repository**
2. **Create a Feature Branch**: `git checkout -b feature/amazing-feature`
3. **Commit Changes**: `git commit -m 'Add amazing feature'`
4. **Push to Branch**: `git push origin feature/amazing-feature`
5. **Open a Pull Request**

### Development Guidelines
- Follow C# coding conventions
- Add XML documentation for public methods
- Test new features thoroughly
- Update README if adding new functionality

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **ConvaiNPC Team**: For providing the AI conversation framework
- **Ready Player Me**: For avatar customization capabilities
- **Unity Technologies**: For the game engine and development tools

## 📞 Support

If you encounter issues or have questions:

1. **Check the Issues**: Look for existing solutions in GitHub Issues
2. **Create New Issue**: Provide detailed description and reproduction steps
3. **Community Discussions**: Join our community discussions for general questions

## 🔮 Future Features

- **Voice Recognition**: Direct speech input for player responses
- **Multiplayer Mode**: Team-based customer service scenarios
- **Custom Scenarios**: User-created customer service situations
- **Advanced Analytics**: Detailed performance breakdowns and trends
- **Mobile Support**: iOS and Android compatibility

---

**Made with ❤️ using Unity and ConvaiNPC**

*Train your customer service skills in a realistic, AI-powered environment!*
