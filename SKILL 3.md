---
name: easyfile-design-system
description: >
  EasyFile UI Design System. Use this skill whenever building, modifying, or reviewing
  any EasyFile front-end code — HTML pages, Razor views, Blazor components, React components,
  or CSS. Ensures all UI output follows CASO's design tokens, component library, and layout
  patterns. Covers .NET 8, Blazor, HTML/CSS, and JavaScript implementations.
---

# EasyFile Design System

## When to Use This Skill

Use this skill for ANY task involving EasyFile UI:
- Creating new pages or views
- Building or modifying components
- Writing CSS or styling code
- Reviewing UI pull requests
- Creating mockups or prototypes
- Answering questions about EasyFile UI patterns

## Quick Reference

**Font:** Inter (fallback: ui-sans-serif, system-ui, Segoe UI, Roboto, Arial)
**Primary Color:** #2563eb (Blue 600)
**Border Radius:** 4px (tight) → 6px (default) → 10px (card) → 14px (modal)
**Default Nav Theme:** Light (white header/sidebar — LOB style)
**CSS Prefix:** All custom properties use `--ef-` prefix

---

## Layer 1: Design Tokens

Every value in EasyFile is a CSS custom property. Never use hardcoded colors, sizes, or
spacing. Always reference tokens.

### Typography

```css
--ef-font: 'Inter', ui-sans-serif, system-ui, "Segoe UI", Roboto, Arial, sans-serif;

/* Scale — do NOT invent intermediate sizes */
--ef-text-24: 24px;   /* KPI values, page titles — font-weight: 700, letter-spacing: -0.5px */
--ef-text-18: 18px;   /* Section headers — font-weight: 700 */
--ef-text-14: 14px;   /* Default body text, card titles (bold) */
--ef-text-13: 13px;   /* Table cells, input text, button labels */
--ef-text-12: 12px;   /* Labels, metadata keys, tab labels — font-weight: 600 */
--ef-text-11: 11px;   /* Timestamps, helper text, captions */
```

**Rules:**
- Body copy: 14px/400 weight
- Labels and metadata: 12px/600 weight, uppercase + letter-spacing: 0.3px for section headers
- Never use font sizes outside this scale
- Line height: 1.5 globally

### Spacing

```css
--ef-space-4: 4px;    /* Micro gaps: between icon and label in a badge */
--ef-space-8: 8px;    /* Intra-component: gaps between badges, small padding */
--ef-space-12: 12px;  /* Small card padding, toast padding */
--ef-space-16: 16px;  /* Grid gaps, KPI card padding, medium spacing */
--ef-space-24: 24px;  /* Card body padding, page content margin, tab gaps */
--ef-space-32: 32px;  /* Section spacing, large separation */
```

**Rules:**
- Only use these 6 values. No `10px`, no `20px`, no `15px`.
- Grid gaps: 16px between cards in a grid
- Page content padding: 24px on all sides
- Card internal padding: 24px (standard), 16px (compact)

### Colors

```css
/* Primary */
--ef-color-primary: #2563eb;         /* Buttons, links, active states */
--ef-color-primary-hover: #1d4ed8;   /* Button hover, link hover */
--ef-color-primary-bg: rgba(37,99,235,0.08);    /* Light tint backgrounds */
--ef-color-primary-light: rgba(37,99,235,0.25);  /* Chart secondary, borders */
--ef-color-focus: rgba(37,99,235,0.4);           /* Focus ring: box-shadow: 0 0 0 2px */

/* Danger */
--ef-color-danger: #dc2626;          /* Delete buttons */
--ef-color-danger-hover: #b91c1c;    /* Delete button hover */

/* Semantic — each has 3 variants: solid, background tint, text */
--ef-green: #16a34a;     --ef-green-bg: rgba(22,163,74,0.08);    --ef-green-text: #15803d;
--ef-yellow: #d97706;    --ef-yellow-bg: rgba(217,119,6,0.08);   --ef-yellow-text: #b45309;
--ef-red: #dc2626;       --ef-red-bg: rgba(220,38,38,0.08);      --ef-red-text: #b91c1c;
--ef-blue: #2563eb;      --ef-blue-bg: rgba(37,99,235,0.08);     --ef-blue-text: #1d4ed8;
```

