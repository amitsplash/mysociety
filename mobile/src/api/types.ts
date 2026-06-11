export type GroupType = 'Rwa' | 'Friends' | 'Club' | 'Office' | 'Custom';
export type ContributionModel = 'Fixed' | 'PerSquareFeet';
export type ContributionFrequency = 'Monthly' | 'Quarterly' | 'HalfYearly' | 'Yearly';
export type GroupFundType = 'Maintenance' | 'Corpus';
export type MemberRole = 'Member' | 'Admin';
export type ExpenseStatus = 'Pending' | 'Approved' | 'Rejected';
export type ContributionStatus = 'Pending' | 'Paid';
export type CommitteeRole = 'President' | 'VicePresident' | 'Secretary' | 'Treasurer' | 'CommitteeMember';
export type MeetingType = 'Regular' | 'Committee' | 'Agm' | 'Special' | 'Other';
export type MeetingStatus = 'Draft' | 'UnderReview' | 'Approved' | 'Published' | 'Archived';
export type OpenMatterStatus = 'Open' | 'Finalized' | 'Cancelled';
export type MeetingItemOutcome = 'NotDiscussed' | 'Discussed' | 'Finalized' | 'Postponed' | 'NeedsMoreDiscussion';
export type AgendaItemSource = 'FromBacklog' | 'AdHoc';
export type ResolutionStatus = 'Open' | 'Active' | 'Completed' | 'Cancelled';
export type GroupDecisionSource = 'Minutes' | 'FormalResolution';
export type GroupDecisionFilter = 'All' | 'HasBudget';

export interface GroupDecisionResponse {
  id: string;
  source: GroupDecisionSource;
  decisionText: string;
  resolutionNumber?: string | null;
  resolutionId?: string | null;
  meetingId: string;
  meetingTitle: string;
  meetingStatus: MeetingStatus;
  meetingDate: string;
  isDraft: boolean;
  agendaItemId?: string | null;
  topicTitle?: string | null;
  openMatterId?: string | null;
  approvedBudget?: number | null;
  outcome?: MeetingItemOutcome | null;
  resolutionStatus?: ResolutionStatus | null;
  decidedAt: string;
}

export interface MinuteResponse {
  id: string;
  agendaItemId: string;
  discussionSummary?: string | null;
  decisionTaken?: string | null;
  budgetApproved?: number | null;
  createdAt: string;
}

export interface UpsertMinuteRequest {
  discussionSummary?: string | null;
  decisionTaken?: string | null;
  budgetApproved?: number | null;
}

export interface ResolutionResponse {
  id: string;
  groupId: string;
  meetingId: string;
  meetingTitle: string;
  meetingStatus: MeetingStatus;
  agendaItemId?: string | null;
  openMatterId?: string | null;
  resolutionNumber: string;
  title: string;
  description?: string | null;
  resolutionDate: string;
  approvedBudget?: number | null;
  status: ResolutionStatus;
  createdByMemberId: string;
  createdByName: string;
  createdAt: string;
}

export interface CreateResolutionRequest {
  meetingId: string;
  title: string;
  description?: string | null;
  agendaItemId?: string | null;
  openMatterId?: string | null;
  resolutionDate?: string | null;
  approvedBudget?: number | null;
  status?: ResolutionStatus;
}

export interface UpdateResolutionRequest {
  title?: string | null;
  description?: string | null;
  resolutionDate?: string | null;
  approvedBudget?: number | null;
  status?: ResolutionStatus | null;
}

export interface LoginRequest {
  phone: string;
  password: string;
}

export interface RegisterRequest {
  phone: string;
  email: string;
  name: string;
  password: string;
}

export interface MembershipSummary {
  memberId: string;
  groupId: string;
  groupName: string;
  role: MemberRole;
}

export interface LoginResponse {
  token: string;
  user: {
    id: string;
    username: string;
    email: string;
    name: string;
    phone?: string | null;
  };
  memberships: MembershipSummary[];
}

export interface GroupResponse {
  id: string;
  name: string;
  tagline?: string | null;
  logoUrl?: string | null;
  type: GroupType;
  contributionModel: ContributionModel;
  contributionAmount: number;
  contributionFrequency: ContributionFrequency;
  openingMaintenanceBalance: number;
  openingCorpusBalance: number;
  createdAt: string;
}

