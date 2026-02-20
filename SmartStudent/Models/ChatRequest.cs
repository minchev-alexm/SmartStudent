/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  ChatRequest.cs				                    Date: 1/5/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│               Represents a user message sent to AI Assitant                 │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

namespace SmartStudent.Models
{
    public class ChatRequest
    {
        public required string UserMessage { get; set; }
    }
}
