using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SmsService.Api.Contracts;
using SmsService.Api.Data.Entities;
using SmsService.Api.Services;

namespace SmsService.Api.Controllers;

/// <summary>
/// HTTP layer only: maps requests to MessageService and results to status codes.
/// [ApiController] returns 400 with field errors automatically when DataAnnotations validation fails.
/// </summary>
[ApiController]
[Route("messages")]
public class MessagesController(MessageService messages) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateMessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Send(SendMessageRequest request)
    {
        var result = await messages.CreateAsync(request);
        if (result.Message is null)
            return Problem(title: "Cannot send message", detail: result.Error, statusCode: StatusCodes.Status422UnprocessableEntity);

        // 202: saved and queued, not sent yet. The status is available from GET /messages/{id}.
        var id = result.Message.Id;
        return AcceptedAtAction(nameof(Get), new { id }, new CreateMessageResponse(id));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var message = await messages.GetAsync(id);
        return message == null ? NotFound() : Ok(MessageResponse.From(message));
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<MessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        string? status,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 200)] int pageSize = 50)
    {
        MessageStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            // Names only (case-insensitive); Enum.TryParse would also accept numbers like "2".
            var match = Enum.GetValues<MessageStatus>()
                .Where(s => s.ToString().Equals(status.Trim(), StringComparison.OrdinalIgnoreCase))
                .Cast<MessageStatus?>()
                .FirstOrDefault();
            if (match == null)
            {
                ModelState.AddModelError(nameof(status),
                    $"Unknown status '{status}'. Allowed: {string.Join(", ", Enum.GetNames<MessageStatus>())}.");
                return ValidationProblem();
            }
            statusFilter = match;
        }

        var (items, totalCount) = await messages.ListAsync(statusFilter, page, pageSize);
        return Ok(new PagedResponse<MessageResponse>(items.Select(MessageResponse.From).ToList(), page, pageSize, totalCount));
    }
}
