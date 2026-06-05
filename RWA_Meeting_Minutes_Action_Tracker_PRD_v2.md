# RWA Management App
# Meeting Minutes, Resolution & Action Tracker Module
## Product Requirements Document (PRD) - Version 2.0

## 1. Executive Summary

This module enables RWAs and Apartment Associations to:
- Conduct and document meetings
- Record agenda discussions
- Capture resolutions and decisions
- Assign action items
- Track execution status
- Provide transparency to residents
- Maintain a permanent governance record

This feature extends the existing RWA application where User Management, Authentication, Roles and Resident Directory already exist.

---

# 2. Business Objectives

1. Digitize all committee meetings.
2. Ensure accountability for assigned actions.
3. Maintain historical decisions.
4. Improve transparency.
5. Simplify committee handover.
6. Reduce dependency on WhatsApp messages.

---

# 3. User Roles

## Super Admin
Full control.

## Committee Member
Create and manage meetings.

## Action Owner
Update assigned tasks.

## Resident
View published records.

---

# 4. User Stories

### Meeting Creation

As a Committee Member,
I want to create a meeting,
So that discussions can be formally recorded.

Acceptance Criteria:
- Meeting can be saved as draft.
- Meeting can be published.
- Agenda items can be added.

### Action Tracking

As a Committee Member,
I want to assign tasks,
So that work ownership is clear.

Acceptance Criteria:
- Task owner mandatory.
- Due date mandatory.
- Notifications generated.

### Resident Visibility

As a Resident,
I want to see approved minutes,
So that society decisions remain transparent.

Acceptance Criteria:
- Only approved minutes visible.
- Drafts hidden.

---

# 5. Functional Modules

## Module 1 – Meeting Management

### Fields

MeetingId
Title
MeetingType
Date
StartTime
EndTime
Location
Status
CreatedBy

### Status

Draft
UnderReview
Approved
Published
Archived

---

## Module 2 – Agenda Management

### Fields

AgendaId
MeetingId
AgendaNumber
Title
Description
DisplayOrder

Features:
- Add agenda
- Reorder agenda
- Delete agenda

---

## Module 3 – Minutes Recording

### Fields

MinuteId
AgendaId
DiscussionSummary
DecisionTaken
BudgetApproved
AttachmentUrl

Features:
- Rich text editor
- Attachment upload
- Version history

---

## Module 4 – Resolution Management

### Purpose

Maintain permanent committee resolutions.

### Resolution Fields

ResolutionId
ResolutionNumber
MeetingId
Title
Description
ResolutionDate
ApprovedBudget
Status

### Status

Open
Active
Completed
Cancelled

### Sample

RES-2026-001
Install Borewell Filtration System

RES-2026-002
Appoint Security Agency

---

## Module 5 – Action Tracker

### Fields

ActionId
MeetingId
ResolutionId
Title
Description
AssignedTo
Priority
DueDate
CompletionDate
Status

### Priority

Low
Medium
High
Critical

### Status

Open
InProgress
OnHold
Completed
Cancelled

---

## Module 6 – Comments & Updates

Fields:

CommentId
ActionId
Comment
CreatedBy
CreatedDate

Features:
- Progress updates
- Photos
- Documents

---

## Module 7 – Notifications

Trigger Events:

- Task Assigned
- Task Due in 3 Days
- Task Overdue
- Meeting Created
- Minutes Published
- Resolution Approved

Channels:

- In App
- Email

Future:
- WhatsApp

---

# 6. Mobile Screens

## Screen 1
Meeting List

Features:
- Search
- Filter
- Create Meeting

## Screen 2
Meeting Details

Sections:
- Meeting Info
- Attendees
- Agenda
- Minutes
- Resolutions
- Actions

## Screen 3
Create Meeting

Fields:
- Meeting Name
- Date
- Time
- Venue

## Screen 4
Agenda Editor

Add/Edit/Delete Agenda.

## Screen 5
Minutes Editor

Capture:
- Discussion
- Decision
- Attachments

## Screen 6
Resolution Register

List of all resolutions.

## Screen 7
Action Dashboard

Cards:
- Open
- In Progress
- Completed
- Overdue

## Screen 8
My Tasks

Assigned tasks only.

## Screen 9
Resident View

Read-only access.

---

# 7. Dashboard Requirements

## Committee Dashboard

Widgets:

- Upcoming Meetings
- Open Actions
- Overdue Actions
- Active Resolutions
- Recent Decisions

## Resident Dashboard

Widgets:

- Published Minutes
- Recent Decisions
- Upcoming Meetings
- Notices

---

# 8. Database Design

## Meetings

MeetingId PK
Title
MeetingType
MeetingDate
StartTime
EndTime
Location
Status
CreatedBy
CreatedDate

## MeetingAttendees

MeetingAttendeeId PK
MeetingId FK
UserId FK
AttendanceStatus

## AgendaItems

AgendaId PK
MeetingId FK
Title
Description
DisplayOrder

## Minutes

MinuteId PK
AgendaId FK
DiscussionSummary
DecisionTaken
BudgetApproved

## Resolutions

ResolutionId PK
MeetingId FK
ResolutionNumber
Title
Description
Status

## ActionItems

ActionId PK
MeetingId FK
ResolutionId FK
AssignedTo FK
Title
Description
DueDate
Status

## ActionComments

CommentId PK
ActionId FK
Comment
CreatedBy
CreatedDate

---

# 9. REST API Design

## Meetings

GET /api/meetings

GET /api/meetings/{id}

POST /api/meetings

PUT /api/meetings/{id}

DELETE /api/meetings/{id}

## Agenda

POST /api/meetings/{id}/agenda

PUT /api/agenda/{id}

DELETE /api/agenda/{id}

## Minutes

POST /api/agenda/{id}/minutes

PUT /api/minutes/{id}

## Resolutions

GET /api/resolutions

POST /api/resolutions

PUT /api/resolutions/{id}

## Actions

GET /api/actions

GET /api/actions/my

POST /api/actions

PUT /api/actions/{id}

## Comments

POST /api/actions/{id}/comments

---

# 10. Permissions Matrix

Feature | Admin | Committee | Owner | Resident
--------|--------|--------|--------|--------
View Meetings | Yes | Yes | Yes | Published Only
Create Meeting | Yes | Yes | No | No
Edit Meeting | Yes | Yes | No | No
Create Action | Yes | Yes | No | No
Update Action | Yes | Yes | Assigned Only | No
View Resolutions | Yes | Yes | Yes | Published Only

---

# 11. Reports

Meeting Report PDF

Includes:
- Meeting Details
- Attendees
- Agenda
- Minutes
- Resolutions
- Action Items

Action Report

Filters:
- Status
- Owner
- Date Range

Resolution Report

Filters:
- Active
- Completed
- Cancelled

---

# 12. Audit Requirements

Track:

CreatedBy
CreatedDate
ModifiedBy
ModifiedDate

For all entities.

---

# 13. Non Functional Requirements

Security:
- Role based authorization
- API authentication

Performance:
- Dashboard < 5 sec
- Meeting page < 3 sec

Scalability:
- 1000+ meetings
- 10000+ actions

Availability:
- 99.5% uptime

---

# 14. Phase 2 Enhancements

- AI generated minutes
- Voice recording transcription
- WhatsApp notifications
- Resident voting
- Budget approval workflow
- Vendor quotation comparison
- Digital signatures
- AGM management

---

# Success Metrics

- 100% meetings recorded digitally
- 100% action ownership
- Reduced overdue tasks
- Improved transparency
- Faster committee handover
