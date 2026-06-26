using HungStore.Application.Chat.Dtos;
using HungStore.Application.Chat.Interfaces;
using HungStore.Application.Common;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Chat;

public class ChatService : IChatService
{
    private const int PreviewMaxLength = 200;

    private readonly IChatRepository _chatRepository;
    private readonly IChatNotifier _chatNotifier;

    public ChatService(IChatRepository chatRepository, IChatNotifier chatNotifier)
    {
        _chatRepository = chatRepository;
        _chatNotifier = chatNotifier;
    }

    public async Task<ConversationDto> GetOrCreateMyConversationAsync(string customerId, string customerName)
    {
        var conversation = await _chatRepository.GetByCustomerIdAsync(customerId);
        if (conversation is null)
        {
            conversation = await _chatRepository.AddConversationAsync(new Conversation { CustomerId = customerId, CustomerName = customerName });
        }

        return MapToDto(conversation);
    }

    public async Task<PagedResult<ConversationDto>> GetConversationsForAdminAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _chatRepository.GetConversationsPagedAsync(page, pageSize);

        return new PagedResult<ConversationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<PagedResult<ChatMessageDto>>> GetMessagesAsync(Guid conversationId, string userId, bool isAdmin, int page, int pageSize)
    {
        var conversation = await _chatRepository.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return ServiceResult<PagedResult<ChatMessageDto>>.Failure("Không tìm thấy hội thoại.");
        }

        if (!isAdmin && conversation.CustomerId != userId)
        {
            return ServiceResult<PagedResult<ChatMessageDto>>.Failure("Bạn không có quyền truy cập hội thoại này.");
        }

        var (items, totalCount) = await _chatRepository.GetMessagesPagedAsync(conversationId, page, pageSize);

        return ServiceResult<PagedResult<ChatMessageDto>>.Success(new PagedResult<ChatMessageDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<ChatMessageDto>> SendMessageAsync(Guid conversationId, string senderId, string senderName, bool isAdmin, SendMessageRequest request)
    {
        var conversation = await _chatRepository.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return ServiceResult<ChatMessageDto>.Failure("Không tìm thấy hội thoại.");
        }

        if (!isAdmin && conversation.CustomerId != senderId)
        {
            return ServiceResult<ChatMessageDto>.Failure("Bạn không có quyền gửi tin nhắn trong hội thoại này.");
        }

        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderName = senderName,
            IsFromAdmin = isAdmin,
            Content = request.Content
        };
        await _chatRepository.AddMessageAsync(message);

        conversation.LastMessagePreview = request.Content.Length > PreviewMaxLength
            ? request.Content[..PreviewMaxLength]
            : request.Content;
        conversation.LastMessageAt = message.CreatedAt;
        if (isAdmin)
        {
            conversation.UnreadCountForCustomer++;
        }
        else
        {
            conversation.UnreadCountForAdmin++;
        }
        await _chatRepository.UpdateConversationAsync(conversation);

        var messageDto = MapToDto(message);
        await _chatNotifier.NotifyMessageAsync(messageDto);
        await _chatNotifier.NotifyConversationUpdatedAsync(MapToDto(conversation));

        return ServiceResult<ChatMessageDto>.Success(messageDto);
    }

    public async Task<ServiceResult<bool>> MarkAsReadAsync(Guid conversationId, string userId, bool isAdmin)
    {
        var conversation = await _chatRepository.GetByIdAsync(conversationId);
        if (conversation is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy hội thoại.");
        }

        if (!isAdmin && conversation.CustomerId != userId)
        {
            return ServiceResult<bool>.Failure("Bạn không có quyền truy cập hội thoại này.");
        }

        if (isAdmin)
        {
            conversation.UnreadCountForAdmin = 0;
        }
        else
        {
            conversation.UnreadCountForCustomer = 0;
        }

        await _chatRepository.UpdateConversationAsync(conversation);
        await _chatNotifier.NotifyConversationUpdatedAsync(MapToDto(conversation));

        return ServiceResult<bool>.Success(true);
    }

    private static ConversationDto MapToDto(Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            CustomerId = conversation.CustomerId,
            CustomerName = conversation.CustomerName,
            LastMessagePreview = conversation.LastMessagePreview,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCountForAdmin = conversation.UnreadCountForAdmin,
            UnreadCountForCustomer = conversation.UnreadCountForCustomer
        };
    }

    private static ChatMessageDto MapToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            IsFromAdmin = message.IsFromAdmin,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }
}
