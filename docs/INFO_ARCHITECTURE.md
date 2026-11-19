# RemitMon (RM) — Information Architecture

> Companion to `RemitMon-Copilot-Spec.md`. Copilot: use this file to build the React navigation shell, route table, page layout, shared components, and quick-access features. Produce complete files. Where this document and the spec disagree on data or API behavior, the spec wins; where they disagree on navigation or layout, this document wins.

---

## 1. Design principles

1. **Three-click rule.** Any decision, policy, counterparty, obligation, or approval is reachable in three clicks or fewer from any page, and in one step from the command palette.
2. **Queue first.** The most time-sensitive object is an open approval with a deadline (RfP expiry, ACH pre-posting cutoff, return window). Its count and nearest deadline are visible on every page.
3. **Explain every outcome.** Any outcome badge (Allow / Reject / Escalate) is clickable and opens the same Decision Drawer showing rule hits, matched obligation, and actions.
4. **One object, one URL.** Every entity has a stable, shareable deep link.
5. **Role-shaped navigation.** The same shell shows different nav items by role. No dead links, no disabled-looking items for unreachable areas; hide them.

---

## 2. Top-level structure

```
RemitMon
├── Global chrome (every page)
│   ├── Top bar: Account switcher · Mode badge (Monitor/Enforce) · Global search / Command palette · Approvals pill · Notifications · User menu
│   ├── Left nav (role-based, collapsible)
│   └── Simulator banner (conditional)
│
├── Business area            (BusinessAdmin, BusinessApprover)
│   ├── Dashboard
│   ├── Activity
│   ├── Approvals
│   ├── Policies
│   ├── Counterparties
│   ├── Obligations
│   ├── Integrations
│   └── Audit
│
├── Bank area                (BankAdmin)
│   ├── Bank Overview
│   ├── Customers
│   ├── Baseline Policies
│   └── Audit
│
└── Admin area               (PlatformAdmin)
    ├── Simulator
    ├── Tenants
    └── System Health
```

### Role to navigation map

| Nav item | BusinessApprover | BusinessAdmin | BankAdmin | PlatformAdmin |
|---|---|---|---|---|
| Dashboard | ✔ | ✔ | | ✔ |
| Activity | ✔ | ✔ | read-only per customer | ✔ |
| Approvals | ✔ (approve/reject) | ✔ | read-only | ✔ |
| Policies | view | ✔ (edit, submit) | baseline only | ✔ |
| Counterparties | view | ✔ | read-only | ✔ |
| Obligations | ✔ | ✔ | read-only | ✔ |
| Integrations | | ✔ | | ✔ |
| Audit | | ✔ | ✔ | ✔ |
| Bank Overview / Customers / Baseline | | | ✔ | ✔ |
| Simulator / Tenants / System Health | | | | ✔ |

Policy activation requires a second approver (author cannot self-approve), enforced by the API and reflected in the UI (the Approve button is hidden for the author).

---

## 3. Route table

All routes are under the authenticated shell. Entity routes accept an optional `?drawer=` parameter so a deep link opens the page with a drawer already open.

| Route | Page | Primary API |
|---|---|---|
| `/` | Redirect to role home (`/dashboard` or `/bank`) | |
| `/dashboard` | Dashboard | `GET /bank/overview` (bank) · aggregated `GET /events`, `/approvals`, `/obligations` |
| `/activity` | Activity table | `GET /events` |
| `/activity/:eventId` | Event detail (same content as Decision Drawer, full page) | `GET /events/{id}` |
| `/approvals` | Approvals queue | `GET /approvals` |
| `/approvals/:id` | Approval detail with approve/reject | `GET /approvals`, `POST /approvals/{id}/approve\|reject` |
| `/policies` | Policy list per account | `GET /accounts/{id}/policies` |
| `/policies/:policyId` | Policy editor (rule tree, test panel, backtest, versions) | `GET/PUT`, `POST /policies/{id}/test\|backtest\|submit\|approve\|activate` |
| `/policies/:policyId/diff/:otherId` | Version diff | `GET /policies/{id}/diff/{otherId}` |
| `/counterparties` | Counterparty list | `GET /counterparties` |
| `/counterparties/:id` | Counterparty detail + change history | `GET /counterparties/{id}`, `/changes` |
| `/obligations` | Expected obligations | `GET /obligations` |
| `/obligations/:id` | Obligation detail + matched payment | `GET /obligations/{id}` |
| `/integrations` | Adapter status, webhook log | `GET /integrations/status` (add), webhook log endpoint (add) |
| `/audit` | Audit log + verify chain | `GET /audit`, `GET /audit/verify` |
| `/bank` | Bank Overview | `GET /bank/overview` |
| `/bank/customers/:id` | Customer drill-in (read-only) | `GET /bank/customers/{id}` |
| `/bank/baseline` | Locked baseline rules, fail-safe defaults | policy endpoints, bank scope |
| `/admin/simulator` | Scenario runner + live event stream | `GET /sim/scenarios`, `POST /sim/scenarios/{name}/run`, `POST /sim/reset` |
| `/admin/tenants` | Tenant list | tenant endpoints (add) |
| `/admin/health` | Health, queue depth, dead-letter | `/health/ready` + metrics endpoint (add) |

