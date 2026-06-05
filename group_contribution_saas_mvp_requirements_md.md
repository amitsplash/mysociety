# Group Contribution Management SaaS – MVP Requirement Document

## 1. Project Overview

Build a mobile-first SaaS application for managing recurring group contributions, shared expenses, approvals, and member ledgers.

The application should support multiple types of groups including:
- Residential societies / RWAs
- Friends groups
- Clubs
- Office teams
- Communities

The platform must support multiple independent groups (multi-tenant architecture).

---

# 2. Technology Stack (MANDATORY)

## Backend
- C#
- ASP.NET Core Web API (.NET 9 preferred)

## Database
- SQLite

## ORM
- Entity Framework Core

## Mobile App
- React Native

## API Documentation
- Swagger / OpenAPI

## Authentication
- **Self-service registration** — username, email, name, password (`POST /api/auth/register`)
- **Sign-in** — username + password (`POST /api/auth/login`); invite-activated members may also sign in with **phone** + password
- JWT for API access; `X-Member-Id` for acting membership in the active group
- **Account activation** via admin-issued **invite code** for members added by phone (no SMS required for MVP)
- **Forgot password** — self-service **email reset code** (`POST /api/auth/reset-password/send-code` → `POST /api/auth/reset-password`); optional **admin-issued reset code** for members without a real email (see §4.2.1)
- Optional **SMS OTP** at activation when `Otp:Required` is enabled and an SMS provider is configured (off by default)
- Optional **SMTP email** for password reset when `Email:Smtp:Host` is configured; otherwise codes are logged (`LoggingEmailSender`) in development

---

# 3. Architecture Requirements

Use Clean Architecture principles.

Suggested structure:

```plaintext
/src
   /Api
   /Application
   /Domain
   /Infrastructure
```

Required layers:
- Controllers
- Services
- Repositories
- Database Context
- DTOs
- Validation
- Global Exception Handling

Use:
- Dependency Injection
- Async/Await
- Repository Pattern
- Service Layer

---

# 4. Core Functional Requirements

## 4.0 SaaS user model (implemented)

There is **no platform operator / SuperAdmin**. Any registered user is a **tenant user** who can create and manage their own groups.

| Concept | Storage | Capabilities |
|---|---|---|
| **Registered user** | `User` (username, email, password, optional phone) | Register, sign in, create groups, join groups via admin invite |
| **Group Admin** | `Member.Role = Admin` | In-group admin actions (members, contributions, society funds, group settings, delete group, optional admin password reset) |
| **Group Member** | `Member.Role = Member` | Member actions (own ledger, expenses, payments) |

Rules:
- `POST /api/auth/register` creates a user and returns JWT (empty memberships until they create or join a group).
- `POST /api/groups` — any authenticated user; **creator becomes group Admin** (no separate initial admin).
- `GET /api/groups` — lists groups where the user has a **membership** (not all groups in the system).
- `DELETE /api/groups/{id}` — **group Admin** only (`X-Member-Id` required).
- `GET/PUT /api/groups/{id}` — group member / admin via `X-Member-Id`.
- `Group.CreatedByUserId` records who created the group (audit); authorization uses **Admin role**, not this field alone.
- Legacy `PlatformRole`, bootstrap endpoint, and platform console have been **removed**.

---

## 4.1 Group Management

**Any registered user** should be able to:
- **Create a group** — configure contribution settings (type, model, frequency, amount); creator is automatically added as **Admin**
- Set optional **creator opening balance** and square feet (for per-sq-ft groups) at creation
- Set optional **opening maintenance fund** (`OpeningSocietyBalance`) and **opening corpus fund** (`OpeningCorpusBalance`) at group create
- Set optional **creator corpus amount** and whether corpus was **already received** (see §4.6.2)

**Group Admin** should be able to:
- Update group details — contribution settings only; opening maintenance and corpus funds are **not** editable after create
- **Delete the group** — permanently removes the group and all related data (members, contributions, payments, expenses, ledger, invites)

### Group Fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| Name | Group name |
| Type | RWA, Friends, Club, Office, Custom |
| ContributionModel | FIXED Or Per Square feet initially |
| ContributionAmount | Monthly amount |
| ContributionFrequency | MONTHLY/Quaterly, Half Yearly, Yearly |
| OpeningSocietyBalance | Optional initial **maintenance** fund (₹); set only at group create; default 0 |
| OpeningCorpusBalance | Optional initial **corpus** fund (₹); pre-collected corpus in bank at onboarding; default 0 |
| CreatedByUserId | User who created the group (audit) |
| CreatedAt | Timestamp |

### Opening maintenance fund (at group create)

Purpose:
- Record cash the society/group already holds for **day-to-day maintenance** when onboarding (e.g. bank balance, petty cash)
- Avoid starting maintenance funds at zero when migrating from manual books

Rules:
- Optional on `POST /api/groups`; must be ≥ 0
- Stored on the group; **not** a member ledger entry
- Counts as **maintenance fund inflow** in balance and maintenance ledger (first line: “Opening maintenance fund” on group `CreatedAt`)
- Cannot be changed via group update API (MVP)

### Opening corpus fund (at group create)

Purpose:
- Record **corpus already collected** before using the app (bulk onboarding from manual books)
- Members marked **corpus already paid** at add/create do **not** add a second inflow (see §4.6.2)

Rules:
- Optional on `POST /api/groups` as `openingCorpusBalance`; must be ≥ 0
- Counts as **corpus fund inflow** only (not maintenance)
- Cannot be changed via group update API (MVP)

---

# 4.2 Member Management

