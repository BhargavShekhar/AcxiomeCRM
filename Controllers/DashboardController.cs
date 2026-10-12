using AcxiomCRM.Data; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize] public class DashboardController(ApplicationDbContext db) : Controller {
 public async Task<IActionResult> Index(DateTime? from, DateTime? to){
    var startDate = from ?? DateTime.Today.AddDays(-30);
    var endDate = to ?? DateTime.Today;
    if (endDate < startDate) { var tmp = startDate; startDate = endDate; endDate = tmp; }
    var effectiveEnd = endDate.Date.AddDays(1);

    var isAdmin = User.IsInRole("Admin");
    var isManager = User.IsInRole("Manager");
    var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var customers = db.Customers.AsQueryable();
    var leads = db.Leads.AsQueryable();
    var opps = db.Opportunities.AsQueryable();
    var followups = db.FollowUps.AsQueryable();

    if (!isAdmin && !isManager) {
       customers = customers.Where(x => x.OwnerId == uid);
       leads = leads.Where(x => x.AssignedTo == uid);
       opps = opps.Where(x => x.AssignedTo == uid);
       followups = followups.Where(x => x.AssignedTo == uid);
    }

    var customersInRange = customers.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
    var leadsInRange = leads.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
    var oppsInRange = opps.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
    var followupsInRange = followups.Where(x => x.FollowUpDate >= startDate.Date && x.FollowUpDate < effectiveEnd);

    var totalCustomers = await customers.CountAsync();
    var totalLeads = await leads.CountAsync();
    var newLeads = await leads.CountAsync(x => x.Status == "New");
    var qualifiedLeads = await leads.CountAsync(x => x.Status == "Qualified");
    var convertedLeads = await leads.CountAsync(x => x.Status == "Converted");
    var lostLeads = await leads.CountAsync(x => x.Status == "Lost");
    var openLeads = await leads.CountAsync(x => x.Status != "Lost" && x.Status != "Converted");

    var totalOpportunities = await opps.CountAsync();
    var openOpportunities = await opps.CountAsync(x => x.Status == "Open");
    var won = await opps.CountAsync(x => x.Stage == "Won");
    var lost = await opps.CountAsync(x => x.Stage == "Lost");
    var pipeline = (await opps.Where(x => x.Status == "Open").Select(x => x.Amount).ToListAsync()).Sum();
    var wonRevenue = (await opps.Where(x => x.Stage == "Won").Select(x => x.Amount).ToListAsync()).Sum();
    var averageDeal = won > 0 ? wonRevenue / won : 0m;

    var conversionRate = totalLeads > 0 ? (convertedLeads * 100m) / totalLeads : 0m;
    var winRate = totalOpportunities > 0 ? (won * 100m) / totalOpportunities : 0m;
    var upcomingFollowUps = await followups.CountAsync(x => x.FollowUpDate >= DateTime.Today && x.FollowUpDate <= DateTime.Today.AddDays(7) && x.Status == "Planned");
    var overdueFollowUps = await followups.CountAsync(x => x.FollowUpDate < DateTime.Today && x.Status == "Planned");

    ViewBag.Customers = await customersInRange.CountAsync();
    ViewBag.Leads = await leadsInRange.CountAsync();
    ViewBag.OpenLeads = openLeads;
    ViewBag.Opportunities = await oppsInRange.CountAsync();
    ViewBag.OpenOpportunities = openOpportunities;
    ViewBag.Won = won;
    ViewBag.Lost = lost;
    ViewBag.Pipeline = pipeline;
    ViewBag.TotalRevenue = wonRevenue;
    ViewBag.ConversionRate = conversionRate;
    ViewBag.WinRate = winRate;
    ViewBag.AverageDeal = averageDeal;
    ViewBag.UpcomingFollowUps = upcomingFollowUps;
    ViewBag.OverdueFollowUps = overdueFollowUps;
    ViewBag.TotalCustomers = totalCustomers;
    ViewBag.TotalLeads = totalLeads;
    ViewBag.ConvertedLeads = convertedLeads;
    ViewBag.TotalOpportunities = totalOpportunities;
    ViewBag.NewLeads = newLeads;
    ViewBag.QualifiedLeads = qualifiedLeads;
    ViewBag.DateFrom = startDate.Date.ToString("yyyy-MM-dd");
    ViewBag.DateTo = endDate.Date.ToString("yyyy-MM-dd");
    ViewBag.RangeLabel = $"{startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";

    ViewBag.LeadLabels = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
    ViewBag.LeadValues = await Task.WhenAll(new[] { "New", "Contacted", "Qualified", "Lost", "Converted" }
        .Select(async s => await leadsInRange.CountAsync(x => x.Status == s)));

    ViewBag.StageLabels = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
    ViewBag.StageValues = await Task.WhenAll(new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" }
        .Select(async s => await oppsInRange.CountAsync(x => x.Stage == s)));

    var monthLabels = new List<string>();
    var monthValues = new List<decimal>();
    var currentMonth = new DateTime(endDate.Year, endDate.Month, 1);
    for (var i = 5; i >= 0; i--) {
        var monthStart = currentMonth.AddMonths(-i);
        var nextMonth = monthStart.AddMonths(1);
        monthLabels.Add(monthStart.ToString("MMM yyyy"));
        var revenue = (await opps.Where(x => x.Stage == "Won" && x.CreatedDate >= monthStart && x.CreatedDate < nextMonth)
            .Select(x => x.Amount).ToListAsync()).Sum();
        monthValues.Add(revenue);
    }

    ViewBag.MonthLabels = monthLabels.ToArray();
    ViewBag.MonthValues = monthValues.Select(x => (double)x).ToArray();
    return View();
 }
}
