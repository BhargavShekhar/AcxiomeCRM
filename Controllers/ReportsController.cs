using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var startDate = from ?? DateTime.Today.AddDays(-30);
        var endDate = to ?? DateTime.Today;
        if (endDate < startDate)
        {
            var tmp = startDate;
            startDate = endDate;
            endDate = tmp;
        }

        var effectiveEnd = endDate.Date.AddDays(1);
        var isAdmin = User.IsInRole("Admin");
        var isManager = User.IsInRole("Manager");
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var customers = db.Customers.AsQueryable();
        var leads = db.Leads.AsQueryable();
        var opps = db.Opportunities.AsQueryable();
        var followups = db.FollowUps.AsQueryable();

        if (!isAdmin && !isManager)
        {
            customers = customers.Where(x => x.OwnerId == uid);
            leads = leads.Where(x => x.AssignedTo == uid);
            opps = opps.Where(x => x.AssignedTo == uid);
            followups = followups.Where(x => x.AssignedTo == uid);
        }

        var filteredCustomers = customers.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
        var filteredLeads = leads.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
        var filteredOpps = opps.Where(x => x.CreatedDate >= startDate.Date && x.CreatedDate < effectiveEnd);
        var filteredFollowUps = followups.Where(x => x.FollowUpDate >= startDate.Date && x.FollowUpDate < effectiveEnd);

        var totalCustomers = await filteredCustomers.CountAsync();
        var totalLeads = await filteredLeads.CountAsync();
        var newLeads = await filteredLeads.CountAsync(x => x.Status == "New");
        var contactedLeads = await filteredLeads.CountAsync(x => x.Status == "Contacted");
        var qualifiedLeads = await filteredLeads.CountAsync(x => x.Status == "Qualified");
        var convertedLeads = await filteredLeads.CountAsync(x => x.Status == "Converted");
        var lostLeads = await filteredLeads.CountAsync(x => x.Status == "Lost");

        var totalOpps = await filteredOpps.CountAsync();
        var openOpps = await filteredOpps.CountAsync(x => x.Status == "Open");
        var wonOpps = await filteredOpps.CountAsync(x => x.Stage == "Won");
        var lostOpps = await filteredOpps.CountAsync(x => x.Stage == "Lost");
        var pipeline = (await filteredOpps.Where(x => x.Status == "Open").Select(x => x.Amount).ToListAsync()).Sum();
        var wonRevenue = (await filteredOpps.Where(x => x.Stage == "Won").Select(x => x.Amount).ToListAsync()).Sum();
        var avgDeal = wonOpps > 0 ? wonRevenue / wonOpps : 0m;
        var conversionRate = totalLeads > 0 ? convertedLeads * 100m / totalLeads : 0m;
        var winRate = totalOpps > 0 ? wonOpps * 100m / totalOpps : 0m;

        var overdueFollowUps = await filteredFollowUps.CountAsync(x => x.Status == "Planned" && x.FollowUpDate < DateTime.Today);
        var upcomingFollowUps = await filteredFollowUps.CountAsync(x => x.Status == "Planned" && x.FollowUpDate >= DateTime.Today && x.FollowUpDate <= DateTime.Today.AddDays(7));

        var leadStatusLabels = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
        var leadStatusValues = await Task.WhenAll(leadStatusLabels.Select(async status => await filteredLeads.CountAsync(x => x.Status == status)));

        var stageLabels = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var stageValues = await Task.WhenAll(stageLabels.Select(async stage => await filteredOpps.CountAsync(x => x.Stage == stage)));

        var monthLabels = new List<string>();
        var monthValues = new List<decimal>();
        var currentMonth = new DateTime(endDate.Year, endDate.Month, 1);
        for (var i = 5; i >= 0; i--)
        {
            var monthStart = currentMonth.AddMonths(-i);
            var nextMonth = monthStart.AddMonths(1);
            monthLabels.Add(monthStart.ToString("MMM yyyy"));
            var revenue = (await filteredOpps.Where(x => x.Stage == "Won" && x.CreatedDate >= monthStart && x.CreatedDate < nextMonth)
                .Select(x => x.Amount)
                .ToListAsync()).Sum();
            monthValues.Add(revenue);
        }

        var userMap = await db.Users.ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? "Unknown") : u.FullName);
        var opportunitiesList = await filteredOpps.ToListAsync();
        var leadsList = await filteredLeads.ToListAsync();

        var salesPerformance = opportunitiesList
            .GroupBy(x => x.AssignedTo ?? "Unassigned")
            .Select(g => new
            {
                UserId = g.Key,
                LeadCount = leadsList.Count(l => l.AssignedTo == g.Key),
                OpportunityCount = g.Count(),
                PipelineValue = g.Where(x => x.Status == "Open").Sum(x => x.Amount),
                WonRevenue = g.Where(x => x.Stage == "Won").Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.WonRevenue + x.PipelineValue)
            .Take(5)
            .ToList();

        var performanceRows = salesPerformance
            .Select(x => (
                Name: x.UserId == "Unassigned" ? "Unassigned" : (userMap.TryGetValue(x.UserId, out var mappedName) ? mappedName : x.UserId),
                LeadCount: x.LeadCount,
                OpportunityCount: x.OpportunityCount,
                PipelineValue: x.PipelineValue,
                WonRevenue: x.WonRevenue
            ))
            .ToList();

        ViewBag.DateFrom = startDate.Date.ToString("yyyy-MM-dd");
        ViewBag.DateTo = endDate.Date.ToString("yyyy-MM-dd");
        ViewBag.RangeLabel = $"{startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";

        ViewBag.TotalCustomers = totalCustomers;
        ViewBag.TotalLeads = totalLeads;
        ViewBag.NewLeads = newLeads;
        ViewBag.ContactedLeads = contactedLeads;
        ViewBag.QualifiedLeads = qualifiedLeads;
        ViewBag.ConvertedLeads = convertedLeads;
        ViewBag.LostLeads = lostLeads;

        ViewBag.TotalOpportunities = totalOpps;
        ViewBag.OpenOpportunities = openOpps;
        ViewBag.WonOpportunities = wonOpps;
        ViewBag.LostOpportunities = lostOpps;
        ViewBag.PipelineValue = pipeline;
        ViewBag.WonRevenue = wonRevenue;
        ViewBag.AverageDeal = avgDeal;
        ViewBag.ConversionRate = conversionRate;
        ViewBag.WinRate = winRate;

        ViewBag.OverdueFollowUps = overdueFollowUps;
        ViewBag.UpcomingFollowUps = upcomingFollowUps;

        ViewBag.LeadStatusLabels = leadStatusLabels;
        ViewBag.LeadStatusValues = leadStatusValues;
        ViewBag.StageLabels = stageLabels;
        ViewBag.StageValues = stageValues;
        ViewBag.MonthLabels = monthLabels.ToArray();
        ViewBag.MonthValues = monthValues.Select(x => (double)x).ToArray();
        ViewBag.PerformanceRows = performanceRows;

        return View();
    }
}