Users should be able to:
- Add members (admin)
- Update member details (admin) — name, phone, role, square feet
- Remove members (admin)
- Assign admin role
- Link an existing or new user to the group by phone (invite flow for users without a password)
- Set each member’s **corpus amount** and whether corpus was **already paid** at onboarding (see §4.6.2)
- **Mark corpus received** when a pending member pays (credits corpus fund)
- Issue an optional **password reset code** for an activated member (group admin fallback; see §4.2.1)

### User account fields (registered)

| Field | Description |
|---|---|
| Id | Unique identifier |
| Username | Unique login name (3–32 chars, alphanumeric + underscore) |
| Email | Unique email address |
| Name | Display name |
| Phone | Optional; set when added to a group by phone or updated by admin |
| PasswordHash | Bcrypt hash; empty until registration or invite activation |

### Member fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| GroupId | Group reference |
| UserId | Linked user account |
| Name | Member display name (from linked user) |
| Phone | On linked user; required for invite activation flow |
| Role | Member / Admin |
| SquareFeet | Required for per-square-feet contribution groups |
| OpeningBalance | Set on create; creates OPENING_BALANCE ledger entry |
| CorpusAmount | Expected one-time corpus for this member (0 if N/A) |
| CorpusPaidAt | When corpus was received; `null` = **pending** (if `CorpusAmount` > 0) |
| CreatedAt | Timestamp |

---

## 4.2.1 User accounts, registration, activation & credentials (implemented)

### Registration (self-service SaaS)

New users open the app → **Create account** → enter:

- Username (unique)
- Email (unique)
- Full name
- Password (min 8 characters)

`POST /api/auth/register` creates the user and returns **JWT + empty memberships** (`memberships: []`). Mobile app lands on **Home** with **Create group** CTA (no group selected until user creates or joins one).

### Sign-in

`POST /api/auth/login` with **username + password**. Users who were added by a group admin (phone invite) may sign in with **phone + password** once activated (synthetic username `user_{phone}` is assigned at invite time).

### Add member → invite (admin)

When an admin creates a member:

| User state | API behaviour |
|---|---|
| **New user** (no account password yet) | Create user (synthetic username/email from phone) + membership + one-time **invite** (8-character code, 7-day expiry). Response includes plaintext `inviteCode` **once** for the admin to share. |
| **Existing user** (already has password, e.g. joining a second group) | Add membership only; `requiresActivation: false`; no invite. |

**Mobile:** After add, if activation is required, show a modal with phone, invite code, expiry, and **Share** (system share sheet).

### Activate account (member, no login yet)

Member opens app → **Activate account with invite code** → enters:

- Phone (must match admin record)
- Invite code (from admin)
- New password (min 8 characters)

`POST /api/auth/activate` sets password, marks invite used, returns **JWT + memberships** (auto sign-in).

**Optional OTP (off by default):** If `Otp:Required` is `true` and SMS is configured, member must also verify a 6-digit OTP from `POST /api/auth/activate/send-otp`. Without an SMS provider, keep `Otp:Required: false` (invite-only).

### Forgot password (email reset code)

Primary self-service flow — **no admin required**:

1. Member: Sign-in → **Forgot password?** → enter **email** → **Send reset code**.
2. `POST /api/auth/reset-password/send-code` emails a **6-digit code** (15-minute expiry by default).
3. Member enters email + code + new password → `POST /api/auth/reset-password` → signed in.

Config (`Email` section in appsettings):

| Setting | Purpose |
|---|---|
| `Email:Smtp:Host` (and credentials) | Production SMTP delivery |
| *(empty Host)* | `LoggingEmailSender` — code logged to app logs (development) |
| `Email:PasswordReset:ExposeCodeInApi` | When `true`, API returns code in response (dev only) |
| `Email:PasswordReset:ResendCooldownSeconds` | Rate limit between send requests |

Rules:

- Response always uses a generic message (“If an account exists…”) to avoid email enumeration.
- Reset is only for users who **already have a password** (registered or activated). Pending invite members use **Activate account** instead.
- Each new send invalidates previous unused reset codes for that user.
- Email codes: 6-digit OTP, hashed at rest via `IOtpService`.

### Forgot password (admin fallback)

For invite-only users with placeholder email (`{phone}@invite.local`) who cannot receive email:

1. **Group admin:** Edit member → **Reset password** → API returns one-time **8-character reset code** (7-day expiry); share via WhatsApp/in person.
2. Member: Forgot password → enter **email on file** + admin code + new password → `POST /api/auth/reset-password`.

Admin-issued codes use `IInviteCodeService` (8-character); email codes use `IOtpService` (6-digit). The reset endpoint accepts either when matched to an active token.

### Security notes (MVP)

- Invite codes: random 8-character alphanumeric, SHA-256 hash at rest, constant-time verify.
- Email reset codes: random 6-digit numeric, hashed at rest.
- Activation/reset errors use generic messages where appropriate.
- Lost invite: admin removes and re-adds member (MVP; no regenerate API).

### Data entities

| Entity | Purpose |
|---|---|
| `MemberInvite` | One-time activation code per membership (when user has no password) |
| `PasswordResetToken` | One-time reset code per user (`CreatedByMemberId` when issued by group admin; email self-service has no issuer) |
| `PhoneOtpVerification` | Optional SMS OTP at activation when `Otp:Required` is enabled |

---

# 4.3 Opening Balance Feature

The system must support initializing member balances.

Purpose:
- Migration from manual bookkeeping
- Carry-forward balances

Rules:
- Positive balance = **prepaid credit** (member has paid ahead)
- Negative balance = **amount already due** (member owes from before onboarding)

When member created:
- Automatically create ledger entry:
  - Type = OPENING_BALANCE

Examples:
- +1000 → member has ₹1000 credit (prepaid)
- -1000 → member owes ₹1000 from prior books