---

## 4. Quick-access system

These features are required, not optional. Build them in the shell before the individual pages.

### 4.1 Command palette (`Cmd/Ctrl + K`)
- Single input searching: pages, counterparties, obligations (by vendor, invoice ref), events (by amount, reference, company ID), policies, approvals.
- Result groups with icons; Enter navigates; `Shift+Enter` opens in a drawer.
- Verb commands: "Approve next request", "New policy draft", "Switch account", "Run scenario: …" (Admin), "Verify audit chain".
- Backed by `GET /search?q=` (add endpoint; returns typed, role-filtered results; max 20).

### 4.2 Approvals pill (top bar, always visible)
- Shows open count and a live countdown to the nearest deadline. Color escalates as the deadline approaches (thresholds in config).
- Click opens a slide-over queue with Approve / Reject inline; "Open full queue" goes to `/approvals`.

### 4.3 Account switcher
- Lists accounts the user can access with mode badge (Monitor / Enforce) and a status dot. Selection persists per user and is reflected in the URL query `?account=`.

### 4.4 Decision Drawer (shared, opens from anywhere)
- Triggered by any outcome badge, event row, or `?drawer=event:{id}`.
- Sections: Summary (rail, initiation, amount, counterparty), Outcome and reasons (ordered rule hits with plain-language text), Matched obligation (with score breakdown), Counterparty signals (new payee, bank detail change), Actions taken (reject/return/reverse/accept/decline with status and window deadline), Raw payload (collapsed), Audit trail link.

### 4.5 Keyboard shortcuts
`g d` Dashboard · `g a` Activity · `g q` Approvals · `g p` Policies · `g c` Counterparties · `g o` Obligations · `j/k` move in lists · `a` approve · `r` reject (opens note field) · `?` shortcut help.

### 4.6 Saved views
- Activity and Approvals support saved filters (rail, initiation, outcome, counterparty, date range). Three defaults ship: "Pulls needing attention", "Rejected today", "Unmatched debits".

### 4.7 Breadcrumbs and entity chips
- Every entity reference (counterparty, obligation, policy, event) renders as a chip linking to its route; hovering shows a mini-card (name, status, key figure).

---

## 5. Page layouts

### 5.1 Dashboard
Layout: top KPI strip, then two-column grid.
- KPI strip: Allowed / Rejected / Escalated (count and dollars, today) · Blocked exposure · Open approvals · Obligations due in 7 days.
- Left column: Approvals needing action (top 5 with countdowns) · Recent webhook-driven alerts ("Bill created: Vendor X, $4,200", "Vendor bank details changed").
- Right column: Blocked-exposure chart (Recharts, 30 days) · Rail mix · Policy status card (active version, last change, pending approvals).

### 5.2 Activity
- Filter bar (saved views dropdown) → table (time, rail, initiation badge, counterparty, amount, outcome badge, matched obligation chip, mode applied) → row click opens Decision Drawer.
- Initiation badge values: `Push`, `Pull`, `RfP`. Pulls get emphasis styling.

### 5.3 Approvals
- Two-pane: list left (sorted by deadline), detail right. Detail shows counterparty, amount, obligation match status, risk signals, deadline countdown, recourse window, and Approve / Reject with required note. Next item auto-selects after action.

### 5.4 Policies
- Left: scope tree (Account → Counterparty → Payment type → Transaction overrides). Center: rule forms for the selected node. Right: "Test a payment" panel and backtest results. Header: version, status chip (Draft / Active / Archived), actions (Submit, Approve, Activate), "Compare versions".
- Locked rules (set by bank) show a lock icon and are read-only.

### 5.5 Counterparties
- Table with status chips (Pending / Approved / Blocked), source (Manual / Accounting / Observed), ACH company IDs. Detail page: identity, bank-detail change timeline, linked obligations, linked events, approve/block actions.

### 5.6 Obligations
- Table: counterparty, amount, due date, status (Open / Matched / Paid / Cancelled), source. Detail: source record reference, matched payment event, match score breakdown.

