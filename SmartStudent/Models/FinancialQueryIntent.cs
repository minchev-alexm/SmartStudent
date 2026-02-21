/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  FinancialQueryIntent.cs				           Date: 2/21/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│             Represents the intent of a user's financial query               │
│		    Used to route deterministic responses vs AI responses			  │										  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/


using Microsoft.AspNetCore.Mvc;

namespace SmartStudent.Models
{
    public enum FinancialQueryIntent
    {
        None = 0,
        Balance,
        Income,
        Expenses,
        Budget
    }
}