**Important (billing vs ledger):** Opening credit/debit affects the **member ledger balance**. Contribution generation still stores the **full** period amount, but when an existing credit fully covers that amount at generation time, the contribution is created as **PAID** with zero pending (see §4.4).

---

# 4.4 Contribution Management

Admins generate contributions for a **month range** chosen in the UI (not auto-scheduled from group frequency).

### Generation rules (implemented)

- Request: `FromMonth` and `ToMonth` in `yyyy-MM` format (inclusive). **Future months are allowed.**
- Stored period key example: `2026-04..2026-06` (single month: `2026-05..2026-05`).
- Amount per member:
  - **Fixed model:** monthly rate × number of months in range.
  - **Per square feet model:** monthly rate × member square feet × number of months.
- **Always stores the full base amount** for the period in contribution `Amount`.
- If a member’s current ledger credit at generation time fully covers that amount, contribution `Status` is set to **PAID** immediately (no pending due shown for that period).
- If credit is insufficient, contribution remains **PENDING** and standard payment flow applies.
- **Overlap prevention:** generation is rejected if **any calendar month** in the requested range overlaps an existing group billing period (not only an exact period-key match). Example: May 2026 already generated → Mar–Jun 2026 is blocked.
- Duplicate member + exact period key is prevented (DB unique index + service checks).
- On generation: create contribution record + ledger **DEBIT** per member (when amount > 0).

`ContributionFrequency` on the group (Monthly / Quarterly / Half Yearly / Yearly) is **informational only** in the mobile UI; it does not drive automatic period calculation.

### Generate UI (mobile, admin)

- **Payments** tab shows a **Generate contributions** button (not an inline form).
- Opens a bottom sheet with **Month** and **Year** dropdowns for **From** and **To** (defaults: current month).
- Preview shows period label, approximate per-member amount, and overlap warning if applicable.
- **Generate** → confirmation dialog → API call.

### Contribution report & export (mobile, admin)

- **Contribution report** screen (stack route): tabular view grouped by billing period — columns: Member, Generated, Paid, Pending, Status (Paid / Partial / Pending).
- Entry points: Payments tab (“Open contribution report”) and Group hub.
- **Per-period CSV download:** export button on each period block; native share sheet on device, direct download on web.

### Contribution Fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| MemberId | Member reference |
| GroupId | Group reference |
| Period | Range key, e.g. `2026-04..2026-06` |
| Amount | Total amount billed for the period (full base amount) |
| Status | PENDING / PAID |
| CreatedAt | Timestamp |

### Payment recording (implemented)

- Members record payments against their own pending contributions; **admins** can record **cash received** on behalf of any member.
- **Partial payments** supported: multiple payments until `PaidAmount` reaches `Amount`; contribution stays **PENDING** until fully paid, then **PAID**.
- Payment amount cannot exceed remaining balance on the linked contribution.
- Creates `Payment` entity + ledger **CREDIT** entry.
- **Payments tab (admin):** consolidated **Pending collections** list grouped by member (no separate payment-history list in MVP UI).

---

# 4.5 Expense Management (two types)

## 4.5.1 Member expense (reimbursement)

Any member can submit an expense they paid out of pocket.

Admin approval required before it affects the member ledger.

### Member expense workflow

1. Member submits expense → `PENDING`
2. Admin approves → ledger **CREDIT** for submitter (reimbursement)
3. Admin rejects → no ledger entry

Does **not** reduce maintenance or corpus fund balances (see §4.6).

### Member expense fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| GroupId | Group reference |
| CreatedByMemberId | Submitter |
| Amount | Expense amount |
| Description | Expense details |
| ExpenseDate | Date of expense (defaults to today; **no future dates**) |
| Status | PENDING / APPROVED / REJECTED |
| ApprovedByMemberId | Admin reference (nullable) |
| CreatedAt | System timestamp when submitted |

## 4.5.2 Society expense (group cash pool)

Admins record spending from the **maintenance** or **corpus** fund (see §4.6).

- No approval workflow — recorded immediately.
- Deducts from the selected **fund balance** (`FundType`: Maintenance or Corpus).
- Blocked if amount exceeds available balance in that fund.

### Society expense fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| GroupId | Group reference |
| CreatedByMemberId | Admin who recorded it |
| Amount | Expense amount |
| Description | Expense details |
| ExpenseDate | Date of expense (defaults to today; **no future dates**) |
| FundType | `Maintenance` (default) or `Corpus` — which pool pays for this expense |
| CreatedAt | System timestamp when recorded |

---

# 4.6 Society Funds — Maintenance & Corpus (implemented)

RWA groups track **two separate cash pools**:

| Fund | Purpose (typical RWA) |
|---|---|
| **Maintenance** | Monthly dues, day-to-day repairs, utilities, recurring upkeep |
| **Corpus** | One-time capital / building fund collected from members at onboarding or later |

**Sinking fund** is out of scope for this phase (see §15).

### 4.6.1 Maintenance fund balance

```plaintext
Maintenance balance = OpeningSocietyBalance (at create)
                    + Total PAYMENT ledger credits (contribution collections)
                    − Total society expenses where FundType = Maintenance
```

- Contribution **payments** always credit the **maintenance** fund (not corpus).
- **Mobile UI:** Society funds screen shows **maintenance** and **corpus** balance cards (admin-only entry from Home / Group hub).
- **API:** `GET /api/groups/{id}/society-balance` returns `{ groupId, maintenance, corpus }` (`GroupFundsResponse`).

### 4.6.2 Corpus fund balance & member onboarding

```plaintext
Corpus balance = OpeningCorpusBalance (at create)
               + Total CORPUS_PAYMENT ledger credits (mark received only)
               − Total society expenses where FundType = Corpus
```

