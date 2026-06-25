using ShopeeClone.Application.Chat.Dtos;
using ShopeeClone.Application.Common;

namespace ShopeeClone.Application.Chat.Interfaces;

public interface IChatService
{
    Task<ConversationDto> GetOrCreateMyConversationAsync(string customerId, string customerName);
    Task<PagedResult<ConversationDto>> GetConversationsForAdminAsync(int page, int pageSize);
    Task<ServiceResult<PagedResult<ChatMessageDto>>> GetMessagesAsync(Guid conversationId, string userId, bool isAdmin, int page, int pageSize);
    Task<ServiceResult<ChatMessageDto>> SendMessageAsync(Guid conversationId, string senderId, string senderName, bool isAdmin, SendMessageRequest request);
    Task<ServiceResult<bool>> MarkAsReadAsync(Guid conversationId, string userId, bool isAdmin);
}