### 5.7 Bank Overview
- Customer portfolio table (protected accounts, mode adoption, blocked dollars, open approvals, SLA status), rail mix chart, approval SLA chart. Click-through to read-only customer drill-in.

### 5.8 Simulator
- Scenario list with descriptions and "Run" buttons; live event stream (decisions as they appear); reset button; scenario outcome checklist showing expected versus actual decision.

---

## 6. Frontend code structure (`/web/src`)

```
/app
  AppShell.tsx            top bar, left nav, banner, outlet
  routes.tsx              route table from Section 3, role guards
  providers.tsx           QueryClient, auth, account context, command palette
/features
  dashboard/  activity/  approvals/  policies/  counterparties/
  obligations/  integrations/  audit/  bank/  admin/
    (each: pages/, components/, hooks/, api.ts)
/shared
  /components             OutcomeBadge, InitiationBadge, RailIcon, AmountText,
                          DeadlineCountdown, EntityChip, DecisionDrawer,
                          DataTable, FilterBar, SavedViews, ConfirmNoteDialog,
                          EmptyState, SimulatorBanner, RoleGate
  /hooks                  useAccount, useRole, useHotkeys, useDeepLinkDrawer,
                          useApprovalsSummary, useCommandSearch
  /api                    generated client (openapi-typescript) + fetch wrapper
  /lib                    formatters, banking-day utils (display only), constants
/styles                   tailwind config, tokens (light/dark)
```

Rules:
- Feature folders own their pages; `/shared` holds only cross-feature components.
- `RoleGate` wraps nav items and action buttons; roles come from the JWT claims via `useRole`.
- `DecisionDrawer`, `CommandPalette`, and the approvals slide-over are mounted once in `AppShell` and controlled through URL state (`?drawer=`) so deep links work.
- No business or policy logic in the browser; display formatting only.

---

## 7. Object relationship map

Use this to build entity chips, drill-through links, and the command palette's result grouping.

```
Bank ─< BusinessCustomer ─< Account ─< Policy ─< PolicyRule
                       │          └─< PaymentEvent ─1 Decision ─< ActionRecord
                       │                    │              └─0..1 Approval
                       │                    └─0..1 ExpectedObligation (matched)
                       ├─< Counterparty ─< CounterpartyChangeLog
                       └─< ExpectedObligation
AuditEntry (append-only) references any entity above
```

Drill-through links to implement:
- Event → Decision → (Policy version, Rule hits) · Counterparty · Matched obligation · Actions · Approval
- Counterparty → Events · Obligations · Change history · Policies scoped to it
- Obligation → Counterparty · Matched event
- Policy → Rule list · Versions · Backtest events
- Approval → Event/Decision · Counterparty · Obligation

---

## 8. Cross-cutting UI states

| State | Behavior |
|---|---|
| Loading | Skeleton rows/cards, never a blank page |
| Empty | Explains what populates the view and links to the Simulator when in Simulator mode |
| Error | ProblemDetails message with correlation id and a copy button |
| Fail-safe event | Distinct badge ("Fail-safe applied") with reason in the Decision Drawer |
| Monitor mode | Outcome badges read "Would reject / Would escalate"; no action rows |
| Expired approval | Greyed with resolution "Auto-declined at expiry" or "Auto-rejected at cutoff" |
| Simulated action | Tag "Simulated" next to every action record |

Accessibility: WCAG AA contrast, full keyboard navigation, focus-visible styles, ARIA labels on badges and countdowns, no color-only status signals (pair with icon and text).

---

## 9. Build order for the shell and navigation

1. `AppShell`, role-aware left nav, route table with guards.
2. Auth context, account context, URL-state hooks (`useDeepLinkDrawer`).
3. Shared components: badges, `DataTable`, `FilterBar`, `EntityChip`, `DeadlineCountdown`.
4. `DecisionDrawer` (wired to `GET /events/{id}`).
5. Approvals pill and slide-over.
6. Command palette plus `GET /search` endpoint.
7. Dashboard, Activity, Approvals pages.
8. Policies, Counterparties, Obligations pages.
9. Bank area, Admin/Simulator pages.
10. Keyboard shortcuts, saved views, accessibility pass.

## 10. Acceptance criteria

- Every route in Section 3 renders, is guarded by role, and supports deep linking.
- The command palette finds a counterparty, an obligation by invoice ref, and an event by reference, and navigates in one step.
- The approvals pill shows the open count and nearest deadline on every page and updates without a manual refresh.
- Any outcome badge in the app opens the Decision Drawer with rule hits, match details, and actions.
- Hidden navigation items match the role map in Section 2; the policy author never sees the Approve button on their own version.
- A shared URL with `?drawer=event:{id}` opens the correct drawer for another authorized user.