**Member corpus fields:** `CorpusAmount` (expected one-time amount), `CorpusPaidAt` (`null` = **pending** when amount > 0).

**Onboarding rules (no double-count with opening corpus):**

| Scenario | `CorpusPaidAt` | Corpus ledger inflow? |
|---|---|---|
| Add/create member with corpus **already paid** | Set at create | **No** — assumed covered by `OpeningCorpusBalance` and/or prior manual books |
| Add/create member with corpus **pending** | `null` | **No** until admin marks received |
| Admin **Mark received** (`POST /api/members/{id}/corpus/receive`) | Set to now | **Yes** — `CORPUS_PAYMENT` credit for `CorpusAmount` |

- Mark received is rejected if corpus amount is 0, already paid, or member not in caller’s group.
- Creator at group create: optional `creatorCorpusAmount` + `creatorCorpusPaid` (same rules as add member).

### 4.6.3 Society expense (fund-specific)

- Admin selects **Maintenance** or **Corpus** when recording a society expense.
- Validation uses the **selected fund’s** available balance only.
- Expense list shows fund type; mobile Add society expense includes fund picker.

### 4.6.4 Group ledger (fund-specific cash flow)

- **Admin Group ledger** shows society **inflow / outflow** with **Maintenance / Corpus tabs** (client filters API lines by `fundType`).
- **Maintenance inflows:** opening maintenance fund (if > 0), contribution payments.
- **Corpus inflows:** opening corpus fund (if > 0), corpus payments (mark received).
- **Outflows:** society expenses (per fund).
- Rows sorted **ascending by date** with **running balance per fund**; Group hub links **Maintenance ledger** and **Corpus ledger** separately.
- **Member ledgers** are opened from **Members** (book icon) or **My ledger** — not from Group ledger.

---

# 4.7 Ledger System (MOST IMPORTANT)

The ledger is the core financial engine.

The application must maintain immutable ledger entries.

Do NOT directly store calculated balance.

Balance must always be derived from ledger entries.

---

# 5. Ledger Rules

## 5.1 Ledger Entry Types

```plaintext
OPENING_BALANCE
CONTRIBUTION
PAYMENT
EXPENSE
ADJUSTMENT
CORPUS_PAYMENT
```

---

## 5.2 LedgerEntry Fields

| Field | Description |
|---|---|
| Id | Unique identifier |
| MemberId | Member reference |
| GroupId | Group reference |
| Type | Ledger entry type |
| Direction | Credit or Debit |
| Amount | Entry amount |
| ReferenceId | Related entity id (contribution, payment, expense, etc.) |
| CreatedAt | Timestamp |

Society expenses are **not** stored as member ledger rows; they use the `SocietyExpense` table and affect **maintenance or corpus** fund balance only (see §4.6).

Corpus collections use ledger type **`CORPUS_PAYMENT`** (group-level inflow; not a member balance credit).

---

# 6. Financial Logic

## 6.1 Contribution Generation

When monthly contribution generated:
- Create contribution record
- Create ledger DEBIT entry

---

## 6.2 Payment Recording

When payment received:
- Create PAYMENT ledger CREDIT entry

---

## 6.3 Member expense approval

When a **member** expense is approved:
- Create `EXPENSE` ledger **CREDIT** entry for the submitter.

## 6.4 Society expense recording

When a **society** expense is created:
- Persist `SocietyExpense` row with `FundType` (Maintenance or Corpus).
- Reduce computed balance for that fund only (see §6.6).

When corpus is **marked received**:
- Set member `CorpusPaidAt`.
- Create `CORPUS_PAYMENT` ledger **CREDIT** for the group (corpus fund inflow).

---

## 6.5 Member balance formula

```plaintext
Member balance = Total Credits − Total Debits
```

(Derived from ledger entries; never stored as a column.)

---

## 6.6 Fund balance formulas (implemented)

### Maintenance fund

```plaintext
Maintenance = OpeningSocietyBalance
            + Total PAYMENT credits (all members)
            − Total society expenses (FundType = Maintenance)
```

### Corpus fund

```plaintext
Corpus = OpeningCorpusBalance
       + Total CORPUS_PAYMENT credits
       − Total society expenses (FundType = Corpus)
```

- Member expense approvals do **not** change either fund (they adjust member ledger only).
- Contribution payments credit **maintenance** only; corpus inflows come from opening corpus and mark-received only.

---

# 7. Expense Adjustment Logic

Generated contributions keep the **full billed amount** in `Amount`, but existing ledger credit (opening balance or approved reimbursement credit) is applied at generation-time status calculation:

- If credit **covers full billed amount** → generated contribution is **PAID**.
- If credit is **less than billed amount** → generated contribution stays **PENDING**.
- Contribution `Amount` itself is **not reduced**.

Example:

| Scenario | Value |
|---|---|
| Monthly contribution billed | ₹2000 |
| Existing ledger credit | ₹500 |
| Generated contribution `Amount` | ₹2000 |
| Generated contribution status | PENDING |
| Pending shown in contribution row | ₹2000 (no invoice amount reduction in MVP) |

---

# 8. Roles & Authorization

## REGISTERED USER (`User`)

- Self-registers with username, email, name, password.
- Can **create groups** (becomes Admin of each created group).
- Can belong to multiple groups via memberships.
- JWT claims include `username` and `email` (not platform role).

## GROUP ADMIN (`Member.Role = Admin`)

