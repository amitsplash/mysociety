import { NavigatorScreenParams } from '@react-navigation/native';

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  ActivateAccount: { phone?: string; inviteCode?: string } | undefined;
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
  MainTabs: NavigatorScreenParams<MainTabParamList> | undefined;
  Notifications: undefined;
  CreateGroup: undefined;
  GroupSettings: undefined;
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
  AddGroupIncome: undefined;
  Ledger: { memberId?: string; memberName?: string; fundType?: 'Maintenance' | 'Corpus' } | undefined;
  ContributionReport: undefined;
  CommitteeMembers: undefined;
  OpenMatters: undefined;
  Meetings: undefined;
  AddMeeting: undefined;
  MeetingDetail: { meetingId: string };
  Resolutions: undefined;
  AssetRegister: undefined;
  AssetDetail: { assetId: string };
  AddEditAsset: { assetId?: string } | undefined;
  LogMaintenance: { assetId: string; assetName: string };
};
