# 🧠📊 AI-Powered Chart Generator with Syncfusion Blazor Components

This project showcases how to harness **natural language processing** and **AI (via Azure OpenAI)** to dynamically generate **interactive charts** in a Blazor application using **Syncfusion Blazor Components**.

It features a conversational UI that interprets user input, generates chart data using AI, and renders it as a fully styled, interactive chart component.

## 🚀 Key Features

- 🗣️ Natural Language Input – Describe the chart you want in plain English
- 🤖 AI-Powered Parsing – Uses Azure OpenAI to interpret and respond
- 📊 Dynamic Chart Rendering – Converts AI-generated JSON into charts
- 🎨 Themed Visuals – Built-in themes via Syncfusion Blazor
- 💬 ChatGPT-like UX – Powered by `AIAssistView` for a conversational experience
- ⚡ Real-Time Interactivity – Charts update instantly based on input

## 📸 Preview
![Presentation1](https://github.com/user-attachments/assets/74a5fb3d-3690-454a-b679-421e3be962df)


## 🧰 Technologies Used

- [Blazor – Web UI framework by Microsoft](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/intro)
- [Syncfusion Blazor Components](https://www.syncfusion.com/blazor-components)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/)
- Visual Studio 2022+ or VS Code (optional)

## 🧑‍💻 How It Works

1. User enters a chart request in natural language.
2. The app sends a structured prompt to Azure OpenAI.
3. AI returns a JSON object with chart data and configuration.
4. The app deserializes the JSON and binds it to Syncfusion chart components.
5. The chart is rendered interactively in the UI.

## 📝 Prerequisites

- [.NET 8 SDK with Blazor](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/intro)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- An active [Azure subscription](https://azure.microsoft.com/)
- [Access to Azure OpenAI](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/overview)