Can:
- Approve/reject **member** expenses
- Add/update/remove members (admin UI); **Add member** also linked from Members screen and Group hub
- Set member **corpus amount** and paid/pending at add; **Mark corpus received** for pending members
- Share **invite codes** after adding members who need activation
- Issue **password reset codes** from Edit member (admin fallback for invite-only users)
- **Delete group** (permanent; from Group hub danger zone)
- View group member list, all member balances, each member’s ledger (from Members)
- View **Maintenance ledger** and **Corpus ledger** (fund-specific society cash-flow) and **Society funds**
- Generate contributions (month range popup), record full/partial cash payments for any member
- View **pending collections** summary and **contribution report** (tabular + CSV export)
- Record **society** expenses (maintenance or corpus fund)
- Update group settings

## MEMBER

Can:
- Submit **member** expenses (reimbursement) with expense date
- View own ledger and contribution status
- Record own payments
- Use **Expenses** tab to track submission status (not admin approval queue)

### API access rules (implemented)

| Endpoint / feature | Member | Admin |
|---|---|---|
| `GET /groups/{id}/members` | No | Yes |
| `GET /groups/{id}/balances` | No | Yes |
| `GET /groups/{id}/ledger-overview` | No | Yes (legacy; member drill-down uses per-member ledger API) |
| `GET /groups/{id}/society-ledger` | No | Yes |
| `GET /groups/{id}/society-balance` | Yes | Yes (returns maintenance + corpus) |
| `GET /groups/{id}/society-expenses` | Yes | Yes |
| `POST /society-expenses` | No | Yes (body includes `fundType`) |
| `POST /members/{id}/corpus/receive` | No | Yes |
| `GET /groups/{id}/contributions` | No | Yes (all members; includes paid/remaining) |
| `GET /groups/{id}/contributions/pending-summary` | No | Yes |
| `DELETE /groups/{id}` | No | Yes (group admin) |
| `GET /groups` (list) | Yes (own memberships) | Yes (own memberships) |
| `POST /groups` | Yes (any authenticated user) | Yes |
| `GET /ledger/{memberId}` | Own only | Any member in group |

Authentication:
- JWT on all protected routes.
- `X-Member-Id` header selects **acting group membership** for in-group APIs (group admin/member).

---

# 9. Mobile App Screens

React Native (Expo) app with tab navigation: **Home**, **Payments**, **Expenses**, **Group**.

### Implemented screens

| # | Screen | Notes |
|---|---|---|
| 1 | Login | Username + password; JWT session; links to Register, Activate account & Forgot password |
| 1a | Register | Username, email, name, password; auto sign-in; lands on Home with **Create group** when no memberships |
| 1b | Activate account | Phone + invite code + password; auto sign-in on success |
| 1c | Forgot password | Email → send 6-digit code → code + new password; auto sign-in |
| 2 | Create Group | Group settings; optional opening maintenance/corpus funds; creator opening balance, corpus amount/paid |
| 3 | Dashboard (Home) | Quick-action tiles with live metrics; empty state for new users without groups |
| 4 | Members List | Admin only; compact rows; balance; **pending corpus** badge; **Mark received**; edit; member ledger |
| 5 | Add Member | Opening balance; corpus amount + paid/pending; square feet (per-sq-ft); invite modal when activation required |
| 6 | Edit Member | Name, phone, role (Member/Admin); **Reset password** (admin fallback reset code modal) |
| 7 | Member expense (Add) | Reimbursement; expense date; pending until admin approves |
| 8 | Expenses tab | List + approve/reject (admin) |
| 9 | Ledger | **Admin group view:** Maintenance / Corpus tabs (fund-filtered society ledger). **All:** My ledger. **Members →** per-member ledger |
| 10 | Contributions (Payments tab) | Generate (month/year range popup + confirm); pending collections; partial/full pay; link to report |
| 10b | Contribution report | Admin; tabular report by period; per-period CSV export |
| 11 | Society funds | Admin only; **maintenance + corpus** balance cards, expense list, record expense |
| 12 | Add society expense | Admin only; **Maintenance / Corpus** fund picker; expense date; immediate deduction from selected fund |
| 13 | Group hub | Admin links incl. **Maintenance ledger**, **Corpus ledger**, Society funds; **Create group** when no membership; **Delete group** |

### Dashboard quick actions (implemented)

Tiles use **icon + title on one row**, with a **metric** and subtitle below:

| Tile | Who | Metric shown |
|---|---|---|
| Pay dues | All | Count of pending contributions |
| Society funds | Admin | Maintenance balance (₹); subtitle shows corpus balance |
| Maintenance ledger | Admin | — (opens maintenance fund cash-flow; Corpus via tab or Group hub) |
| My ledger | Member | — |
| Members | Admin | — |
| Review expenses | Admin | Count pending approval |
| Group hub | Member | — |

There is **no** duplicate “View society funds” button on Home; fund balances are on the Society funds tile and screen only.

### Mobile UX notes (implemented)

- Dark enterprise-style theme across components.
- Compact list rows: members (2-line), society expenses, member ledger entries.
- Group ledger: horizontal-scroll Excel-style columns (Date, Particulars, In, Out, Balance); **Maintenance / Corpus** segment tabs on admin group view.
- Group creation: any registered user; creator becomes Admin; optional dual opening funds and creator corpus.
- Empty home (no memberships): Dashboard and Group hub show **Create group** CTA; register flow tolerates empty `memberships` array without crash.
- Sign out from Home and Group tab.
- **Android:** tab bar respects safe-area insets (icons not hidden behind system navigation).
- **Add member:** opening balance hint (+ credit, − due); invite modal when activation required.
- **Generate contributions:** month + year dropdowns (From / To); future months allowed; overlap shown in preview.

---

# 10. API Requirements

Implement REST APIs.

## Auth APIs

