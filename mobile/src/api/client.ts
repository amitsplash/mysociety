import { getApiBaseUrl } from '../config/api';
import type { ApiError } from './types';

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface RequestOptions {
  method?: HttpMethod;
  body?: unknown;
  token?: string | null;
  memberId?: string | null;
  auth?: boolean;
}

export class ApiClientError extends Error {
  status: number;
  code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

const REQUEST_TIMEOUT_MS = 15_000;

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let baseUrl: string;
  try {
    baseUrl = getApiBaseUrl();
  } catch (e) {
    const message = e instanceof Error ? e.message : 'API URL is not configured';
    throw new ApiClientError(message, 0);
  }

  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.auth !== false && options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  if (options.memberId) {
    headers['X-Member-Id'] = options.memberId;
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  let response: Response;
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: controller.signal,
    });
  } catch (e) {
    if (e instanceof Error && e.name === 'AbortError') {
      throw new ApiClientError(
        `Could not reach the API at ${baseUrl}. Is the server running? On a phone, set EXPO_PUBLIC_API_URL in mobile/.env to your PC's LAN IP and use launch profile http-mobile.`,
        0,
      );
    }
    const message =
      e instanceof TypeError
        ? `Network error calling ${baseUrl}. Check Wi‑Fi, firewall, and EXPO_PUBLIC_API_URL.`
        : e instanceof Error
          ? e.message
          : 'Network request failed';
    throw new ApiClientError(message, 0);
  } finally {
    clearTimeout(timeoutId);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      throw new ApiClientError(`Invalid JSON from server (${response.status})`, response.status);
    }
  }

  if (!response.ok) {
    const err = data as ApiError | null;
    throw new ApiClientError(
      err?.error ?? `Request failed (${response.status})`,
      response.status,
      err?.code,
    );
  }

  return data as T;
}

export function normalizeLoginResponse(
  data: import('./types').LoginResponse,
): import('./types').LoginResponse {
  return {
    token: data.token,
    user: data.user,
    memberships: Array.isArray(data.memberships) ? data.memberships : [],
  };
}

