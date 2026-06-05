export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  ActivateAccount: undefined;
  ForgotPassword: undefined;
};

export type MainTabParamList = {
  Home: undefined;
  Payments: undefined;
  Expenses: undefined;
  Minutes: undefined;
  Group: undefined;
};

export type MainStackParamList = {
  MainTabs: undefined;
  CreateGroup: undefined;
  Members: undefined;
  AddMember: undefined;
  EditMember: {
    id: string;
    name: string;
    phone: string;
    role: 'Member' | 'Admin';
    squareFeet?: number | null;
  };
  AddExpense: undefined;
  GroupFunds: undefined;
  AddGroupExpense: undefined;
  Ledger: { memberId?: string; memberName?: string; fundType?: 'Maintenance' | 'Corpus' } | undefined;
  ContributionReport: undefined;
  CommitteeMembers: undefined;
  OpenMatters: undefined;
  Meetings: undefined;
  AddMeeting: undefined;
  MeetingDetail: { meetingId: string };
  Resolutions: undefined;
};