**Rules:**
- Success/Active → green. Warning/Pending → yellow. Error/Overdue → red. Info/Progress → blue.
- Background tints are 8% opacity of the solid color
- Text colors are one shade darker than the solid for contrast
- Never use raw hex colors — always reference the token

### Surfaces (Theme-Dependent)

```css
/* Light theme (default) */
[data-theme="light"] {
  --ef-bg: #f8f9fb;           /* Page background */
  --ef-surface: #ffffff;       /* Cards, panels, modals */
  --ef-surface-2: #f1f3f5;    /* Table headers, hover states, secondary surfaces */
  --ef-text: #1e293b;         /* Primary text */
  --ef-text-muted: #64748b;   /* Secondary text, labels, placeholders */
  --ef-border: #e2e8f0;       /* Borders, dividers */
}

/* Dark theme */
[data-theme="dark"] {
  --ef-bg: #0f172a;
  --ef-surface: #1e293b;
  --ef-surface-2: #253249;
  --ef-text: #e2e8f0;
  --ef-text-muted: #94a3b8;
  --ef-border: #334155;
}
```

### Navigation Themes

EasyFile supports 4 navigation themes (sidebar + header). The default is **light**.

```css
/* Light — white/light gray. Default. Used for LOB apps like Salesforce, Office 365 */
[data-nav="light"] {
  --nav-bg: #ffffff;
  --nav-bg-2: #f8f9fb;         /* Sidebar background */
  --nav-text: #64748b;
  --nav-text-active: #1e293b;
  --nav-hover: rgba(0,0,0,0.04);
  --nav-active-bg: var(--ef-color-primary-bg);
  --nav-active-text: var(--ef-color-primary);
  --nav-accent: var(--ef-color-primary);
  --nav-divider: #e2e8f0;
  --nav-border: #e2e8f0;
  --nav-brand: #1e293b;
  --header-shadow: 0 1px 3px rgba(0,0,0,0.06);
}

/* Soft — muted blue-gray. ServiceNow / Dynamics style */
[data-nav="soft"] { --nav-bg: #f0f4f8; --nav-bg-2: #e8edf2; ... }

/* Slate — medium dark. Azure Portal / GitHub style */
[data-nav="slate"] { --nav-bg: #1e293b; --nav-bg-2: #263448; ... }

/* Dark — deep navy. Original EasyFile dark theme */
[data-nav="dark"] { --nav-bg: #0f172a; --nav-bg-2: #162033; ... }
```

### Elevation

```css
--ef-shadow-1: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);  /* Cards */
--ef-shadow-2: 0 4px 12px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06);  /* Modals, dropdowns, toasts */
```

### Border Radius

```css
--ef-radius-4: 4px;    /* Checkboxes, tight elements, zoom-level controls */
--ef-radius-6: 6px;    /* Buttons, inputs, sidebar items, small cards */
--ef-radius-10: 10px;  /* Cards, toasts, modals inner content */
--ef-radius-14: 14px;  /* Badges (pill), modal outer, large containers */
```

---

## Layer 2: Component Library

### Buttons

5 variants, 3 sizes. All use `border-radius: var(--ef-radius-6)`.

| Variant | Class | Background | Text | Border |
|---------|-------|-----------|------|--------|
| Primary | `.btn-primary` | `--ef-color-primary` | `#fff` | none |
| Secondary | `.btn-secondary` | `--ef-surface` | `--ef-text` | `1px solid --ef-border` |
| Ghost | `.btn-ghost` | transparent | `--ef-text-muted` | none |
| Danger | `.btn-danger` | `--ef-color-danger` | `#fff` | none |
| Icon-only | `.btn-icon` | transparent | `--ef-text-muted` | none, 34×34px square |

| Size | Class | Height | Padding | Font |
|------|-------|--------|---------|------|
| Small | `.btn-sm` | 28px | 0 8px | 12px |
| Default | `.btn-md` | 34px | 0 12px | 13px |
| Large | `.btn-lg` | 42px | 0 16px | 14px |

**Rules:**
- Always include both variant + size class: `btn btn-primary btn-md`
- Icons inside buttons: 15×15px SVG, `stroke-width="2"`, before the label
- Focus: `outline: 2px solid var(--ef-color-focus); outline-offset: 2px`
- Disabled: `opacity: 0.4; cursor: not-allowed`
- All transitions: `150ms`

### Badges & Status Indicators

