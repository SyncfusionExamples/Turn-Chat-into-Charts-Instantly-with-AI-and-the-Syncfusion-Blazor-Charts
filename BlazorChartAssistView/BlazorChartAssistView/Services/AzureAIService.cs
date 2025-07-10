using Azure;
using Azure.AI.OpenAI;
using BlazorChartAssistView.Models;
using BlazorChartAssistView.Utils;
using OpenAI.Chat;
using System.Collections.ObjectModel;

namespace BlazorChartAssistView.Services
{
    public class AzureAIService
    {
        private readonly string _endpoint;
        private readonly string _key;
        private readonly string _deploymentName;
        private ChatClient? _client;
        private bool _isCredentialValid;
        private readonly ILogger<AzureAIService> _logger;

        public AzureAIService(IConfiguration configuration, ILogger<AzureAIService> logger)
        {
            _endpoint = configuration["Azure:OpenAI:Endpoint"] ?? "Your_EndPoint";
            _key = configuration["Azure:OpenAI:Key"] ?? "Your_Key";
            _deploymentName = configuration["Azure:OpenAI:DeploymentName"] ?? "Your_Deployment";
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                if (_endpoint != "Your_EndPoint" && _key != "Your_Key")
                {
                    var azureClient = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_key));
                    _client = azureClient.GetChatClient(_deploymentName);
                    _isCredentialValid = true;
                }
                else
                {
                    _isCredentialValid = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                _isCredentialValid = false;
            }
        }

        public async Task<string> GetResultsFromAIAsync(string userPrompt, List<ChatMessageModel>? chatHistory = null)
        {
            userPrompt = GetChartUserPrompt(userPrompt);

            if (!_isCredentialValid || _client == null)
            {
                return GetOfflineResponse(userPrompt);
            }

            try
            {
                var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful, intelligent and conversational assistant that can assist with a wide variety of topics including data visualization and chart creation.")
        };

                if (chatHistory != null)
                {
                    foreach (var message in chatHistory)
                    {
                        // Only include plain text messages
                        if (message.MessageType != ChatMessageType.Text || string.IsNullOrWhiteSpace(message.Text))
                            continue;

                        messages.Add(new AssistantChatMessage(message.Text));
                    }
                }

                // Add the current user prompt
                messages.Add(new UserChatMessage(userPrompt));

                var response = await _client.CompleteChatAsync(messages);

                return response?.Value?.Content?.FirstOrDefault()?.Text ?? "No response generated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response");
                return GetOfflineResponse(userPrompt);
            }
        }

        private string GetChartUserPrompt(string userPrompt)
        {
            string userQuery = @"
            You are a data visualization assistant. Your task is to convert user inputs describing chart specifications into structured JSON format. Each input will describe a chart type and its configuration, including axes, legends, series, and data.

            ### Supported Chart Types
            - **Chart Type**: Only `cartesian` or `circular`
            - **Series Types**: Line, Column, Spline, Area, Pie, Doughnut

            ### JSON Output Format
            {
              ""chartType"": ""cartesian | circular"",
              ""title"": ""<Chart Title>"",
              ""showLegend"": true,
              ""sideBySidePlacement"": true | false,
              ""xAxis"": [
                {
                  ""type"": ""category | numerical | datetime | logarithmic"",
                  ""title"": ""<X Axis Title>""
                }
              ],
              ""yAxis"": [
                {
                  ""type"": ""numerical | logarithmic"",
                  ""title"": ""<Y Axis Title>"",
                  ""min"": 0
                }
              ],
              ""series"": [
                {
                  ""type"": ""<SeriesType>"",
                  ""name"": ""<Series Name>"",
                  ""dataSource"": [
                    { ""xvalue"": ""<X>"", ""yvalue"": <Y> },
                    ...
                  ],
                  ""tooltip"": true | false
                }
              ]
            }

            ### Rules to Follow
            1. **Chart Type**: Infer from keywords like ""pie"", ""line"", ""column"", etc.
            2. **Title**: Derive a meaningful title from the user input.
            3. **Axis**: Cartesian charts must include both X and Y axes. Circular charts omit axes.
            4. **Series**: Use only supported types. Series name should reflect the category.
            5. **Data Source**: Always include `xvalue` and `yvalue` pairs.
            6. **Legend**: Default to `true` unless explicitly stated otherwise.
            7. **SideBySidePlacement**:
               - `true` if multiple column series are placed side-by-side.
               - `false` if columns are stacked or mixed.

            ### Examples

            **User Input**: ""Sales by region column chart""
            **Expected Output**:
            {
              ""chartType"": ""cartesian"",
              ""title"": ""Sales by Region"",
              ""showLegend"": true,
              ""sideBySidePlacement"": true,
              ""xAxis"": [{ ""type"": ""category"", ""title"": ""Region"" }],
              ""yAxis"": [{ ""type"": ""numerical"", ""title"": ""Sales"", ""min"": 0 }],
              ""series"": [
                {
                  ""type"": ""column"",
                  ""name"": ""Sales"",
                  ""dataSource"": [
                    { ""xvalue"": ""North"", ""yvalue"": 100 },
                    { ""xvalue"": ""South"", ""yvalue"": 80 },
                    { ""xvalue"": ""East"", ""yvalue"": 60 },
                    { ""xvalue"": ""West"", ""yvalue"": 90 }
                  ],
                  ""tooltip"": true
                }
              ]
            }

            **User Input**: ""Market share pie chart""
            **Expected Output**:
            {
              ""chartType"": ""circular"",
              ""title"": ""Market Share"",
              ""showLegend"": true,
              ""sideBySidePlacement"": false,
              ""series"": [
                {
                  ""type"": ""pie"",
                  ""name"": ""Market Share"",
                  ""dataSource"": [
                    { ""xvalue"": ""Product A"", ""yvalue"": 40 },
                    { ""xvalue"": ""Product B"", ""yvalue"": 30 },
                    { ""xvalue"": ""Product C"", ""yvalue"": 20 },
                    { ""xvalue"": ""Product D"", ""yvalue"": 10 }
                  ],
                  ""tooltip"": true
                }
              ]
            }

            Now, generate the JSON configuration for the following user request:

            User Request: " + userPrompt;

            return userQuery;
        }

        public ChartConfig? GenerateChartFromPrompt(string prompt)
        {
            var lowerPrompt = prompt.ToLower();

            if (lowerPrompt.Contains("pie") || lowerPrompt.Contains("doughnut"))
                return GeneratePieChart();
            else if (lowerPrompt.Contains("line") || lowerPrompt.Contains("trend"))
                return GenerateLineChart();
            else if (lowerPrompt.Contains("bar") || lowerPrompt.Contains("column"))
                return GenerateColumnChart();
            else if (lowerPrompt.Contains("area"))
                return GenerateAreaChart();
            else
                return GenerateColumnChart();
        }

        private ChartConfig GeneratePieChart()
        {
            return new ChartConfig
            {
                Title = "Sample Pie Chart",
                ChartType = ChartTypeEnum.Circular,
                ShowLegend = true,
                Series = new System.Collections.ObjectModel.ObservableCollection<SeriesConfig>
                {
                    new SeriesConfig
                    {
                        Type = SeriesType.Pie,
                        Name = "Data Series",
                        DataSource = new System.Collections.ObjectModel.ObservableCollection<ChartDataModel>
                        {
                            new ChartDataModel { XValue = "Category A", YValue = 30 },
                            new ChartDataModel { XValue = "Category B", YValue = 25 },
                            new ChartDataModel { XValue = "Category C", YValue = 20 },
                            new ChartDataModel { XValue = "Category D", YValue = 15 },
                            new ChartDataModel { XValue = "Category E", YValue = 10 }
                        }
                    }
                }
            };
        }

        private ChartConfig GenerateColumnChart()
        {
            return new ChartConfig
            {
                Title = "Sample Column Chart",
                ChartType = ChartTypeEnum.Cartesian,
                ShowLegend = true,
                SideBySidePlacement = true,
                XAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Categories", Type = AxisType.Category }
                },
                YAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Values", Type = AxisType.Numerical }
                },
                Series = new System.Collections.ObjectModel.ObservableCollection<SeriesConfig>
                {
                    new SeriesConfig
                    {
                        Type = SeriesType.Column,
                        Name = "Sales Data",
                        DataSource = new System.Collections.ObjectModel.ObservableCollection<ChartDataModel>
                        {
                            new ChartDataModel { XValue = "Jan", YValue = 35 },
                            new ChartDataModel { XValue = "Feb", YValue = 28 },
                            new ChartDataModel { XValue = "Mar", YValue = 34 },
                            new ChartDataModel { XValue = "Apr", YValue = 32 },
                            new ChartDataModel { XValue = "May", YValue = 40 },
                            new ChartDataModel { XValue = "Jun", YValue = 32 }
                        }
                    }
                }
            };
        }

        private ChartConfig GenerateLineChart()
        {
            return new ChartConfig
            {
                Title = "Sample Line Chart",
                ChartType = ChartTypeEnum.Cartesian,
                ShowLegend = true,
                XAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Time Period", Type = AxisType.Category }
                },
                YAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Values", Type = AxisType.Numerical }
                },
                Series = new System.Collections.ObjectModel.ObservableCollection<SeriesConfig>
                {
                    new SeriesConfig
                    {
                        Type = SeriesType.Line,
                        Name = "Trend Data",
                        DataSource = new System.Collections.ObjectModel.ObservableCollection<ChartDataModel>
                        {
                            new ChartDataModel { XValue = "Q1", YValue = 21 },
                            new ChartDataModel { XValue = "Q2", YValue = 24 },
                            new ChartDataModel { XValue = "Q3", YValue = 36 },
                            new ChartDataModel { XValue = "Q4", YValue = 38 }
                        }
                    }
                }
            };
        }

        private ChartConfig GenerateAreaChart()
        {
            return new ChartConfig
            {
                Title = "Sample Area Chart",
                ChartType = ChartTypeEnum.Cartesian,
                ShowLegend = true,
                XAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Months", Type = AxisType.Category }
                },
                YAxis = new System.Collections.ObjectModel.ObservableCollection<AxisConfig>
                {
                    new AxisConfig { Title = "Revenue", Type = AxisType.Numerical }
                },
                Series = new System.Collections.ObjectModel.ObservableCollection<SeriesConfig>
                {
                    new SeriesConfig
                    {
                        Type = SeriesType.Area,
                        Name = "Revenue",
                        DataSource = new System.Collections.ObjectModel.ObservableCollection<ChartDataModel>
                        {
                            new ChartDataModel { XValue = "Jan", YValue = 10 },
                            new ChartDataModel { XValue = "Feb", YValue = 20 },
                            new ChartDataModel { XValue = "Mar", YValue = 30 },
                            new ChartDataModel { XValue = "Apr", YValue = 40 },
                            new ChartDataModel { XValue = "May", YValue = 50 },
                            new ChartDataModel { XValue = "Jun", YValue = 60 }
                        }
                    }
                }
            };
        }

        private string GetOfflineResponse(string prompt)
        {
            var responses = new Dictionary<string, string>
            {
                { "hello", "Hello! How can I help you today?" },
                { "help", "I'm here to assist you with various tasks including creating charts and analyzing data." },
                { "chart", "I can help you create various types of charts. What data would you like to visualize?" },
                { "graph", "I can generate graphs for your data. Please provide the data or describe what you'd like to visualize." }
            };

            var lowerPrompt = prompt.ToLower();
            foreach (var kvp in responses)
            {
                if (lowerPrompt.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Please connect to your preferred AI service for real-time queries.";
        }

        public bool IsCredentialValid => _isCredentialValid;
    }
}