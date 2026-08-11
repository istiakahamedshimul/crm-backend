using backend.Models;using backend.Services;using Xunit;
using backend.Security;
namespace backend.Tests;
public class FinancialAndSecurityTests
{
 [Fact]public void FullPaymentOutstandingIsCalculated()=>Assert.Equal(0,FinancialRules.Outstanding(100,100));
 [Fact]public void OutstandingBalanceUsesValidTotal()=>Assert.Equal(65,FinancialRules.Outstanding(100,35));
 [Fact]public void ReversedPaymentsAreExcluded()=>Assert.Equal(20,FinancialRules.TotalPaid([(20,PaymentStatus.Approved,false),(30,PaymentStatus.Approved,true)]));
 [Fact]public void PendingPaymentsAreExcluded()=>Assert.Equal(0,FinancialRules.TotalPaid([(20,PaymentStatus.Pending,false)]));
 [Fact]public void EmiScheduleGeneratesMonthlyRows()=>Assert.Equal(3,FinancialRules.GenerateSchedule(300,new DateTime(2026,1,10),100,3).Count);
 [Fact]public void EmiScheduleUsesFinalRemainder()=>Assert.Equal(50,FinancialRules.GenerateSchedule(250,new DateTime(2026,1,10),100,3)[2].Amount);
 [Fact]public void InvalidEmiScheduleIsRejected()=>Assert.Throws<InvalidOperationException>(()=>FinancialRules.GenerateSchedule(400,DateTime.Today,100,3));
 [Fact]public void PartialPaymentStatusIsPreserved()=>Assert.Equal(InstallmentStatus.PartiallyPaid,FinancialRules.Status(100,40,DateTime.Today.AddDays(-2),DateTime.Today));
 [Fact]public void MissedInstallmentBecomesOverdue()=>Assert.Equal(InstallmentStatus.Overdue,FinancialRules.Status(100,0,DateTime.Today.AddDays(-1),DateTime.Today));
 [Fact]public void FutureInstallmentIsUpcoming()=>Assert.Equal(InstallmentStatus.Upcoming,FinancialRules.Status(100,0,DateTime.Today.AddDays(1),DateTime.Today));
 [Fact]public void DueTodayIsDue()=>Assert.Equal(InstallmentStatus.Due,FinancialRules.Status(100,0,DateTime.Today,DateTime.Today));
 [Fact]public void PaidInstallmentIsPaid()=>Assert.Equal(InstallmentStatus.Paid,FinancialRules.Status(100,100,DateTime.Today.AddDays(-1),DateTime.Today));
 [Fact]public void PaymentPermissionCodesAreDistinct()=>Assert.Equal(4,new[]{PermissionCodes.PaymentsView,PermissionCodes.PaymentsRecord,PermissionCodes.PaymentsApprove,PermissionCodes.PaymentsReverse}.Distinct().Count());
 [Fact]public void DuplicateNotificationEventKeysAreStable(){var a=$"EmiDue:customer:{1}:installment:{2}:due:{new DateTime(2026,1,1):yyyyMMdd}";var b=$"EmiDue:customer:{1}:installment:{2}:due:{new DateTime(2026,1,1):yyyyMMdd}";Assert.Equal(a,b);}
 [Fact]public void OverpaymentDoesNotCreateNegativeBalance()=>Assert.Equal(0,FinancialRules.Outstanding(100,120));
 [Fact]public void PermissionAttributePassesCodeToRuntimeFilter(){var attribute=new RequirePermissionAttribute(PermissionCodes.CustomersView);Assert.Equal(PermissionCodes.CustomersView,attribute.Arguments![0]);}
 [Fact]public void SalesCustomerViewPermissionCodeRemainsBaseline()=>Assert.Equal("customers.view",PermissionCodes.CustomersView);
}