export const api = {
  register: async (body: import('./types').RegisterRequest) =>
    normalizeLoginResponse(
      await apiRequest<import('./types').LoginResponse>('/api/auth/register', {
        method: 'POST',
        body,
        auth: false,
      }),
    ),

  login: async (body: import('./types').LoginRequest) =>
    normalizeLoginResponse(
      await apiRequest<import('./types').LoginResponse>('/api/auth/login', {
        method: 'POST',
        body,
        auth: false,
      }),
    ),

  getGroup: (id: string, token: string, memberId: string) =>
    apiRequest<import('./types').GroupResponse>(`/api/groups/${id}`, { token, memberId }),

  createGroup: (body: import('./types').CreateGroupRequest, token: string) =>
    apiRequest<import('./types').CreateGroupResponse>('/api/groups', {
      method: 'POST',
      body,
      token,
    }),

  updateGroup: (
    groupId: string,
    body: import('./types').UpdateGroupRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').GroupResponse>(`/api/groups/${groupId}`, {
      method: 'PUT',
      body,
      token,
      memberId,
    }),

  deleteGroup: (groupId: string, token: string, memberId: string) =>
    apiRequest<void>(`/api/groups/${groupId}`, {
      method: 'DELETE',
      token,
      memberId,
    }),

  getMembers: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').MemberResponse[]>(`/api/groups/${groupId}/members`, {
      token,
      memberId,
    }),

  createMember: (body: import('./types').CreateMemberRequest, token: string, memberId: string) =>
    apiRequest<import('./types').CreateMemberResponse>('/api/members', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  sendActivationOtp: (body: import('./types').SendActivationOtpRequest) =>
    apiRequest<import('./types').SendActivationOtpResponse>('/api/auth/activate/send-otp', {
      method: 'POST',
      body,
      auth: false,
    }),

  activateAccount: async (body: import('./types').ActivateAccountRequest) =>
    normalizeLoginResponse(
      await apiRequest<import('./types').LoginResponse>('/api/auth/activate', {
        method: 'POST',
        body,
        auth: false,
      }),
    ),

  sendPasswordResetCode: (body: import('./types').SendPasswordResetCodeRequest) =>
    apiRequest<import('./types').SendPasswordResetCodeResponse>('/api/auth/reset-password/send-code', {
      method: 'POST',
      body,
      auth: false,
    }),

  resetPassword: async (body: import('./types').ResetPasswordRequest) =>
    normalizeLoginResponse(
      await apiRequest<import('./types').LoginResponse>('/api/auth/reset-password', {
        method: 'POST',
        body,
        auth: false,
      }),
    ),

  issuePasswordReset: (memberId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').IssuePasswordResetResponse>(
      `/api/members/${memberId}/password-reset`,
      { method: 'POST', token, memberId: actingMemberId },
    ),

  updateMember: (
    id: string,
    body: import('./types').UpdateMemberRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MemberResponse>(`/api/members/${id}`, {
      method: 'PUT',
      body,
      token,
      memberId,
    }),

  markCorpusReceived: (memberId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').MarkCorpusReceivedResponse>(
      `/api/members/${memberId}/corpus/receive`,
      { method: 'POST', token, memberId: actingMemberId },
    ),

  getExpenses: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').ExpenseResponse[]>(`/api/groups/${groupId}/expenses`, {
      token,
      memberId,
    }),

  createExpense: (body: import('./types').CreateExpenseRequest, token: string, memberId: string) =>
    apiRequest<import('./types').ExpenseResponse>('/api/expenses', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  approveExpense: (id: string, token: string, memberId: string) =>
    apiRequest<import('./types').ExpenseResponse>(`/api/expenses/${id}/approve`, {
      method: 'PATCH',
      token,
      memberId,
    }),

  rejectExpense: (id: string, token: string, memberId: string) =>
    apiRequest<import('./types').ExpenseResponse>(`/api/expenses/${id}/reject`, {
      method: 'PATCH',
      token,
      memberId,
    }),

  getContributions: (memberId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').ContributionResponse[]>(
      `/api/members/${memberId}/contributions`,
      { token, memberId: actingMemberId },
    ),

  getGroupContributions: (groupId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').ContributionResponse[]>(
      `/api/groups/${groupId}/contributions`,
      { token, memberId: actingMemberId },
    ),

  getPendingContributionsSummary: (groupId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').GroupPendingContributions>(
      `/api/groups/${groupId}/contributions/pending-summary`,
      { token, memberId: actingMemberId },
    ),

  generateContributions: (
    body: { groupId: string; fromMonth: string; toMonth: string },
    token: string,
    memberId: string,
  ) =>
    apiRequest<{
      createdCount: number;
      period: string;
      fromMonth: string;
      toMonth: string;
      monthCount: number;
    }>('/api/contributions/generate', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  recordPayment: (
    body: { memberId: string; amount: number; contributionId?: string },
    token: string,
    actingMemberId: string,
  ) =>
    apiRequest<import('./types').RecordPaymentResponse>('/api/payments', {
      method: 'POST',
      body,
      token,
      memberId: actingMemberId,
    }),

  getPendingPaymentSubmissions: (groupId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').PendingPaymentSubmission[]>(
      `/api/groups/${groupId}/payments/pending-approval`,
      { token, memberId: actingMemberId },
    ),

  getMyPendingPaymentSubmissions: (token: string, actingMemberId: string) =>
    apiRequest<import('./types').PendingPaymentSubmission[]>('/api/payments/my-pending-approval', {
      token,
      memberId: actingMemberId,
    }),

  approvePaymentSubmission: (submissionId: string, token: string, actingMemberId: string) =>
    apiRequest<unknown>(`/api/payments/submissions/${submissionId}/approve`, {
      method: 'POST',
      token,
      memberId: actingMemberId,
    }),

  rejectPaymentSubmission: (submissionId: string, token: string, actingMemberId: string) =>
    apiRequest<unknown>(`/api/payments/submissions/${submissionId}/reject`, {
      method: 'POST',
      token,
      memberId: actingMemberId,
    }),

  getLedger: (memberId: string, token: string, actingMemberId: string) =>
    apiRequest<import('./types').MemberLedgerResponse>(`/api/ledger/${memberId}`, {
      token,
      memberId: actingMemberId,
    }),

  getBalances: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').MemberBalanceDto[]>(`/api/groups/${groupId}/balances`, {
      token,
      memberId,
    }),

  getLedgerOverview: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').GroupLedgerOverviewResponse>(
      `/api/groups/${groupId}/ledger-overview`,
      { token, memberId },
    ),

  getFundLedger: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').FundLedgerResponse>(
      `/api/groups/${groupId}/fund-ledger`,
      { token, memberId },
    ),

  getGroupFunds: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').GroupFundsResponse>(
      `/api/groups/${groupId}/group-funds`,
      { token, memberId },
    ),

  getGroupExpenses: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').GroupExpenseResponse[]>(
      `/api/groups/${groupId}/group-expenses`,
      { token, memberId },
    ),

  createGroupExpense: (
    body: import('./types').CreateGroupExpenseRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').GroupExpenseResponse>('/api/group-expenses', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  getGroupIncomes: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').GroupIncomeResponse[]>(
      `/api/groups/${groupId}/group-incomes`,
      { token, memberId },
    ),

  createGroupIncome: (
    body: import('./types').CreateGroupIncomeRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').GroupIncomeResponse>('/api/group-incomes', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  getCommitteeMembers: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').CommitteeMemberResponse[]>(
      `/api/groups/${groupId}/committee-members`,
      { token, memberId },
    ),

  createCommitteeMember: (
    groupId: string,
    body: import('./types').CreateCommitteeMemberRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').CommitteeMemberResponse>(
      `/api/groups/${groupId}/committee-members`,
      { method: 'POST', body, token, memberId },
    ),

  updateCommitteeMember: (
    groupId: string,
    id: string,
    body: import('./types').UpdateCommitteeMemberRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').CommitteeMemberResponse>(
      `/api/groups/${groupId}/committee-members/${id}`,
      { method: 'PUT', body, token, memberId },
    ),

  deleteCommitteeMember: (groupId: string, id: string, token: string, memberId: string) =>
    apiRequest<void>(`/api/groups/${groupId}/committee-members/${id}`, {
      method: 'DELETE',
      token,
      memberId,
    }),

  getOpenMattersSummary: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').OpenMatterSummaryResponse>(
      `/api/groups/${groupId}/open-matters/summary`,
      { token, memberId },
    ),

  getOpenMatters: (groupId: string, token: string, memberId: string, status?: string) => {
    const query = status ? `?status=${status}` : '';
    return apiRequest<import('./types').OpenMatterResponse[]>(
      `/api/groups/${groupId}/open-matters${query}`,
      { token, memberId },
    );
  },

  createOpenMatter: (
    groupId: string,
    body: import('./types').CreateOpenMatterRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').OpenMatterResponse>(`/api/groups/${groupId}/open-matters`, {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  updateOpenMatter: (
    groupId: string,
    id: string,
    body: import('./types').UpdateOpenMatterRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').OpenMatterResponse>(`/api/groups/${groupId}/open-matters/${id}`, {
      method: 'PUT',
      body,
      token,
      memberId,
    }),

  promoteAgendaToOpenMatter: (
    groupId: string,
    agendaItemId: string,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').OpenMatterResponse>(
      `/api/groups/${groupId}/open-matters/from-agenda/${agendaItemId}`,
      { method: 'POST', token, memberId },
    ),

  getMeetings: (groupId: string, token: string, memberId: string, status?: string) => {
    const query = status ? `?status=${status}` : '';
    return apiRequest<import('./types').MeetingSummaryResponse[]>(
      `/api/groups/${groupId}/meetings${query}`,
      { token, memberId },
    );
  },

  getMeeting: (groupId: string, meetingId: string, token: string, memberId: string) =>
    apiRequest<import('./types').MeetingDetailResponse>(
      `/api/groups/${groupId}/meetings/${meetingId}`,
      { token, memberId },
    ),

  createMeeting: (
    groupId: string,
    body: import('./types').CreateMeetingRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MeetingDetailResponse>(`/api/groups/${groupId}/meetings`, {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  updateMeeting: (
    groupId: string,
    meetingId: string,
    body: import('./types').UpdateMeetingRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MeetingDetailResponse>(
      `/api/groups/${groupId}/meetings/${meetingId}`,
      { method: 'PUT', body, token, memberId },
    ),

  updateMeetingStatus: (
    groupId: string,
    meetingId: string,
    body: import('./types').UpdateMeetingStatusRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MeetingDetailResponse>(
      `/api/groups/${groupId}/meetings/${meetingId}/status`,
      { method: 'PATCH', body, token, memberId },
    ),

  deleteMeeting: (groupId: string, meetingId: string, token: string, memberId: string) =>
    apiRequest<void>(`/api/groups/${groupId}/meetings/${meetingId}`, {
      method: 'DELETE',
      token,
      memberId,
    }),

  addAgendaItem: (
    groupId: string,
    meetingId: string,
    body: import('./types').CreateAgendaItemRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AgendaItemResponse>(
      `/api/groups/${groupId}/meetings/${meetingId}/agenda`,
      { method: 'POST', body, token, memberId },
    ),

  addAgendaFromOpenMatter: (
    groupId: string,
    meetingId: string,
    openMatterId: string,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AgendaItemResponse>(
      `/api/groups/${groupId}/meetings/${meetingId}/agenda/from-open-matter/${openMatterId}`,
      { method: 'POST', token, memberId },
    ),

  updateAgendaOutcome: (
    groupId: string,
    agendaItemId: string,
    body: import('./types').UpdateAgendaOutcomeRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AgendaItemResponse>(
      `/api/groups/${groupId}/agenda/${agendaItemId}/outcome`,
      { method: 'PATCH', body, token, memberId },
    ),

  deleteAgendaItem: (groupId: string, agendaItemId: string, token: string, memberId: string) =>
    apiRequest<void>(`/api/groups/${groupId}/agenda/${agendaItemId}`, {
      method: 'DELETE',
      token,
      memberId,
    }),

  upsertMinute: (
    groupId: string,
    agendaItemId: string,
    body: import('./types').UpsertMinuteRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AgendaItemResponse>(
      `/api/groups/${groupId}/agenda/${agendaItemId}/minutes`,
      { method: 'PUT', body, token, memberId },
    ),

  getGroupDecisions: (
    groupId: string,
    token: string,
    memberId: string,
    filter?: import('./types').GroupDecisionFilter,
  ) => {
    const query = filter && filter !== 'All' ? `?filter=${filter}` : '';
    return apiRequest<import('./types').GroupDecisionResponse[]>(
      `/api/groups/${groupId}/group-decisions${query}`,
      { token, memberId },
    );
  },

  getResolutions: (groupId: string, token: string, memberId: string, status?: string) => {
    const query = status ? `?status=${status}` : '';
    return apiRequest<import('./types').ResolutionResponse[]>(
      `/api/groups/${groupId}/resolutions${query}`,
      { token, memberId },
    );
  },

  createResolution: (
    groupId: string,
    body: import('./types').CreateResolutionRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').ResolutionResponse>(`/api/groups/${groupId}/resolutions`, {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  updateResolution: (
    groupId: string,
    id: string,
    body: import('./types').UpdateResolutionRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').ResolutionResponse>(`/api/groups/${groupId}/resolutions/${id}`, {
      method: 'PUT',
      body,
      token,
      memberId,
    }),

  getNotifications: (token: string, skip = 0, take = 50) =>
    apiRequest<import('./types').NotificationResponse[]>(
      `/api/notifications?skip=${skip}&take=${take}`,
      { token },
    ),

  getUnreadNotificationCount: (token: string) =>
    apiRequest<import('./types').UnreadNotificationCountResponse>(
      '/api/notifications/unread-count',
      { token },
    ),

  markNotificationRead: (id: string, token: string) =>
    apiRequest<import('./types').NotificationResponse>(`/api/notifications/${id}/read`, {
      method: 'PATCH',
      token,
    }),

  markAllNotificationsRead: (token: string) =>
    apiRequest<void>('/api/notifications/read-all', {
      method: 'PATCH',
      token,
    }),

  getAssets: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').AssetResponse[]>(`/api/groups/${groupId}/assets`, {
      token,
      memberId,
    }),

  getAsset: (groupId: string, assetId: string, token: string, memberId: string) =>
    apiRequest<import('./types').AssetResponse>(
      `/api/groups/${groupId}/assets/${assetId}`,
      { token, memberId },
    ),

  getAssetMaintenanceSummary: (groupId: string, token: string, memberId: string) =>
    apiRequest<import('./types').AssetMaintenanceSummaryResponse>(
      `/api/groups/${groupId}/assets/maintenance-summary`,
      { token, memberId },
    ),

  createAsset: (
    body: import('./types').CreateAssetRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AssetResponse>('/api/assets', {
      method: 'POST',
      body,
      token,
      memberId,
    }),

  updateAsset: (
    assetId: string,
    body: import('./types').UpdateAssetRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').AssetResponse>(`/api/assets/${assetId}`, {
      method: 'PUT',
      body,
      token,
      memberId,
    }),

  decommissionAsset: (assetId: string, token: string, memberId: string) =>
    apiRequest<import('./types').AssetResponse>(`/api/assets/${assetId}`, {
      method: 'DELETE',
      token,
      memberId,
    }),

  getMaintenanceRecords: (
    groupId: string,
    assetId: string,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MaintenanceRecordResponse[]>(
      `/api/groups/${groupId}/assets/${assetId}/maintenance-records`,
      { token, memberId },
    ),

  createMaintenanceRecord: (
    body: import('./types').CreateMaintenanceRecordRequest,
    token: string,
    memberId: string,
  ) =>
    apiRequest<import('./types').MaintenanceRecordResponse>('/api/maintenance-records', {
      method: 'POST',
      body,
      token,
      memberId,
    }),
};
