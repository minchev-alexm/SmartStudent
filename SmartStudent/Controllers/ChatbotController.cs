/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  ChatbotController.cs				            Date: 2/17/2026       │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│               Provides an API endpoint for the AI assistant                 │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudent.Data;
using SmartStudent.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartStudent.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;

        private const bool AlwaysUseAI = false;
        private const string ModelName = "qwen/qwen2.5-vl-7b";

        public ChatbotController(IHttpClientFactory httpClientFactory, ApplicationDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        // Detect intent using regex
        private static FinancialQueryIntent ClassifyIntent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return FinancialQueryIntent.None;

            message = message.ToLowerInvariant();

            // Explanation / analysis → ALWAYS AI
            if (message.Contains("why") ||
                message.Contains("explain") ||
                message.Contains("reason") ||
                message.Contains("how come"))
            {
                return FinancialQueryIntent.None;
            }

            //Only intercept numeric questions
            bool asksAmount =
                message.Contains("how much") ||
                message.Contains("total") ||
                message.Contains("what is") ||
                message.Contains("what's");

            if (!asksAmount)
                return FinancialQueryIntent.None;

            if (Regex.IsMatch(message, @"\b(balance|remaining|left|net)\b"))
                return FinancialQueryIntent.Balance;

            if (Regex.IsMatch(message, @"\b(income|earnings|salary|earned)\b"))
                return FinancialQueryIntent.Income;

            if (Regex.IsMatch(message, @"\b(expense|expenses|spend|spent|cost|costs)\b"))
                return FinancialQueryIntent.Expenses;

            if (Regex.IsMatch(message, @"\b(budget|planned|plan)\b"))
                return FinancialQueryIntent.Budget;

            return FinancialQueryIntent.None;
        }

        private static string FormatCurrency(decimal amount)
            => amount.ToString("C");

        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new { error = "UserMessage cannot be empty." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var now = DateTime.Now;

                // Aggregate monthly data
                var incomeTotal = await _db.Transactions
                    .Where(t => t.UserId == userId &&
                                t.Type == "Income" &&
                                t.Date.Month == now.Month &&
                                t.Date.Year == now.Year)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var expenseTotal = await _db.Transactions
                    .Where(t => t.UserId == userId &&
                                t.Type == "Expense" &&
                                t.Date.Month == now.Month &&
                                t.Date.Year == now.Year)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var plannedBudget = await _db.Budgets
                    .Where(b => b.UserId == userId)
                    .SumAsync(b => (decimal?)b.Planned) ?? 0;

                var actualBudget = await _db.Budgets
                    .Where(b => b.UserId == userId)
                    .SumAsync(b => (decimal?)b.Actual) ?? 0;

                var balance = incomeTotal - expenseTotal;

                // ✔ INTENT ROUTING
                var intent = ClassifyIntent(request.UserMessage);

                if (!AlwaysUseAI && intent != FinancialQueryIntent.None)
                {
                    var reply = intent switch
                    {
                        FinancialQueryIntent.Balance =>
                            $"Your current balance this month is {FormatCurrency(balance)}.",

                        FinancialQueryIntent.Income =>
                            $"Your total income this month is {FormatCurrency(incomeTotal)}.",

                        FinancialQueryIntent.Expenses =>
                            $"Your total expenses this month are {FormatCurrency(expenseTotal)}.",

                        FinancialQueryIntent.Budget =>
                            $"Your planned budget is {FormatCurrency(plannedBudget)} and actual spending is {FormatCurrency(actualBudget)}.",

                        _ => null
                    };

                    if (reply != null)
                        return Ok(new { aiMessage = reply });
                }

                // ✔ AI FALLBACK
                var systemPrompt = $@"
You are a personal finance assistant.

Here is the user's financial summary for the current month:
- Income: {FormatCurrency(incomeTotal)}
- Expenses: {FormatCurrency(expenseTotal)}
- Balance: {FormatCurrency(balance)}
- Planned Budget: {FormatCurrency(plannedBudget)}
- Actual Budget: {FormatCurrency(actualBudget)}

Use these numbers exactly when relevant.

User says: {request.UserMessage}
";

                var client = _httpClientFactory.CreateClient();

                var payload = new
                {
                    model = ModelName,
                    input = systemPrompt,
                    temperature = 0.7,
                    top_p = 0.9
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "https://eligibly-peaceless-ginette.ngrok-free.dev/api/v1/chat",
                    content);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { error = "AI API error" });

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var output = doc.RootElement
                                .GetProperty("output")[0]
                                .GetProperty("content");

                string aiMessage = output.ValueKind == JsonValueKind.Array
                    ? string.Join(" ", output.EnumerateArray().Select(x => x.GetString()))
                    : output.GetString();

                return Ok(new { aiMessage = aiMessage?.Trim() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error contacting AI model.",
                    details = ex.Message
                });
            }
        }
    }
}