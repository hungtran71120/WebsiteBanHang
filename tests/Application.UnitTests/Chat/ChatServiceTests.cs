using FluentAssertions;
using Moq;
using HungStore.Application.Chat;
using HungStore.Application.Chat.Dtos;
using HungStore.Application.Chat.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Chat;

public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _chatRepositoryMock = new();
    private readonly Mock<IChatNotifier> _chatNotifierMock = new();
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _sut = new ChatService(_chatRepositoryMock.Object, _chatNotifierMock.Object);
    }

    [Fact]
    public async Task GetOrCreateMyConversationAsync_NoExistingConversation_CreatesNew()
    {
        _chatRepositoryMock.Setup(x => x.GetByCustomerIdAsync("customer-1")).ReturnsAsync((Conversation?)null);
        _chatRepositoryMock.Setup(x => x.AddConversationAsync(It.IsAny<Conversation>())).ReturnsAsync((Conversation c) => c);

        var result = await _sut.GetOrCreateMyConversationAsync("customer-1", "Nguyễn Văn A");

        result.CustomerId.Should().Be("customer-1");
        result.CustomerName.Should().Be("Nguyễn Văn A");
        _chatRepositoryMock.Verify(x => x.AddConversationAsync(It.Is<Conversation>(c => c.CustomerId == "customer-1")), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateMyConversationAsync_ExistingConversation_ReturnsItWithoutCreating()
    {
        var conversation = new Conversation { CustomerId = "customer-1", CustomerName = "Nguyễn Văn A" };
        _chatRepositoryMock.Setup(x => x.GetByCustomerIdAsync("customer-1")).ReturnsAsync(conversation);

        var result = await _sut.GetOrCreateMyConversationAsync("customer-1", "Nguyễn Văn A");

        result.Id.Should().Be(conversation.Id);
        _chatRepositoryMock.Verify(x => x.AddConversationAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task GetMessagesAsync_UnknownConversation_ReturnsFailure()
    {
        var conversationId = Guid.NewGuid();
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversationId)).ReturnsAsync((Conversation?)null);

        var result = await _sut.GetMessagesAsync(conversationId, "customer-1", false, 1, 30);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMessagesAsync_CustomerNotOwner_ReturnsFailure()
    {
        var conversation = new Conversation { CustomerId = "owner" };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);

        var result = await _sut.GetMessagesAsync(conversation.Id, "someone-else", false, 1, 30);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMessagesAsync_Admin_CanAccessAnyConversation()
    {
        var conversation = new Conversation { CustomerId = "owner" };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);
        _chatRepositoryMock.Setup(x => x.GetMessagesPagedAsync(conversation.Id, 1, 30))
            .ReturnsAsync((new List<ChatMessage>(), 0));

        var result = await _sut.GetMessagesAsync(conversation.Id, "admin-1", true, 1, 30);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_UnknownConversation_ReturnsFailure()
    {
        var conversationId = Guid.NewGuid();
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversationId)).ReturnsAsync((Conversation?)null);

        var result = await _sut.SendMessageAsync(conversationId, "customer-1", "A", false, new SendMessageRequest { Content = "Xin chào" });

        result.Succeeded.Should().BeFalse();
        _chatRepositoryMock.Verify(x => x.AddMessageAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_CustomerNotOwner_ReturnsFailure()
    {
        var conversation = new Conversation { CustomerId = "owner" };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);

        var result = await _sut.SendMessageAsync(conversation.Id, "someone-else", "A", false, new SendMessageRequest { Content = "Xin chào" });

        result.Succeeded.Should().BeFalse();
        _chatRepositoryMock.Verify(x => x.AddMessageAsync(It.IsAny<ChatMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_FromCustomer_IncrementsUnreadCountForAdminAndNotifies()
    {
        var conversation = new Conversation { CustomerId = "customer-1", UnreadCountForAdmin = 2 };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);
        _chatRepositoryMock.Setup(x => x.AddMessageAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
        _chatRepositoryMock.Setup(x => x.UpdateConversationAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync(conversation.Id, "customer-1", "Nguyễn Văn A", false, new SendMessageRequest { Content = "Sản phẩm này còn hàng không?" });

        result.Succeeded.Should().BeTrue();
        conversation.UnreadCountForAdmin.Should().Be(3);
        conversation.UnreadCountForCustomer.Should().Be(0);
        conversation.LastMessagePreview.Should().Be("Sản phẩm này còn hàng không?");
        _chatNotifierMock.Verify(x => x.NotifyMessageAsync(It.Is<ChatMessageDto>(m => m.SenderId == "customer-1")), Times.Once);
        _chatNotifierMock.Verify(x => x.NotifyConversationUpdatedAsync(It.IsAny<ConversationDto>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_FromAdmin_IncrementsUnreadCountForCustomer()
    {
        var conversation = new Conversation { CustomerId = "customer-1" };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);
        _chatRepositoryMock.Setup(x => x.AddMessageAsync(It.IsAny<ChatMessage>())).Returns(Task.CompletedTask);
        _chatRepositoryMock.Setup(x => x.UpdateConversationAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync(conversation.Id, "admin-1", "Shop", true, new SendMessageRequest { Content = "Còn hàng bạn nhé." });

        result.Succeeded.Should().BeTrue();
        conversation.UnreadCountForCustomer.Should().Be(1);
        conversation.UnreadCountForAdmin.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsReadAsync_CustomerNotOwner_ReturnsFailure()
    {
        var conversation = new Conversation { CustomerId = "owner" };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);

        var result = await _sut.MarkAsReadAsync(conversation.Id, "someone-else", false);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_Admin_ResetsUnreadCountForAdminToZero()
    {
        var conversation = new Conversation { CustomerId = "customer-1", UnreadCountForAdmin = 5 };
        _chatRepositoryMock.Setup(x => x.GetByIdAsync(conversation.Id)).ReturnsAsync(conversation);
        _chatRepositoryMock.Setup(x => x.UpdateConversationAsync(It.IsAny<Conversation>())).Returns(Task.CompletedTask);

        var result = await _sut.MarkAsReadAsync(conversation.Id, "admin-1", true);

        result.Succeeded.Should().BeTrue();
        conversation.UnreadCountForAdmin.Should().Be(0);
    }
}
