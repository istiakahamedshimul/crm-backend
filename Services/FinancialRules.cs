using backend.Models;
namespace backend.Services;
public static class FinancialRules
{
 public static IReadOnlyList<(int Number,DateTime DueDate,decimal Amount)> GenerateSchedule(decimal balance,DateTime start,decimal monthly,int count){if(balance<=0||monthly<=0||count<=0)throw new ArgumentOutOfRangeException();var rows=new List<(int,DateTime,decimal)>();for(var n=1;n<=count&&balance>0;n++){var amount=Math.Min(balance,monthly);rows.Add((n,start.Date.AddMonths(n-1),amount));balance-=amount;}if(balance>0)throw new InvalidOperationException("EMI schedule does not cover the agreement balance.");return rows;}
 public static InstallmentStatus Status(decimal expected,decimal paid,DateTime due,DateTime today)=>paid>=expected?InstallmentStatus.Paid:paid>0?InstallmentStatus.PartiallyPaid:due.Date<today.Date?InstallmentStatus.Overdue:due.Date==today.Date?InstallmentStatus.Due:InstallmentStatus.Upcoming;
 public static decimal TotalPaid(IEnumerable<(decimal Amount,PaymentStatus Status,bool Reversed)> rows)=>rows.Where(x=>x.Status==PaymentStatus.Approved&&!x.Reversed).Sum(x=>x.Amount);
 public static decimal Outstanding(decimal agreed,decimal paid)=>Math.Max(0,agreed-paid);
 public static void AllocateOldest(IList<EmiInstallment> installments,decimal amount,DateTime paidAt){foreach(var installment in installments.OrderBy(x=>x.InstallmentNumber).Where(x=>x.PaidAmount<x.ExpectedAmount)){if(amount<=0)break;var applied=Math.Min(amount,installment.ExpectedAmount-installment.PaidAmount);installment.PaidAmount+=applied;amount-=applied;if(installment.PaidAmount>=installment.ExpectedAmount)installment.PaidAt=paidAt;}}
}