```html
<!-- Status badge with dot -->
<span class="badge b-grn"><span class="bdot"></span>Active</span>
<span class="badge b-ylw"><span class="bdot"></span>Pending</span>
<span class="badge b-red"><span class="bdot"></span>Overdue</span>
<span class="badge b-blu"><span class="bdot"></span>In Progress</span>
<span class="badge b-gry"><span class="bdot"></span>On Hold</span>

<!-- Environment badge -->
<span class="env-badge">PROD</span>

<!-- Tag -->
<span class="tag"># Financial</span>

<!-- Delta indicator -->
<span class="delta delta-pos">↑ +8.2%</span>
```

Badge: height 22px, pill shape (`--ef-radius-14`), 11px font, 600 weight.
Dot: 6px circle, matching semantic color.

### Data Tables

```html
<table class="dt">
  <thead><tr>
    <th style="width:32px"><input type="checkbox" class="cb"></th>
    <th>Name</th>
    <th>Status</th>
    <th class="cr">Value</th>  <!-- .cr = text-align: right -->
  </tr></thead>
  <tbody><tr>
    <td><input type="checkbox" class="cb"></td>
    <td><a href="#" class="cl">Invoice #ABC-1040</a></td>  <!-- .cl = primary link -->
    <td><span class="badge b-blu"><span class="bdot"></span>Awaiting</span></td>
    <td class="cr">$10,476.00</td>
  </tr></tbody>
</table>
```

**Rules:**
- Header: `--ef-surface-2` background, 12px uppercase labels, `letter-spacing: 0.3px`
- Rows: 40px height, bottom border `--ef-border`, hover → `--ef-surface-2`
- Checkbox column: 32px wide
- Custom checkbox `.cb`: 15×15px, `--ef-border` stroke, checked → `--ef-color-primary` fill
- Clickable names: `.cl` class → primary color, underline on hover
- Right-aligned numbers: `.cr` class
- Muted text: `.cm` class

### Form Controls

```html
<!-- Text input -->
<div class="fg">
  <label class="fl">Label</label>
  <input type="text" class="fi" placeholder="Placeholder...">
</div>

<!-- Error state -->
<input class="fi fi-err" value="">
<div class="f-err">This field is required</div>

<!-- Select -->
<select class="form-select">...</select>

<!-- Toggle switch -->
<div class="toggle on" onclick="this.classList.toggle('on')"></div>
```

**Rules:**
- Input height: 34px
- Border: `1px solid --ef-border`, radius 6px
- Focus: border → primary, `box-shadow: 0 0 0 2px var(--ef-color-focus)`
- Error: border → danger, error text below in 11px red
- Labels `.fl`: 12px, 600 weight
- Toggle: 34×18px, 9px radius. Off → `--ef-border`. On → `--ef-color-primary`

### Tabs

```html
<div class="tabs">
  <div class="tab active">Metadata</div>
  <div class="tab">Versions</div>
  <div class="tab">Workflow</div>
</div>
```

**Rules:**
- 38px height, 12px font, 600 weight, uppercase, `letter-spacing: 0.3px`
- Active: primary color text + 2px bottom border in primary
- Default: muted text, transparent bottom border
- Gap between tabs: 24px

### Cards

```html
<div class="card pad">
  <!-- Content -->
</div>
```

**Rules:**
- Background: `--ef-surface`
- Border: `1px solid --ef-border`
- Radius: `--ef-radius-10`
- Shadow: `--ef-shadow-1`
- Padding: use `.pad` for standard 24px, or custom padding

### Toasts / Notifications

4 types: `success`, `info`, `warn`, `error`.

**Rules:**
- Width: 340px, fixed top-right (60px from top, 16px from right)
- 4px left border in semantic color
- Auto-dismiss: success 5s, warn 8s, error persists until closed
- Max 3 visible, oldest removed first
- Animate in: slide from right (300ms), out: fade right (150ms)

### Modals

```html
<div class="modal-ov">
  <div class="modal-box">
    <div class="modal-hdr"><h2>Title</h2><button>×</button></div>
    <div class="modal-bd"><!-- Body --></div>
    <div class="modal-ft"><!-- Footer buttons --></div>
  </div>
</div>
```

**Rules:**
- Overlay: `rgba(0,0,0,0.4)`, click to dismiss
- Box: 440px wide, `--ef-radius-14`, `--ef-shadow-2`
- Header/footer separated by `--ef-border`
- Footer buttons right-aligned, cancel (secondary) then action (primary/danger)

