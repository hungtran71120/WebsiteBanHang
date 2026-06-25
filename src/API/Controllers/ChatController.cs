using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Chat.Dtos;
using ShopeeClone.Application.Chat.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IValidator<SendMessageRequest> _sendMessageValidator;

    public ChatController(IChatService chatService, IValidator<SendMessageRequest> sendMessageValidator)
    {
        _chatService = chatService;
        _sendMessageValidator = sendMessageValidator;
    }

    [HttpGet("conversation")]
    public async Task<IActionResult> GetMyConversation()
    {
        var conversation = await _chatService.GetOrCreateMyConversationAsync(CurrentUserId, CurrentUserName);
        return Ok(conversation);
    }

    [HttpGet("conversations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _chatService.GetConversationsForAdminAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var result = await _chatService.GetMessagesAsync(id, CurrentUserId, IsAdmin, page, pageSize);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, SendMessageRequest request)
    {
        var validation = await _sendMessageValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _chatService.SendMessageAsync(id, CurrentUserId, CurrentUserName, IsAdmin, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("conversations/{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _chatService.MarkAsReadAsync(id, CurrentUserId, IsAdmin);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    private bool IsAdmin => User.IsInRole("Admin");
}