export interface CreateGroupRequest {
  name: string;
  type: GroupType;
  contributionModel: ContributionModel;
  contributionAmount: number;
  contributionFrequency: ContributionFrequency;
  openingMaintenanceBalance: number;
  openingCorpusBalance?: number;
  creatorOpeningBalance?: number;
  creatorSquareFeet?: number | null;
  creatorCorpusAmount?: number;
  creatorCorpusPaid?: boolean;
  tagline?: string | null;
  logoUrl?: string | null;
}

export interface UpdateGroupRequest {
  name: string;
  type: GroupType;
  contributionModel: ContributionModel;
  contributionAmount: number;
  contributionFrequency: ContributionFrequency;
  tagline?: string | null;
  logoUrl?: string | null;
}

export interface CreateGroupResponse {
  group: GroupResponse;
  creatorMember: MemberResponse;
}

export interface MemberResponse {
  id: string;
  groupId: string;
  name: string;
  phone?: string | null;
  role: MemberRole;
  squareFeet?: number | null;
  corpusAmount: number;
  corpusPaidAt?: string | null;
  createdAt: string;
}

export interface CreateMemberRequest {
  groupId: string;
  name: string;
  phone: string;
  role: MemberRole;
  openingBalance: number;
  squareFeet?: number | null;
  corpusAmount?: number;
  corpusPaid?: boolean;
}

export interface CreateMemberResponse {
  member: MemberResponse;
  requiresActivation: boolean;
  inviteCode?: string | null;
  inviteExpiresAt?: string | null;
}

export interface SendActivationOtpRequest {
  phone: string;
}

export interface SendActivationOtpResponse {
  message: string;
  expiresInSeconds: number;
  otp?: string | null;
}

export interface ActivateAccountRequest {
  phone: string;
  inviteCode: string;
  otp?: string | null;
  password: string;
  email?: string | null;
  name?: string | null;
}

export interface SendPasswordResetCodeRequest {
  email: string;
}

export interface SendPasswordResetCodeResponse {
  message: string;
  expiresInSeconds: number;
  resetCode?: string | null;
}

export interface ResetPasswordRequest {
  email: string;
  resetCode: string;
  newPassword: string;
}

export interface IssuePasswordResetResponse {
  phone?: string | null;
  username: string;
  name: string;
  resetCode: string;
  expiresAt: string;
}

export interface UpdateMemberRequest {
  name: string;
  phone: string;
  role: MemberRole;
  squareFeet?: number | null;
}

export interface ExpenseResponse {
  id: string;
  groupId: string;
  createdByMemberId: string;
  createdByName: string;
  amount: number;
  description: string;
  expenseDate: string;
  status: ExpenseStatus;
  approvedByMemberId?: string | null;
  createdAt: string;
}

export interface CreateExpenseRequest {
  groupId: string;
  amount: number;
  description: string;
  expenseDate: string;
}

export interface FundBalanceDto {
  balance: number;
  totalInflows: number;
  totalOutflows: number;
}

export interface GroupFundsResponse {
  groupId: string;
  maintenance: FundBalanceDto;
  corpus: FundBalanceDto;
}

export interface MarkCorpusReceivedResponse {
  member: MemberResponse;
  corpusAmountAdded: number;
  corpusFundBalance: number;
}

export interface FundLedgerLine {
  id: string;
  transactionDate: string;
  description: string;
  fundType: GroupFundType;
  inflow: number;
  outflow: number;
  runningBalance: number;
}

export interface FundLedgerResponse {
  groupId: string;
  funds: GroupFundsResponse;
  lines: FundLedgerLine[];
}

export interface GroupExpenseResponse {
  id: string;
  groupId: string;
  createdByMemberId: string;
  createdByName: string;
  amount: number;
  description: string;
  expenseDate: string;
  fundType: GroupFundType;
  createdAt: string;
}

export interface CreateGroupExpenseRequest {
  groupId: string;
  amount: number;
  description: string;
  expenseDate: string;
  fundType?: GroupFundType;
}

export interface GroupIncomeResponse {
  id: string;
  groupId: string;
  createdByMemberId: string;
  createdByName: string;
  amount: number;
  description: string;
  incomeDate: string;
  createdAt: string;
}