### Alerts / Banners

Inline alert boxes for page-level or section-level messages.

```html
<div style="display:flex;align-items:flex-start;gap:8px;padding:12px;
  background:var(--ef-green-bg);border:1px solid rgba(22,163,74,0.15);
  border-radius:var(--ef-radius-6)">
  <svg><!-- check icon --></svg>
  <div>
    <div style="font-size:12px;font-weight:600;color:var(--ef-green-text)">Title</div>
    <div style="font-size:11px;color:var(--ef-green-text);opacity:0.8">Message</div>
  </div>
</div>
```

4 variants: success (green), warning (yellow), error (red), info (blue).

### Avatars

```html
<!-- Single -->
<div class="avatar">JH</div>

<!-- Stacked group -->
<div style="display:flex">
  <div class="avatar" style="margin-right:-8px;z-index:3;border:2px solid var(--ef-surface)">ST</div>
  <div class="avatar" style="margin-right:-8px;z-index:2;border:2px solid var(--ef-surface)">DK</div>
  <div class="avatar" style="...;background:var(--ef-surface-2);color:var(--ef-text-muted)">+3</div>
</div>
```

Default: 30px circle, primary background, white text, 11px/600.

---

## Layer 3: Pattern Catalog

### KPI Summary Card

4-column grid at top of dashboard. Each card contains: color dot + label, large value (24px/700), optional subtitle, sparkline SVG, footer text.

```
┌─ dot ─ label ──────┐
│ $186,250            │  24px bold
│ ($92,500 overdue)   │  12px warning-text
│ ▁▂▃▄▅▆▇ sparkline  │  36px tall SVG
│ Last 30 Days        │  11px muted
└─────────────────────┘
```

Grid: `grid-template-columns: repeat(4, 1fr); gap: 16px`
Responsive: 2-col at 1200px, 1-col at 768px.

### Workflow Summary Panel

3-column metrics row → segmented status bar → legend → "View All" link.

### Automation Metrics Panel

Large value + delta badge → progress bar → paired bar chart (current vs previous) → month labels.

### Inspector Panel (Right Side)

300px wide, pinned right on document viewer. Contains: tabs (Metadata/Versions/Workflow/AI), key-value metadata rows, action buttons (full-width, 36px height, bordered), activity timeline.

### Document Viewer

Toolbar (38px) → viewport (centered document with shadow) → bottom bar (32px).
Document renders as white card (580px wide) with invoice/document content.
Toolbar contains: grid toggle, page counter, zoom controls.

### Activity Feed

List of entries: avatar circle (28px) → name + reference + timestamp → description.
Separated by `--ef-border` bottom border. Last item: no border.

### Empty State

Centered: large muted icon (48px) → title (14px/600) → description (12px/muted) → CTA button.

### Loading States

Spinner: `border: 2px`, primary color top, 0.8s spin.
Skeleton: pulsing rectangles (1.5s ease-in-out infinite).
Progress bar: 6px tall, rounded, primary fill with width transition.

---

## Layer 4: Page Templates

### Dashboard

```
┌─────── Header (48px) ────────────────────────────────┐
├──────┬───────────────────────────────────────────────┤
│      │  Page Header: "Dashboard" + actions            │
│ Side │  ┌──────┬──────┬──────┬──────┐                │
│ bar  │  │ KPI  │ KPI  │ KPI  │ KPI  │  4-col grid   │
│      │  └──────┴──────┴──────┴──────┘                │
│ 220  │  ┌──── 6fr ────┬──── 4fr ────┐                │
│  px  │  │ Workflow     │ Automation  │  2-col split   │
│      │  └─────────────┴─────────────┘                │
│      │  ┌──── 6fr ────┬──── 4fr ────┐                │
│      │  │ Tasks Table  │ Activity    │  2-col split   │
│      │  └─────────────┴─────────────┘                │
└──────┴───────────────────────────────────────────────┘
```

### Document List

Header → filter bar (search + dropdowns + clear) → data table → pagination.

### Document Viewer

Header with breadcrumb → horizontal split: [viewer main (toolbar + viewport + bottom bar)] + [inspector panel 300px].

### Admin / Settings

Header → content area with 2-column grid of showcase sections.

---

## Layer 5: Global Standards

### App Shell

