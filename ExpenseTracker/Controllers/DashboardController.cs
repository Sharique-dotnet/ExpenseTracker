using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            //Last 7 days transaction
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransaction = await _context.Transactions
                                                                   .Include(c => c.Category)
                                                                   .Where(d => d.Date >= StartDate && d.Date <= EndDate)
                                                                   .ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransaction
                .Where(t => t.Category.Type == "Income")
                .Sum(a => a.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransaction
                .Where(t => t.Category.Type == "Expense")
                .Sum(a => a.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChart = SelectedTransaction
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(c => c.Category.CategoryId)
                .Select(d => new
                {
                    categoryTitleWithIcon = d.First().Category.Icon + " " + d.First().Category.Title,
                    amount = d.Sum(a => a.Amount),
                    formattedAmount = d.Sum(a => a.Amount).ToString("C0")
                })
                .OrderByDescending(a=>a.amount)
                .ToList();

            //Spline Chart - Income vs Expense

            //Income
            List<SplineChartData> IncomeSummary = SelectedTransaction
                .Where(t => t.Category.Type == "Income")
                .GroupBy(d => d.Date)
                .Select(s => new SplineChartData()
                {
                    day = s.First().Date.ToString("dd-MMM"),
                    income = s.Sum(a => a.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransaction
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(d => d.Date)
                .Select(s => new SplineChartData()
                {
                    day = s.First().Date.ToString("dd-MMM"),
                    expense = s.Sum(a => a.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last7Days=Enumerable.Range(0,7)
                .Select(i=> StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense
                                      };

            //Recent 5 Transaction
            ViewBag.RecentTransactions = await _context.Transactions
                                        .Include(c => c.Category)
                                        .OrderByDescending(d => d.Date)
                                        .Take(5)
                                        .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
