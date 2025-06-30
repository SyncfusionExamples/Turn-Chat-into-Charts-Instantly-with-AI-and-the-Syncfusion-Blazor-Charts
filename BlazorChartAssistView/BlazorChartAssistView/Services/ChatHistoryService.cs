using BlazorChartAssistView.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace BlazorChartAssistView.Services
{
    public class ChatHistoryService
    {
        private readonly string _dataPath;
        private readonly ILogger<ChatHistoryService> _logger;

        public ChatHistoryService(IWebHostEnvironment environment, ILogger<ChatHistoryService> logger)
        {
            _dataPath = Path.Combine(environment.ContentRootPath, "Data", "ChatHistory.json");
            _logger = logger;
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
        }

        public async Task<ObservableCollection<ChatHistoryModel>> LoadChatHistoriesAsync()
        {
            try
            {
                if (!File.Exists(_dataPath))
                    return new ObservableCollection<ChatHistoryModel>();

                var json = await File.ReadAllTextAsync(_dataPath);
                var histories = JsonSerializer.Deserialize<List<ChatHistoryModel>>(json) ?? new List<ChatHistoryModel>();
                return new ObservableCollection<ChatHistoryModel>(histories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat histories");
                return new ObservableCollection<ChatHistoryModel>();
            }
        }

        public async Task SaveChatHistoriesAsync(ObservableCollection<ChatHistoryModel> chatHistories)
        {
            try
            {
                var json = JsonSerializer.Serialize(chatHistories.ToList(), new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_dataPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat histories");
            }
        }
    

        public async Task AddChatHistoryAsync(ChatHistoryModel chatHistory)
        {
            var histories = await LoadChatHistoriesAsync();
            histories.Insert(0, chatHistory);
            await SaveChatHistoriesAsync(histories);
        }

        public async Task UpdateChatHistoryAsync(ChatHistoryModel updatedHistory)
        {
            var histories = await LoadChatHistoriesAsync();
            var existing = histories.FirstOrDefault(h =>
                h.ConversationCreatedDate.Date == updatedHistory.ConversationCreatedDate.Date &&
                h.Title == updatedHistory.Title);

            if (existing != null)
            {
                existing.Messages = updatedHistory.Messages;
                existing.Message = updatedHistory.Message;
                await SaveChatHistoriesAsync(histories);
            }
        }

        public async Task DeleteChatHistoryAsync(ChatHistoryModel chatHistory)
        {
            var histories = await LoadChatHistoriesAsync();
            var toRemove = histories.FirstOrDefault(h =>
                h.Title == chatHistory.Title &&
                h.ConversationCreatedDate.Date == chatHistory.ConversationCreatedDate.Date);

            if (toRemove != null)
            {
                histories.Remove(toRemove);
                await SaveChatHistoriesAsync(histories);
            }
        }
    }
}