- Header: 48px tall, nav background, border-bottom
- Sidebar: 220px expanded / 56px collapsed, nav-bg-2 background
- Active nav item: left accent bar (3px), primary highlight bg
- Sidebar collapse: chevron button at bottom

### Date/Time Formats

- Display: `MMM DD, YYYY` → "Apr 2, 2024"
- Timestamps: `h:mm AM/PM` → "2:47 PM"
- Metadata: `MMMM DD, YYYY` → "APRIL 02, 2024" (uppercase)

### Number Formats

- Currency: `$XX,XXX.XX` with comma separators
- Percentages: one decimal max → "78%", "98.4%"
- Counts: comma separators → "1,240"

### Responsive Breakpoints

```css
@media (max-width: 1200px) { /* 2-col KPIs, stack 2-col layouts */ }
@media (max-width: 1024px)  { /* Collapse sidebar, hide inspector */ }
@media (max-width: 768px)   { /* 1-col KPIs, hide sidebar entirely */ }
```

### Animations

- Transitions: 150ms for hover/focus. 200ms for sidebar collapse, modal enter.
- Toasts: 300ms slide-in, 150ms fade-out.
- Progress bars: 600ms width transition.
- Respect `prefers-reduced-motion: reduce` — set all durations to 0.01ms.

### Accessibility

- All interactive elements: visible focus ring (`2px solid var(--ef-color-focus)`)
- Skip link: first focusable element
- Color contrast: minimum 4.5:1 for text
- Checkboxes and toggles: keyboard operable
- Toasts: `role="alert"` for screen reader announcement
- Icons: `aria-label` on icon-only buttons

### Dark Mode

