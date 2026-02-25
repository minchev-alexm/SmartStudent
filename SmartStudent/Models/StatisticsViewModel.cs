namespace SmartStudent.Models
{
    public class StatisticsViewModel
    {
        public decimal IncomeTotal { get; set; }
        public decimal ExpenseTotal { get; set; }
        public decimal Balance => IncomeTotal - ExpenseTotal;

        public List<string> Months { get; set; } = new();
        public List<decimal> MonthlyIncome { get; set; } = new();
        public List<decimal> MonthlyExpense { get; set; } = new();

        public List<string> ExpenseCategories { get; set; } = new();
        public List<decimal> ExpenseCategoryTotals { get; set; } = new();
        public List<Transaction> RecentTransactions { get; set; } = new();
    }
}