```http
POST /api/auth/register             # anonymous; body: username, email, name, password → JWT
POST /api/auth/login                # anonymous; body: username, password (phone also accepted for invite users)
POST /api/auth/activate             # anonymous; body: phone, inviteCode, password [, otp if Otp:Required]
POST /api/auth/activate/send-otp    # anonymous; only when Otp:Required = true
POST /api/auth/reset-password/send-code   # anonymous; body: { email } → emails 6-digit code
POST /api/auth/reset-password       # anonymous; body: email, resetCode, newPassword → JWT
```

## Group APIs

```http
POST /api/groups          # any authenticated user; body: group fields + optional openingSocietyBalance,
                          #   openingCorpusBalance, creatorOpeningBalance, creatorSquareFeet,
                          #   creatorCorpusAmount, creatorCorpusPaid; creator added as Admin
GET /api/groups           # JWT; list groups where user has membership
GET /api/groups/{id}      # group member/admin (X-Member-Id)
PUT /api/groups/{id}      # group admin; does not change openingSocietyBalance or openingCorpusBalance
DELETE /api/groups/{id}   # group admin (X-Member-Id); permanent delete
```

---

## Member APIs

```http
POST /api/members
  # returns CreateMemberResponse: { member, requiresActivation, inviteCode?, inviteExpiresAt? }
  # body may include corpusAmount, corpusPaid (already received at onboarding)
GET /api/groups/{groupId}/members    # admin only
PUT /api/members/{id}                # update member details
DELETE /api/members/{id}
POST /api/members/{id}/password-reset   # admin only; returns reset code for sharing
POST /api/members/{id}/corpus/receive   # admin only; marks pending corpus paid + corpus ledger inflow
```

Member create/update supports `SquareFeet` when group uses per-square-feet model; `CorpusAmount` ≥ 0; `CorpusPaid` on create only.

---

## Contribution APIs

```http
POST /api/contributions/generate              # body: { groupId, fromMonth, toMonth }
POST /api/payments                              # body: { memberId, amount, contributionId? }
GET /api/members/{memberId}/contributions
GET /api/groups/{groupId}/contributions         # admin; all members; paidAmount, remainingAmount
GET /api/groups/{groupId}/contributions/pending-summary   # admin; outstanding by member
```

`ContributionResponse` includes `PaidAmount` and `RemainingAmount` for partial-payment UI.

Generate rejects overlapping month ranges with `409 Conflict` (see §4.4).

---

## Member expense APIs

```http
POST /api/expenses
  # body: { groupId, amount, description, expenseDate }
PATCH /api/expenses/{id}/approve
PATCH /api/expenses/{id}/reject
GET /api/groups/{groupId}/expenses
```

---

## Society funds APIs

```http
GET /api/groups/{groupId}/society-balance    # { groupId, maintenance, corpus } — FundBalanceDto each
GET /api/groups/{groupId}/society-expenses
POST /api/society-expenses           # admin only; body: { groupId, amount, description, expenseDate, fundType? }
                                     # fundType: Maintenance (default) | Corpus
```

---

## Ledger APIs

```http
GET /api/ledger/{memberId}
GET /api/groups/{groupId}/balances        # admin only
GET /api/groups/{groupId}/ledger-overview # admin only; legacy aggregate (UI uses society-ledger for group view)
GET /api/groups/{groupId}/society-ledger  # admin only; lines include fundType; running balance per fund
```

---

# 11. SQLite Requirements

Use SQLite as local database.

Requirements:
- Entity Framework Core migrations
- Proper indexes
- Foreign key constraints
- Transactions for financial operations

---

# 12. Critical Technical Requirements

## MUST HAVE

- Async APIs
- Proper validation
- Global exception handling
- Logging
- Swagger
- DTO separation
- Database migrations

---

# 13. Validation Rules

Examples:
- Contribution amount cannot be negative
- Expense cannot be approved twice
- Members must belong to same group
- Duplicate contribution generation for the same member + period key must be prevented
- **Overlapping** contribution month ranges for a group must be prevented (any shared calendar month)
- Payment amount cannot exceed remaining balance on a linked contribution
- **Expense date** cannot be in the future (member and society expenses)
- Society expense amount cannot exceed available balance in the **selected fund** (Maintenance or Corpus)
- Corpus **mark received** only when `CorpusAmount` > 0 and `CorpusPaidAt` is null; idempotent reject if already paid
- Pre-paid corpus at member add/create must **not** post a duplicate corpus ledger entry (opening corpus covers bulk onboarding)
- Group delete restricted to **group Admin** (not members)

---

# 14. Testing Requirements

Automated tests in `tests/Application.Tests/` (xUnit) cover:

- Ledger calculation and balances
- Contribution generation (month range, overlap rejection, full billing with opening credit/due) and payments (full + partial)
- Member expense approval / rejection
- Opening balance handling
- Member create/update (roles: Member / Admin only)
- Group create/delete (registered user creates; group admin deletes)
- Self-service email password reset + admin fallback reset code
- Dual fund balances (maintenance + corpus), corpus mark received, fund-specific society expenses
- Society ledger (inflow/outflow per fund, ordering and running balance)
- Corpus fund flows (`CorpusFundTests`: onboarding paid/pending, mark received, expense against corpus)
- Expense date validation (no future dates)
- Member invite on create + account activation (invite-only and optional OTP paths)
- Password reset (email self-service + admin issue code + member reset)
- Register/login response normalization (empty memberships array for new users)

**Not yet automated:** contribution report CSV export (mobile-only).

**Not in MVP:** SMS delivery for OTP/invite (admin shares codes manually unless `ISmsSender` is implemented). Email password reset uses SMTP when configured, otherwise logs codes in development.

---

# 15. MVP Scope Restrictions