- Controlled via `data-theme="light|dark"` on `<html>`
- Nav theme independent: `data-nav="light|soft|slate|dark"` on `<html>`
- All component colors automatically adapt through CSS custom properties
- Document viewer page always renders white (it's a document preview)

### Theme Switcher (REQUIRED on every page)

**Always include** the theme switcher widget in the **lower-right corner** of every page or prototype you build. It lets users toggle navigation and content themes at runtime.

**Structure:** A floating toggle button (sun icon) that opens a panel with two groups:

| Group | Options | `data-*` attribute | Values |
|-------|---------|--------------------|--------|
| **Navigation** | Light, Soft Gray, Slate, Dark | `data-nav` on `<html>` | `light`, `soft`, `slate`, `dark` |
| **Content** | Light, Dark | `data-theme` on `<html>` | `light`, `dark` |

**HTML (paste before `</body>`):**

```html
<button class="theme-toggle-btn" id="theme-toggle" onclick="togglePanel()" aria-label="Theme settings">
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
    <circle cx="12" cy="12" r="4"/>
    <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"/>
  </svg>
</button>
<div class="theme-panel" id="theme-panel" style="display:none">
  <div class="theme-group">
    <h4>Navigation</h4>
    <div class="theme-opt active" data-nav="light" onclick="setNav('light')"><div class="theme-swatch" style="background:#fff"></div>Light</div>
    <div class="theme-opt" data-nav="soft" onclick="setNav('soft')"><div class="theme-swatch" style="background:#e8edf2"></div>Soft Gray</div>
    <div class="theme-opt" data-nav="slate" onclick="setNav('slate')"><div class="theme-swatch" style="background:#1e293b"></div>Slate</div>
    <div class="theme-opt" data-nav="dark" onclick="setNav('dark')"><div class="theme-swatch" style="background:#0f172a"></div>Dark</div>
  </div>
  <div class="theme-group">
    <h4>Content</h4>
    <div class="theme-opt active" data-content="light" onclick="setTheme('light')"><div class="theme-swatch" style="background:#f8f9fb"></div>Light</div>
    <div class="theme-opt" data-content="dark" onclick="setTheme('dark')"><div class="theme-swatch" style="background:#0f172a"></div>Dark</div>
  </div>
</div>
```

**CSS (include in stylesheet):**

```css
.theme-panel { position:fixed; bottom:16px; right:16px; z-index:9998; background:var(--ef-surface); border:1px solid var(--ef-border); border-radius:var(--ef-radius-10); box-shadow:var(--ef-shadow-2); padding:var(--ef-space-12); width:220px }
.theme-panel h4 { font-size:var(--ef-text-11); font-weight:700; text-transform:uppercase; letter-spacing:.5px; color:var(--ef-text-muted); margin-bottom:var(--ef-space-8) }
.theme-group { margin-bottom:var(--ef-space-12) }
.theme-group:last-child { margin-bottom:0 }
.theme-opt { display:flex; align-items:center; gap:var(--ef-space-8); padding:5px 8px; border-radius:var(--ef-radius-6); cursor:pointer; font-size:var(--ef-text-12); color:var(--ef-text); transition:all 100ms; margin-bottom:2px }
.theme-opt:hover { background:var(--ef-surface-2) }
.theme-opt.active { background:var(--ef-color-primary-bg); color:var(--ef-color-primary); font-weight:600 }
.theme-swatch { width:18px; height:18px; border-radius:4px; border:1px solid var(--ef-border); flex-shrink:0 }
.theme-toggle-btn { position:fixed; bottom:16px; right:16px; z-index:9997; width:38px; height:38px; border-radius:50%; background:var(--ef-surface); border:1px solid var(--ef-border); box-shadow:var(--ef-shadow-2); display:flex; align-items:center; justify-content:center; color:var(--ef-text-muted); cursor:pointer }
.theme-toggle-btn:hover { background:var(--ef-surface-2); color:var(--ef-text) }
```

**JavaScript (include in page script):**

```js
function togglePanel() {
  var p = document.getElementById('theme-panel'), b = document.getElementById('theme-toggle');
  var s = p.style.display === 'none';
  p.style.display = s ? 'block' : 'none';
  b.style.display = s ? 'none' : 'flex';
}
function setNav(t) {
  document.documentElement.setAttribute('data-nav', t);
  localStorage.setItem('ef-nav-theme', t);
  document.querySelectorAll('.theme-opt[data-nav]').forEach(function(o) {
    o.classList.toggle('active', o.getAttribute('data-nav') === t);
  });
}
function setTheme(t) {
  document.documentElement.setAttribute('data-theme', t);
  localStorage.setItem('ef-content-theme', t);
  document.querySelectorAll('.theme-opt[data-content]').forEach(function(o) {
    o.classList.toggle('active', o.getAttribute('data-content') === t);
  });
}
// Restore saved theme on load
(function() {
  var savedNav = localStorage.getItem('ef-nav-theme');
  var savedContent = localStorage.getItem('ef-content-theme');
  if (savedNav) setNav(savedNav);
  if (savedContent) setTheme(savedContent);
})();
document.addEventListener('click', function(e) {
  var p = document.getElementById('theme-panel'), b = document.getElementById('theme-toggle');
  if (p.style.display === 'block' && !p.contains(e.target) && e.target !== b && !b.contains(e.target)) {
    p.style.display = 'none'; b.style.display = 'flex';
  }
});
```

**Rules:**
- The toggle button and panel are **fixed** to `bottom: 16px; right: 16px`
- Default state: Navigation = Light, Content = Light (unless overridden by saved preference)
- Choices persist via `localStorage` keys `ef-nav-theme` and `ef-content-theme`, restored on page load
- Panel dismisses on outside click
- Active option is highlighted with the primary-bg tint
- This widget must be present on **every** HTML page, prototype, or preview you generate

---

## Implementation Notes for .NET 8 / Blazor

When building Blazor components for EasyFile:

1. **CSS Isolation**: Use component-scoped CSS files (`.razor.css`) that import design tokens
2. **Theme Switching**: Bind `data-theme` and `data-nav` attributes via Blazor JS interop or cascading parameters
3. **Component Naming**: Match the class names in this system — `.btn-primary`, `.badge`, `.card`, etc.
4. **Token File**: Import `design-tokens.css` in your `_Host.cshtml` or `App.razor` as the first stylesheet
5. **No Inline Colors**: Never use hardcoded colors in component CSS — always reference `var(--ef-*)` tokens

---

## Files in This Package

| File | Purpose |
|------|---------|
| `SKILL.md` | This file — Claude AI instruction set |
| `css/design-tokens.css` | Extracted CSS custom properties (import first) |
| `css/components.css` | All component classes |
| `reference/easyfile-themed.html` | Interactive showcase with theme switcher (open in browser) |
| `reference/EASYFILE_DESIGN_STANDARD.md` | Full written specification |
| `examples/blazor-usage.md` | Blazor/Razor component examples |
| `examples/developer-guide.md` | Hands-on developer onboarding guide |
| `README.md` | Setup instructions for GitHub + Claude |