export interface CreateGroupIncomeRequest {
  groupId: string;
  amount: number;
  description: string;
  incomeDate: string;
}

export interface ContributionResponse {
  id: string;
  memberId: string;
  groupId: string;
  period: string;
  amount: number;
  status: ContributionStatus;
  createdAt: string;
  memberName?: string | null;
  paidAmount?: number;
  remainingAmount?: number;
  internalRemark?: string | null;
}

export interface PendingContributionItem {
  id: string;
  period: string;
  amount: number;
  paidAmount: number;
  remainingAmount: number;
  internalRemark?: string | null;
}

export type PaymentStatus = 'PendingApproval' | 'Approved' | 'Rejected';

export interface PaymentAllocationDetail {
  paymentId: string;
  contributionId?: string | null;
  period: string;
  amountApplied: number;
  remainingAfter: number;
  internalRemark?: string | null;
  status?: PaymentStatus;
}

export interface RecordPaymentResponse {
  submissionId: string;
  memberId: string;
  groupId: string;
  totalAmount: number;
  advanceAmount: number;
  status: PaymentStatus;
  createdAt: string;
  allocations: PaymentAllocationDetail[];
}

export interface PendingPaymentSubmission {
  submissionId: string;
  memberId: string;
  memberName: string;
  totalAmount: number;
  advanceAmount: number;
  submittedAt: string;
  allocations: PaymentAllocationDetail[];
}

export interface MemberPendingContributions {
  memberId: string;
  memberName: string;
  totalOutstanding: number;
  items: PendingContributionItem[];
}

export interface GroupPendingContributions {
  groupId: string;
  totalOutstanding: number;
  memberCount: number;
  members: MemberPendingContributions[];
}

export interface LedgerEntryDto {
  id: string;
  memberId: string;
  groupId: string;
  type: string;
  direction: string;
  amount: number;
  referenceId?: string | null;
  createdAt: string;
}

export interface MemberLedgerResponse {
  memberId: string;
  groupId: string;
  balance: number;
  entries: LedgerEntryDto[];
}

export interface MemberBalanceDto {
  memberId: string;
  memberName: string;
  balance: number;
}

export interface MemberLedgerSummary {
  memberId: string;
  memberName: string;
  balance: number;
  entries: LedgerEntryDto[];
}

export interface GroupLedgerOverviewResponse {
  groupId: string;
  members: MemberLedgerSummary[];
}

export interface ApiError {
  error: string;
  code?: string;
}

export interface CommitteeMemberResponse {
  id: string;
  groupId: string;
  memberId: string;
  memberName: string;
  role: CommitteeRole;
  createdAt: string;
}

export interface CreateCommitteeMemberRequest {
  memberId: string;
  role: CommitteeRole;
}

export interface UpdateCommitteeMemberRequest {
  role: CommitteeRole;
}

export interface OpenMatterSummaryResponse {
  openCount: number;
  finalizedCount: number;
  cancelledCount: number;
}

export interface OpenMatterResponse {
  id: string;
  groupId: string;
  title: string;
  description?: string | null;
  status: OpenMatterStatus;
  raisedAt: string;
  lastDiscussedInMeetingId?: string | null;
  createdByName: string;
  createdAt: string;
}

export interface CreateOpenMatterRequest {
  title: string;
  description?: string | null;
}

export interface UpdateOpenMatterRequest {
  title?: string | null;
  description?: string | null;
  status?: OpenMatterStatus | null;
}

export interface AgendaItemResponse {
  id: string;
  meetingId: string;
  openMatterId?: string | null;
  openMatterTitle?: string | null;
  agendaNumber: number;
  title: string;
  description?: string | null;
  displayOrder: number;
  source: AgendaItemSource;
  outcome: MeetingItemOutcome;
  discussionSummary?: string | null;
  minute?: MinuteResponse | null;
  createdAt: string;
}

export interface CreateAgendaItemRequest {
  title: string;
  description?: string | null;
  openMatterId?: string | null;
  source?: AgendaItemSource;
}

export interface UpdateAgendaOutcomeRequest {
  outcome: MeetingItemOutcome;
  discussionSummary?: string | null;
}

export interface MeetingAttendeeResponse {
  id: string;
  memberId: string;
  memberName: string;
  attendanceStatus: string;
}