DO NOT implement yet:
- **Sinking fund** (third pool; deferred after corpus phase 1)
- Expense splitting among members
- **Expense proof / receipt upload** (attachments)
- Online payment gateways
- Push notifications
- **Automated SMS/WhatsApp** for invite or OTP (admin shares codes manually in MVP)
- Regenerate lost invite / reset codes via API (re-add member or issue new reset from admin UI)
- Multi-level approval workflows
- Advanced analytics dashboards
- Automatic contribution scheduling from `ContributionFrequency` (manual month range only)

Keep MVP focused and stable.

---

# 16. Development Priority Order

Implement in this order:

1. Database schema
2. Entity Framework models
3. Ledger engine
4. Contribution generation
5. Expense workflow
6. APIs
7. Mobile UI
8. Testing

---

# 17. Important Notes

## Ledger correctness is highest priority.

Never directly manipulate balances.

Always generate ledger entries.

Use transactions for:
- Expense approval
- Contribution generation
- Payment recording

Avoid duplicate financial entries.

---

# 18. Expected Deliverables

Generate:
- Full backend code
- SQLite database setup
- Entity Framework migrations
- Swagger documentation
- React Native mobile app
- README setup guide
- Seed data for testing

---

# 19. Clarifications Added During Implementation

To keep implementation practical and stable across environments:

1. **.NET Version**  
   - Target framework: **.NET 8** (EF Core 8, SQLite).  
   - Upgrade path to .NET 9 documented in stack section; not required for MVP.

2. **Authentication**  
   - User table with username, email, optional phone; bcrypt password hashes.  
   - `POST /api/auth/register` and `POST /api/auth/login` (username; phone accepted for invite users) → JWT.  
   - All protected APIs: `Authorization: Bearer <token>` + `X-Member-Id: <memberGuid>`.  
   - Multi-group users switch active membership in the mobile app (persisted locally).

3. **Financial Precision**  
   - Money: `decimal(18,2)` in EF; immutable `LedgerEntry` rows; balances computed in services.

4. **Idempotency & Duplicate Prevention**  
   - Unique ledger constraint on member + type + reference id.  
   - Contribution duplicate prevention per member + period key.

5. **Group creation**  
   - `POST /api/groups` — any authenticated user; creator is added as `MemberRole.Admin`.  
   - Optional `creatorOpeningBalance` / `creatorSquareFeet` at create for the creator’s member record.

6. **Contribution periods**  
   - Admin opens **Generate contributions** popup; selects **From** and **To** via month + year dropdowns (defaults: current month). Future months allowed.  
   - API receives `fromMonth` / `toMonth`; frequency on group is display-only.  
   - Overlap with any existing group billing period is rejected (server + mobile preview).

7. **Per square feet groups**  
   - `SquareFeet` on member required when adding/updating members in per-sq-ft groups.

8. **Two expense channels**  
   - **Member expense:** reimbursement ledger credit after approval.  
   - **Society expense:** reduces **maintenance or corpus** fund (selected `FundType`); admin-only create.

9. **Society funds & Group ledger (UI)**  
   - **Society funds**, **Maintenance ledger**, and **Corpus ledger** on Home / Group hub: **admin-only** in the mobile app.  
   - Society funds tile shows maintenance balance (corpus in subtitle); Pay dues and Review expenses tiles show pending counts.  
   - Group ledger = fund-specific cash-flow table with Maintenance / Corpus tabs; member ledgers from **Members** screen.

10. **Expense date**  
    - Required on create for member and society expenses (`expenseDate` in API).  
    - Defaults to today in mobile (`YYYY-MM-DD`); server rejects future dates.

11. **CORS (development)**  
    - API allows localhost, Expo web ports, emulator, and LAN origins for mobile dev.

12. **Member activation (invite code)**  
    - `POST /api/members` returns `CreateMemberResponse` with optional `inviteCode` when the user has no password.  
    - `POST /api/auth/activate` completes setup; returns login payload.  
    - Default: **no SMS** — admin shares invite out of band.

13. **OTP at activation (optional)**  
    - Config: `Otp:Required` (default `false`), `Otp:ExpiryMinutes`, `Otp:ResendCooldownSeconds`, `Otp:ExposeCodeInApi` (dev only).  
    - When `Required` is `false`, `send-otp` returns validation error; mobile activate screen has no OTP fields.  
    - `LoggingSmsSender` logs OTP to application logs until a real `ISmsSender` is plugged in.

14. **Forgot password**  
    - **Primary:** `POST /api/auth/reset-password/send-code` (email) → `POST /api/auth/reset-password` (6-digit code) → JWT.  
    - **Fallback:** `POST /api/members/{id}/password-reset` (admin) → share 8-character `resetCode` for invite-only users.  
    - Not for users who never activated (use invite flow).

15. **Production onboarding**  
    - No bootstrap endpoint. First user **registers** via the app, then creates groups.  
    - Production has no demo seed.

16. **Deployment / operations**  
    - Production SQLite path (e.g. Linux App Service: `/home/data/mysociety.db`).  
    - Health: `/api/health`, `/health`.  
    - Serilog file logging under `LogFiles/Application`.  
    - Mobile API URL via `EXPO_PUBLIC_API_URL` at build time (EAS / `.env`).  
    - Azure pipeline targets **Linux** App Service (`webAppLinux`, `linux-x64`); connection string and JWT key required in app settings.

17. **Opening balance vs contribution billing**  
    - Opening balance (+/−) posts an `OPENING_BALANCE` ledger entry only.  
    - Generated contribution `Amount` is always the full calculated base for the period.  
    - If existing credit at generation time is enough to cover that amount, contribution is created as `PAID`; otherwise it remains `PENDING`.

