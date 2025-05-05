# VideoScriptAI

![VideoScriptAI Logo](https://raw.githubusercontent.com/rimpx/videoScriptAI/main/assets/logo.png)

## 📋 Overview

VideoScriptAI is an advanced AI-powered application that helps content creators, marketers, and filmmakers generate high-quality video scripts quickly and efficiently. Built with C# and offering a web-based interface, this tool leverages artificial intelligence to transform ideas into well-structured, engaging scripts ready for production.

## ✨ Features

- **AI-Powered Script Generation**: Create professional video scripts based on simple prompts or topics
- **Multiple Script Formats**: Generate scripts for various video types including explainer videos, tutorials, advertisements, and social media content
- **Customizable Templates**: Choose from a variety of pre-built templates or create your own
- **Script Editing Tools**: Refine generated scripts with an intuitive editor
- **Export Options**: Download scripts in multiple formats including TXT, PDF, and Word
- **Web Interface**: Easy-to-use HTML interface accessible from any modern browser
- **Multilingual Support**: Generate scripts in multiple languages

## 🚀 Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 (recommended) or any IDE that supports C# development
- API key for the AI service used (see configuration section)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/rimpx/videoScriptAI.git
   cd videoScriptAI
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Configure your API keys in `appsettings.json` (see Configuration section)

4. Build and run the application:
   ```bash
   dotnet build
   dotnet run
   ```

5. Access the web interface at `http://localhost:5000`

## ⚙️ Configuration

Configure the application by editing the `appsettings.json` file:

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "gpt-4"
  },
  "Application": {
    "MaxScriptLength": 5000,
    "DefaultLanguage": "en-US",
    "TemplatePath": "./Templates"
  }
}
```

## 🧩 Project Structure

```
videoScriptAI/
├── src/
│   ├── VideoScriptAI.Core/           # Core business logic
│   ├── VideoScriptAI.Services/       # AI integration services
│   ├── VideoScriptAI.Web/            # Web interface (HTML, CSS, JS)
│   └── VideoScriptAI.Console/        # Optional console interface
├── tests/
│   ├── VideoScriptAI.Core.Tests/     # Unit tests for core functionality
│   └── VideoScriptAI.Services.Tests/ # Tests for AI services
├── docs/                             # Documentation
├── assets/                           # Images, icons, etc.
└── Templates/                        # Script templates
```

## 🛠️ Technology Stack

- **Backend**: C# (.NET 6)
- **Frontend**: HTML, CSS, JavaScript
- **AI Integration**: OpenAI GPT API (or other configured AI service)
- **Testing**: xUnit

## 📊 Usage Examples

### Basic Script Generation

1. Access the web interface
2. Select a script template (e.g., "YouTube Tutorial")
3. Enter your topic and key points
4. Click "Generate Script"
5. Review and edit the generated script
6. Export to your preferred format

### Advanced Customization

1. Navigate to Templates directory
2. Create custom template files following the existing format
3. Restart the application to load new templates
4. Use your custom templates for script generation

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📬 Contact

Rimpx - [GitHub Profile](https://github.com/rimpx)

Project Link: [https://github.com/rimpx/videoScriptAI](https://github.com/rimpx/videoScriptAI)

## 🙏 Acknowledgements

- [.NET Community](https://dotnet.microsoft.com/) for the development framework
- All contributors who have helped shape this project
