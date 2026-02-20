/*
╒═════════════════════════════════════════════════════════════════════════════╕
│  File:  ErrorViewModel.cs	                                Date: 1/4/2026    │
╞═════════════════════════════════════════════════════════════════════════════╡
│																			  │
│        Model for handling error information and request identifiers         │
│																			  │
│		  													                  │
╘═════════════════════════════════════════════════════════════════════════════╛
*/

namespace SmartStudent.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
