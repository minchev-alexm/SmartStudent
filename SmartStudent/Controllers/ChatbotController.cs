/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  ChatbotController.cs				            Date: 2/20/2026       │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│  Provides an authenticated API endpoint for the SmartStudent AI assistant.  │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudent.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using SmartStudent.Models;
using System.Text.Json;

namespace SmartStudent.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;

        // Flag to control if queries asking for data such as income etc should be answered by AI or by system - bypassing AI
        private const bool AlwaysUseAI = false;

        string modelName = "qwen/qwen2.5-vl-7b";

        public ChatbotController(IHttpClientFactory httpClientFactory, ApplicationDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        //Fix currency fomat
        string FormatCurrency(decimal amount)
        {
            return amount < 0
                ? "-" + Math.Abs(amount).ToString("C")
                : amount.ToString("C");
        }

        //POST for SendMessage
        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new { error = "UserMessage cannot be empty." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var now = DateTime.Now;
                int currentMonth = now.Month;
                int currentYear = now.Year;

                // Fetch user's financial data for the current month
                var incomeTotal = await _db.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Income" &&
                                t.Date.Month == currentMonth && t.Date.Year == currentYear)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var expenseTotal = await _db.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Expense" &&
                                t.Date.Month == currentMonth && t.Date.Year == currentYear)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var balance = incomeTotal - expenseTotal;

                var totalPlanned = await _db.Budgets
                    .Where(b => b.UserId == userId)
                    .SumAsync(b => (decimal?)b.Planned) ?? 0;

                var totalActual = await _db.Budgets
                    .Where(b => b.UserId == userId)
                    .SumAsync(b => (decimal?)b.Actual) ?? 0;

                var messageLower = request.UserMessage.ToLower();
                string numericAnswer = null;

                // Only bypass AI if flag is false
                if (!AlwaysUseAI)
                {
                    if (messageLower.Contains("balance"))
                        numericAnswer = $"Your current balance for this month is {FormatCurrency(balance)}.";
                    else if (messageLower.Contains("income"))
                        numericAnswer = $"Your total income for this month is {FormatCurrency(incomeTotal)}.";
                    else if (messageLower.Contains("expense") || messageLower.Contains("spend"))
                        numericAnswer = $"Your total expenses for this month are {FormatCurrency(expenseTotal)}.";
                    else if (messageLower.Contains("budget"))
                        numericAnswer = $"Your planned budget for this month is {FormatCurrency(totalPlanned)} and actual spending is {FormatCurrency(totalActual)}.";

                    if (numericAnswer != null)
                        return Ok(new { aiMessage = numericAnswer });
                }

                // Always send to AI (with numbers) if AlwaysUseAI=true or question is complex
                var systemPrompt = $@"
You are a personal finance assistant. 
Here is the user's financial summary for the current month (DO NOT change these numbers):
- Income: {FormatCurrency(incomeTotal)}
- Expenses: {FormatCurrency(expenseTotal)}
- Balance: {FormatCurrency(balance)}
- Planned Budget: {FormatCurrency(totalPlanned)}
- Actual Budget: {FormatCurrency(totalActual)}

Respond helpfully to the user's question.
User says: {request.UserMessage}
";

                var client = _httpClientFactory.CreateClient();
                var apiUrl = "http://localhost:1234/api/v1/chat";

                var payload = new
                {
                    model = modelName,
                    input = systemPrompt,
                    temperature = 0.7,
                    top_p = 0.9
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { error = "AI API error" });

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var outputElement = doc.RootElement.GetProperty("output")[0].GetProperty("content");
                string aiMessage = outputElement.ValueKind == JsonValueKind.Array
                    ? string.Join(" ", outputElement.EnumerateArray().Select(x => x.GetString()))
                    : outputElement.GetString();

                return Ok(new { aiMessage = aiMessage?.Trim() });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new { error = "Error contacting AI model.", details = ex.Message });
            }
        }
    }
}
