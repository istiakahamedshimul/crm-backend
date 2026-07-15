namespace backend.Models;

public enum LeadSource { Facebook, WhatsApp, Website, PhoneCall, WalkIn, Referral, Signboard, Event, Agent, ManualEntry, Other }
public enum LeadPriority { Cold, Warm, Hot }
public enum LeadStatus { New, Assigned, Contacted, Interested, FollowUpNeeded, SiteVisitScheduled, Visited, Negotiation, InvoiceGenerated, Booked, Lost, NotInterested }
public enum FollowUpType { WhatsApp, PhoneCall, Facebook, PhysicalMeeting, OfficeVisit, SiteVisit, Sms, Email, Other }
public enum ProofType { WhatsAppScreenshot, CallRecording, FacebookScreenshot, PaymentProof, Other }
public enum ProjectType { Apartment, Flat, Plot, Land, CommercialSpace, Shop, OfficeSpace }
public enum ProjectStatus { Upcoming, Ongoing, Ready, Completed, SoldOut, Paused }
public enum InvoiceStatus { Draft, Generated, SentToCustomer, PartiallyPaid, Paid, Cancelled, Expired }
public enum PaymentMethod { Cash, BankTransfer, Cheque, MobileBanking, CardMachine, OnlineGateway, Other }
public enum PaymentStatus { Pending, Approved, Rejected }
public enum CommissionStatus { Pending, Approved, Rejected, Paid, Hold }
public enum VehicleBookingStatus { Pending, Approved, Rejected, Cancelled }
