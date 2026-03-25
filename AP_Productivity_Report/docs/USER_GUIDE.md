# AP Specialist Productivity Report — User Guide

This guide walks you through everything you need to know to use the AP Specialist Productivity Report. It covers how to run a report, read the results, filter and sort data, and export to Excel.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Running a Report](#running-a-report)
- [Understanding the Results](#understanding-the-results)
  - [Rollup Summary Tables](#rollup-summary-tables)
- [Filtering Results](#filtering-results)
- [Sorting](#sorting)
- [Pagination](#pagination)
- [Exporting Data](#exporting-data)
- [Tips and Shortcuts](#tips-and-shortcuts)

---

## Getting Started

To open the report, simply navigate to the URL provided by your administrator in your web browser (Chrome, Edge, or Firefox all work well). You will be signed in automatically using your Windows account — no separate login is required.

When the page loads, you will see:

- A **filter bar** at the top with options for workflow, date range, and date basis
- An empty report area below, ready for you to run your first query

If this is your very first visit, the date range will default to **Month to Date** so you can get started quickly.

---

## Running a Report

Follow these steps to generate a report:

1. **Select a Workflow** — If your organization has more than one workflow configured, choose the one you want to report on from the **Workflow** dropdown. If there is only one workflow, this dropdown will not appear.

2. **Choose a Date Range** — Pick a preset from the **Preset** dropdown or enter a custom range:

   | Preset | What It Covers |
   |--------|----------------|
   | Month to Date | From the 1st of the current month through today |
   | Quarter to Date | From the 1st of the current quarter through today |
   | Year to Date | From January 1st of the current year through today |
   | Last Month | The entire previous calendar month |
   | Last Quarter | The entire previous calendar quarter |
   | Custom | You set the **Start Date** and **End Date** yourself |

3. **Select a Date Basis** — This tells the report which date to use when filtering invoices into your chosen range:

   | Date Basis | What It Filters By |
   |------------|-------------------|
   | System Created Date | The date the invoice was entered into the system |
   | Invoice Date | The date printed on the invoice itself |
   | Site Mgr Approval Date | The date the site manager approved the invoice |
   | AP Approval Date | The date AP gave final approval |

   For example, choosing "Last Month" with "Invoice Date" shows all invoices dated within last month, regardless of when they entered the system.

4. **Click Run Report** — The report will load and display your results.

---

## Understanding the Results

### KPI Summary Strip

At the top of the results, five summary cards give you a quick snapshot of the data:

| Card | What It Tells You |
|------|-------------------|
| **Total Invoices** | The number of invoices that match your date range and filters |
| **Total Dollar Value** | The combined dollar amount of all those invoices |
| **Unique Processors** | How many different AP specialists handled invoices in this set |
| **Unique Vendors** | How many different vendors are represented |
| **Avg Processing Days** | The average number of days from system entry to final approval |

These numbers update automatically when you apply filters (see [Filtering Results](#filtering-results) below).

---

### Rollup Summary Tables

Below the KPI cards, three summary tables provide aggregated views of your data:

#### By AP Specialist
Shows each AP processor with their invoice count, average processing days (SLA color-coded), and number of open (in-progress) invoices. Click any row to filter the detail table to that specialist's invoices.

#### By Site Manager
Shows each site manager with their assigned count, average approval days, pending (awaiting approval) count, and SLA percentage (the percentage of approvals completed within 3 days). Click any row to filter to that manager's invoices.

#### Aging Summary
Shows only in-progress invoices grouped into age buckets (0–3, 4–7, 8–14, 15–30, and 31+ days). Each bucket shows the count, total dollar value, and how many are waiting at the site manager step. Click any bucket row to filter the detail table to those invoices.

**Click-to-filter:** When you click a row in any rollup table, a blue filter banner appears above the detail table showing what filter is active (e.g., "AP Specialist: Martinez, Ana"). Click the **Clear Filter** button on the banner to remove the filter. The rollup filter works together with the Processor and Status dropdown filters.

---

### Report Table

Each row in the table represents one invoice. Here is what each column means:

| Column | Description |
|--------|-------------|
| **AP Processor** | The AP specialist assigned to the invoice |
| **Vendor Name** | The vendor or supplier on the invoice |
| **Invoice Date** | The date printed on the invoice |
| **Invoice #** | The invoice number |
| **Invoice Amount** | The dollar amount of the invoice |
| **System Entry** | The date the invoice was entered into the workflow system |
| **Site Mgr Approver** | The name of the site manager who approved (or is assigned to) the invoice |
| **Site Mgr Date** | The date the site manager completed their approval |
| **Mgr Days** | The number of business days the site manager approval step took |
| **AP Approver** | The name of the AP approver |
| **AP Approval Date** | The date AP approval was completed |
| **AP Days** | The number of business days the AP approval step took |
| **Total Days** | Total days from system entry to final approval (or through today if still in progress) |
| **Comments** | Any notes from the approval steps |
| **Status** | Whether the invoice is **Completed** or **In Progress** |

---

### SLA Color Coding

The timing columns (**Mgr Days**, **AP Days**, and **Total Days**) are color-coded to help you spot invoices that need attention at a glance:

| Color | Days | What It Means |
|-------|------|---------------|
| **Green** | 0 to 3 days | On track — processing is within the expected timeframe |
| **Yellow** | 4 to 30 days | Attention needed — processing is taking longer than usual |
| **Red** | 31 or more days | Overdue — this invoice has been waiting significantly longer than expected |

---

### Status

The **Status** column shows one of two values:

- **Completed** (green) — The invoice has been fully approved through all workflow steps
- **In Progress** (yellow) — The invoice is still moving through the approval process

---

## Filtering Results

After running a report, you can narrow down the results using two additional filters in the filter bar. These filters work instantly — they do not reload data from the database.

- **Processor** — Select a specific AP specialist from the dropdown to see only their invoices. Choose "All" to see everyone.
- **Status** — Choose **All**, **Completed**, or **In Progress** to show only invoices with that status.

The KPI summary cards at the top will update to reflect your filtered results.

---

## Sorting

You can sort the report by any column:

- **Click a column header** to sort by that column in ascending order (A to Z, smallest to largest, oldest to newest).
- **Click the same column header again** to reverse the sort to descending order.

The currently sorted column is highlighted with a small arrow indicator showing the sort direction.

---

## Pagination

Results are displayed **25 invoices per page**. If your report returns more than 25 invoices, use the page navigation controls at the bottom of the table to move between pages.

---

## Exporting Data

To export your report to a file you can open in Excel:

1. Run your report and apply any filters or sorting you want.
2. Click the **Export CSV** button.
3. A CSV file will download to your computer. The file is named with your date range, for example: `AP_Productivity_Report_2026-01-01_to_2026-03-12.csv`.
4. Double-click the downloaded file to open it in Excel.

The export includes all data matching your current filters and sort order — not just the page you are viewing.

**Note:** If the file opens with strange characters in Excel, use **Data > From Text/CSV** in Excel instead of double-clicking the file.

---

## Tips and Shortcuts

- **Your selections are saved automatically.** The workflow, date preset, date basis, and filter choices you make are remembered between sessions. When you come back later, the report will have your last-used settings ready to go.

- **Use presets for quick date ranges.** Instead of typing dates manually, pick Month to Date, Last Month, or another preset to set both the start and end dates in one click.

- **Filter before exporting.** Apply your Processor and Status filters first, then export. The CSV will contain only the filtered data.

- **Watch the colors.** A quick scan of the color coding in the Mgr Days, AP Days, and Total Days columns will immediately highlight invoices that may need follow-up.

- **Sort to find outliers.** Click the Total Days column header to sort by processing time and quickly find the longest-running invoices.

---

*If you have questions or run into issues, contact your system administrator.*
