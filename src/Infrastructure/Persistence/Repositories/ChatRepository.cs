using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _context;

    public ChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Conversation?> GetByIdAsync(Guid id)
    {
        return _context.Conversations.FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<Conversation?> GetByCustomerIdAsync(string customerId)
    {
        return _context.Conversations.FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Conversation> AddConversationAsync(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        try
        {
            await _context.SaveChangesAsync();
            return conversation;
        }
        catch (DbUpdateException)
        {
            // Another concurrent connection from the same customer (e.g. a second browser tab)
            // may have already created this conversation between our GetByCustomerIdAsync check
            // and this insert (CustomerId has a unique index) — fall back to the row that won the race.
            _context.Entry(conversation).State = EntityState.Detached;
            var existing = await GetByCustomerIdAsync(conversation.CustomerId);
            if (existing is null)
            {
                throw;
            }
            return existing;
        }
    }

    public async Task UpdateConversationAsync(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetConversationsPagedAsync(int page, int pageSize)
    {
        var query = _context.Conversations.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesPagedAsync(Guid conversationId, int page, int pageSize)
    {
        var query = _context.ChatMessages.AsNoTracking().Where(m => m.ConversationId == conversationId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }
}
