using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Chat.Dtos;
using ShopeeClone.Application.Common;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ChatControllerTests
{
    private readonly HttpClient _client;

    public ChatControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyConversation_FirstCall_CreatesConversationForCustomer()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDto>();
        conversation!.CustomerName.Should().Be("Test Customer");
        conversation.UnreadCountForAdmin.Should().Be(0);
    }

    [Fact]
    public async Task GetMyConversation_CalledTwice_ReturnsSameConversation()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var first = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);
        var second = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);

        var firstDto = await first.Content.ReadFromJsonAsync<ConversationDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ConversationDto>();
        firstDto!.Id.Should().Be(secondDto!.Id);
    }

    [Fact]
    public async Task SendMessage_AsCustomer_AppearsInAdminConversationList()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        var conversationResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);
        var conversation = await conversationResponse.Content.ReadFromJsonAsync<ConversationDto>();

        var sendResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation!.Id}/messages", customerToken,
            new SendMessageRequest { Content = "Sản phẩm này còn hàng không shop?" });
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversations", adminToken, null);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<ConversationDto>>();
        var found = list!.Items.Should().Contain(c => c.Id == conversation.Id).Subject;
        found.UnreadCountForAdmin.Should().Be(1);
        found.LastMessagePreview.Should().Be("Sản phẩm này còn hàng không shop?");
    }

    [Fact]
    public async Task SendMessage_ToAnotherCustomersConversation_ReturnsBadRequest()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var intruderToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var conversationResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", ownerToken, null);
        var conversation = await conversationResponse.Content.ReadFromJsonAsync<ConversationDto>();

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation!.Id}/messages", intruderToken,
            new SendMessageRequest { Content = "Tin nhắn trái phép" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConversations_AsCustomer_IsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversations", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminReply_ThenMarkRead_ResetsUnreadCountForAdminToZero()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var conversationResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);
        var conversation = await conversationResponse.Content.ReadFromJsonAsync<ConversationDto>();

        await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation!.Id}/messages", customerToken,
            new SendMessageRequest { Content = "Hỏi giá ship." });
        await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation.Id}/messages", customerToken,
            new SendMessageRequest { Content = "Còn câu hỏi thêm." });

        var listBeforeRead = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversations", adminToken, null);
        var beforeRead = await listBeforeRead.Content.ReadFromJsonAsync<PagedResultDto<ConversationDto>>();
        beforeRead!.Items.Should().Contain(c => c.Id == conversation.Id && c.UnreadCountForAdmin == 2);

        var readResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation.Id}/read", adminToken, null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversations", adminToken, null);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<ConversationDto>>();
        var found = list!.Items.Should().Contain(c => c.Id == conversation.Id).Subject;
        found.UnreadCountForAdmin.Should().Be(0);
    }

    [Fact]
    public async Task GetMessages_AfterSending_ReturnsMessageWithSenderInfo()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var conversationResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/chat/conversation", customerToken, null);
        var conversation = await conversationResponse.Content.ReadFromJsonAsync<ConversationDto>();

        await SendAuthorizedAsync(HttpMethod.Post, $"/api/chat/conversations/{conversation!.Id}/messages", customerToken,
            new SendMessageRequest { Content = "Tôi muốn đổi địa chỉ giao hàng." });

        var messagesResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/chat/conversations/{conversation.Id}/messages", adminToken, null);
        var messages = await messagesResponse.Content.ReadFromJsonAsync<PagedResultDto<ChatMessageDto>>();

        messages!.Items.Should().ContainSingle(m => m.Content == "Tôi muốn đổi địa chỉ giao hàng." && !m.IsFromAdmin);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
