using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IChatRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation?> GetByCustomerIdAsync(string customerId);
    Task<Conversation> AddConversationAsync(Conversation conversation);
    Task UpdateConversationAsync(Conversation conversation);
    Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetConversationsPagedAsync(int page, int pageSize);
    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesPagedAsync(Guid conversationId, int page, int pageSize);
    Task AddMessageAsync(ChatMessage message);
}