export interface MeetingSummaryResponse {
  id: string;
  groupId: string;
  title: string;
  meetingType: MeetingType;
  meetingDate: string;
  startTime?: string | null;
  endTime?: string | null;
  location?: string | null;
  summary?: string | null;
  status: MeetingStatus;
  createdByMemberId: string;
  createdByName: string;
  agendaItemCount: number;
  createdAt: string;
}

export interface MeetingDetailResponse {
  id: string;
  groupId: string;
  title: string;
  meetingType: MeetingType;
  meetingDate: string;
  startTime?: string | null;
  endTime?: string | null;
  location?: string | null;
  summary?: string | null;
  status: MeetingStatus;
  createdByMemberId: string;
  createdByName: string;
  agendaItems: AgendaItemResponse[];
  attendees: MeetingAttendeeResponse[];
  resolutions: ResolutionResponse[];
  decisions: GroupDecisionResponse[];
  createdAt: string;
}

export interface CreateMeetingRequest {
  title: string;
  meetingDate: string;
  meetingType?: MeetingType;
  startTime?: string | null;
  endTime?: string | null;
  location?: string | null;
  summary?: string | null;
  status?: MeetingStatus;
}

export interface UpdateMeetingRequest {
  title?: string | null;
  meetingDate?: string | null;
  meetingType?: MeetingType | null;
  startTime?: string | null;
  endTime?: string | null;
  location?: string | null;
  summary?: string | null;
}

export interface UpdateMeetingStatusRequest {
  status: MeetingStatus;
}

export type AssetCategory =
  | 'Lift'
  | 'Generator'
  | 'WaterPump'
  | 'Electrical'
  | 'Hvac'
  | 'Plumbing'
  | 'Security'
  | 'FireSafety'
  | 'Other';

export type AssetStatus = 'Active' | 'Inactive' | 'Decommissioned';

export type AssetMaintenanceStatus = 'Ok' | 'DueSoon' | 'Overdue' | 'NotScheduled';

export interface AssetResponse {
  id: string;
  groupId: string;
  createdByMemberId: string;
  createdByName: string;
  name: string;
  category: AssetCategory;
  location?: string | null;
  description?: string | null;
  serialNumber?: string | null;
  vendorName?: string | null;
  installDate?: string | null;
  status: AssetStatus;
  maintenanceIntervalDays: number;
  alertLeadDays: number;
  nextDueDate?: string | null;
  maintenanceStatus: AssetMaintenanceStatus;
  createdAt: string;
}

export interface CreateAssetRequest {
  groupId: string;
  name: string;
  category: AssetCategory;
  location?: string | null;
  description?: string | null;
  serialNumber?: string | null;
  vendorName?: string | null;
  installDate?: string | null;
  status: AssetStatus;
  maintenanceIntervalDays: number;
  alertLeadDays: number;
}

export interface UpdateAssetRequest {
  name: string;
  category: AssetCategory;
  location?: string | null;
  description?: string | null;
  serialNumber?: string | null;
  vendorName?: string | null;
  installDate?: string | null;
  status: AssetStatus;
  maintenanceIntervalDays: number;
  alertLeadDays: number;
}

export interface AssetMaintenanceSummaryResponse {
  dueSoonCount: number;
  overdueCount: number;
  totalActiveAssets: number;
}

export interface MaintenanceRecordResponse {
  id: string;
  assetId: string;
  groupId: string;
  createdByMemberId: string;
  createdByName: string;
  performedDate: string;
  description: string;
  cost?: number | null;
  vendorName?: string | null;
  notes?: string | null;
  createdAt: string;
}

export interface CreateMaintenanceRecordRequest {
  assetId: string;
  groupId: string;
  performedDate: string;
  description: string;
  cost?: number | null;
  vendorName?: string | null;
  notes?: string | null;
}

export type NotificationType =
  | 'ContributionsGenerated'
  | 'MaintenanceDueSoon'
  | 'MaintenanceOverdue';

export interface NotificationResponse {
  id: string;
  groupId: string;
  groupName: string;
  type: NotificationType;
  title: string;
  body: string;
  dataJson?: string | null;
  readAt?: string | null;
  createdAt: string;
}

export interface UnreadNotificationCountResponse {
  count: number;
}