18. **Partial payments & admin collections**  
    - Payments may be less than remaining balance until fully settled.  
    - Admin **Pending collections** API/UI groups outstanding items by member for cash recording.

19. **Contribution report export**  
    - Mobile admin report screen exports per-period CSV (UTF-8 BOM; share sheet on native, download on web) via `expo-file-system` + `expo-sharing`.

20. **Corpus fund (phase 1)**  
    - Two pools: maintenance (dues + maintenance expenses) and corpus (one-time capital + corpus expenses).  
    - `OpeningCorpusBalance` at group create; member `CorpusAmount` / `CorpusPaidAt`.  
    - Pre-paid corpus at add/create sets `CorpusPaidAt` but **does not** post `CORPUS_PAYMENT` (avoids double-count vs opening corpus).  
    - Pending corpus: admin **Mark received** → `CORPUS_PAYMENT` inflow.  
    - Sinking fund not implemented (§15).

21. **Register / empty memberships**  
    - New users after register may have `memberships: []`; mobile must normalize arrays and show Create group CTA (no navigator crash).

---

# 20. Implementation Status (synced with codebase)

| Area | Status |
|---|---|
| Clean Architecture (Api / Application / Domain / Infrastructure) | Done |
| SQLite + EF migrations (incl. `AddCorpusFund`, `Payments`, `SocietyExpenses`) | Done |
| Ledger engine + member balances | Done |
| Contributions (month range, overlap validation, full billing, credit-aware paid status) + partial payments | Done |
| Contribution report + per-period CSV export (mobile) | Done |
| SaaS auth (register, username login, email password reset) | Done |
| Group create (any user) / list (memberships) / delete (group admin) | Done |
| Member expenses (approve/reject) | Done |
| Dual fund balances (maintenance + corpus) + fund-specific society expenses + per-fund society ledger | Done |
| Corpus mark received + onboarding rules (no duplicate corpus inflow) | Done |
| Auth (JWT) + role-based authorization | Done |
| Member invite + account activation | Done |
| Forgot password (email self-service + admin fallback) | Done |
| Optional activation OTP (`Otp:Required`) | Done (disabled by default) |
| Email sender (`LoggingEmailSender` / optional SMTP) | Done |
| Swagger (dev) + global exception handler | Done |
| Mobile Expo app (tabs + stack screens) | Done |
| Application unit tests (62) | Done |
| Azure App Service Linux deploy + file logging | Done |

### Seed / demo logins (development only)

Seeded in **Development** environment (`DatabaseSeeder`):

| Login | Password | Typical role |
|---|---|---|
| `demo` (username) or `demo@example.com` | Password123! | Registered user; Admin of seeded “Sunrise RWA” group (`OpeningCorpusBalance` ₹5,00,000) |
| `9000000002` / `9000000003` (phone) | Password123! | Activated invite members; **Priya (`9000000003`)** has **pending corpus** (₹1,00,000) for Mark received testing |

Production has **no** demo seed. Users **register** via the app, then create groups. Admin-added members use **invite activation** (§4.2.1).

API default dev URL: `http://localhost:5221` (use host IP or `EXPO_PUBLIC_API_URL` when testing on device/APK).

### Delivery plan (original order — completed)

1. Project scaffolding  
2. Database schema + entities + migrations  
3. Ledger engine + tests  
4. Group and member APIs (+ update member)  
5. Contribution generation + payments  
6. Expense workflows (member + society)  
7. Mobile UI + API integration  
8. Seed data, tests, README  

**Out of scope for current MVP:** items listed in §15.

---

# 21. Document History

| Date | Change |
|---|---|
| Initial | MVP requirements baseline |
| 2026-05-27 | Synced with implementation: month-range contributions, society funds, member vs society expenses, admin APIs, mobile screens, auth rules, test coverage |
| 2026-05-28 | Group ledger as society inflow/outflow table; dashboard quick-action metrics; admin-only Society funds UI; expense date; compact member rows; society-ledger API |
| 2026-05-28 | Member invite activation; optional OTP (`Otp:Required`); admin-relay password reset; bootstrap user API; auth/mobile screens (Activate, Forgot password); §4.2.1; API & test updates |
| 2026-05-27 | Opening society fund at group create (`OpeningSocietyBalance`); included in society balance and group ledger |
| 2026-06-02 | Credit-aware contribution status update: full billed amount retained, but generation marks contribution **PAID** when existing ledger credit covers due (e.g., opening balance +₹500 vs ₹100 monthly). |
| 2026-06-02 | Platform vs group roles: `User.PlatformRole`, platform-only group create/list/delete, initial admin on create, platform password reset by phone; mobile Platform console; legacy `MemberRole.SuperAdmin` migrated to Admin. |
| 2026-06-03 | **Corpus fund (phase 1):** split maintenance/corpus pools; `OpeningCorpusBalance`, member corpus paid/pending, mark received API, `CORPUS_PAYMENT` ledger type, `FundType` on society expenses, dual balance API, Maintenance/Corpus ledger tabs; mobile corpus UI; register empty-memberships fix; 62 tests. Sinking fund deferred. |
| 2026-06-03 | **SaaS revamp:** removed platform operator / bootstrap / Platform console; self-service registration (username, email, password); any user creates groups as Admin; email forgot-password flow (`send-code` + 6-digit reset); admin fallback reset for invite users; mobile Register / updated Login / Forgot password screens; `Group.CreatedByUserId`; demo seed `demo` / `demo@example.com`. |
| 2026-05-28 | Payments overhaul: generate popup (month/year dropdowns), overlap validation, partial payments, pending collections, contribution report + CSV; SuperAdmin delete group; full contribution billing vs opening credit; group contribution APIs; Android safe-area; Azure Linux deploy. |

