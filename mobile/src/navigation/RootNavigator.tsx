import { NavigationContainer, Theme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import { AddExpenseScreen } from '../screens/AddExpenseScreen';
import { AddGroupExpenseScreen } from '../screens/AddGroupExpenseScreen';
import { AddGroupIncomeScreen } from '../screens/AddGroupIncomeScreen';
import { GroupFundsScreen } from '../screens/GroupFundsScreen';
import { AddMemberScreen } from '../screens/AddMemberScreen';
import { ContributionReportScreen } from '../screens/ContributionReportScreen';
import { CreateGroupScreen } from '../screens/CreateGroupScreen';
import { GroupSettingsScreen } from '../screens/GroupSettingsScreen';
import { EditMemberScreen } from '../screens/EditMemberScreen';
import { LedgerScreen } from '../screens/LedgerScreen';
import { ActivateAccountScreen } from '../screens/ActivateAccountScreen';
import { ForgotPasswordScreen } from '../screens/ForgotPasswordScreen';
import { LoginScreen } from '../screens/LoginScreen';
import { RegisterScreen } from '../screens/RegisterScreen';
import { MembersScreen } from '../screens/MembersScreen';
import { CommitteeMembersScreen } from '../screens/CommitteeMembersScreen';
import { OpenMattersScreen } from '../screens/OpenMattersScreen';
import { MeetingsScreen } from '../screens/MeetingsScreen';
import { AddMeetingScreen } from '../screens/AddMeetingScreen';
import { MeetingDetailScreen } from '../screens/MeetingDetailScreen';
import { ResolutionsScreen } from '../screens/ResolutionsScreen';
import { NotificationsScreen } from '../screens/NotificationsScreen';
import { AssetRegisterScreen } from '../screens/AssetRegisterScreen';
import { AssetDetailScreen } from '../screens/AssetDetailScreen';
import { AddEditAssetScreen } from '../screens/AddEditAssetScreen';
import { LogMaintenanceScreen } from '../screens/LogMaintenanceScreen';
import { colors } from '../theme';
import { MainTabNavigator } from './MainTabNavigator';
import { AuthStackParamList, MainStackParamList } from './types';

const AuthStack = createNativeStackNavigator<AuthStackParamList>();
const MainStack = createNativeStackNavigator<MainStackParamList>();

const navTheme: Theme = {
  dark: true,
  colors: {
    primary: colors.primary,
    background: colors.background,
    card: colors.surface,
    text: colors.text,
    border: colors.border,
    notification: colors.danger,
  },
  fonts: {
    regular: { fontFamily: 'System', fontWeight: '400' },
    medium: { fontFamily: 'System', fontWeight: '500' },
    bold: { fontFamily: 'System', fontWeight: '700' },
    heavy: { fontFamily: 'System', fontWeight: '800' },
  },
};

const stackScreenOptions = {
  headerStyle: { backgroundColor: colors.backgroundElevated },
  headerShadowVisible: false,
  headerTintColor: colors.text,
  headerTitleStyle: { fontWeight: '600' as const, fontSize: 17 },
  contentStyle: { backgroundColor: colors.background },
};

function AuthNavigator() {
  return (
    <AuthStack.Navigator screenOptions={stackScreenOptions}>
      <AuthStack.Screen name="Login" component={LoginScreen} options={{ title: 'Sign in' }} />
      <AuthStack.Screen name="Register" component={RegisterScreen} options={{ title: 'Register' }} />
      <AuthStack.Screen
        name="ActivateAccount"
        component={ActivateAccountScreen}
        options={{ title: 'Activate account' }}
      />
      <AuthStack.Screen
        name="ForgotPassword"
        component={ForgotPasswordScreen}
        options={{ title: 'Forgot password' }}
      />
    </AuthStack.Navigator>
  );
}

function MainNavigator() {
  return (
    <MainStack.Navigator screenOptions={stackScreenOptions}>
      <MainStack.Screen
        name="MainTabs"
        component={MainTabNavigator}
        options={{ headerShown: false }}
      />
      <MainStack.Screen
        name="Notifications"
        component={NotificationsScreen}
        options={{ title: 'Notifications' }}
      />
      <MainStack.Screen
        name="CreateGroup"
        component={CreateGroupScreen}
        options={{ title: 'Create group' }}
      />
      <MainStack.Screen
        name="GroupSettings"
        component={GroupSettingsScreen}
        options={{ title: 'Group profile' }}
      />
      <MainStack.Screen name="Members" component={MembersScreen} options={{ title: 'Members' }} />
      <MainStack.Screen
        name="AddMember"
        component={AddMemberScreen}
        options={{ title: 'Add member' }}
      />
      <MainStack.Screen
        name="EditMember"
        component={EditMemberScreen}
        options={{ title: 'Edit member' }}
      />
      <MainStack.Screen
        name="AddExpense"
        component={AddExpenseScreen}
        options={{ title: 'Member expense' }}
      />
      <MainStack.Screen
        name="GroupFunds"
        component={GroupFundsScreen}
        options={{ title: 'Group funds' }}
      />
      <MainStack.Screen
        name="AddGroupExpense"
        component={AddGroupExpenseScreen}
        options={{ title: 'Group expense' }}
      />
      <MainStack.Screen
        name="AddGroupIncome"
        component={AddGroupIncomeScreen}
        options={{ title: 'Record income' }}
      />
      <MainStack.Screen name="Ledger" component={LedgerScreen} options={{ title: 'Ledger' }} />
      <MainStack.Screen
        name="ContributionReport"
        component={ContributionReportScreen}
        options={{ title: 'Contribution report' }}
      />
      <MainStack.Screen
        name="CommitteeMembers"
        component={CommitteeMembersScreen}
        options={{ title: 'Committee members' }}
      />
      <MainStack.Screen
        name="OpenMatters"
        component={OpenMattersScreen}
        options={{ title: 'Open matters' }}
      />
      <MainStack.Screen name="Meetings" component={MeetingsScreen} options={{ title: 'Meeting minutes' }} />
      <MainStack.Screen name="AddMeeting" component={AddMeetingScreen} options={{ title: 'Record meeting' }} />
      <MainStack.Screen
        name="MeetingDetail"
        component={MeetingDetailScreen}
        options={{ title: 'Meeting detail' }}
      />
      <MainStack.Screen
        name="Resolutions"
        component={ResolutionsScreen}
        options={{ title: 'Group decisions' }}
      />
      <MainStack.Screen
        name="AssetRegister"
        component={AssetRegisterScreen}
        options={{ title: 'Asset register' }}
      />
      <MainStack.Screen
        name="AssetDetail"
        component={AssetDetailScreen}
        options={{ title: 'Asset detail' }}
      />
      <MainStack.Screen
        name="AddEditAsset"
        component={AddEditAssetScreen}
        options={{ title: 'Asset' }}
      />
      <MainStack.Screen
        name="LogMaintenance"
        component={LogMaintenanceScreen}
        options={{ title: 'Log maintenance' }}
      />
    </MainStack.Navigator>
  );
}

export function RootNavigator() {
  const { token, isLoading } = useAuth();

  if (isLoading) {
    return (
      <View
        style={{
          flex: 1,
          justifyContent: 'center',
          alignItems: 'center',
          backgroundColor: colors.background,
        }}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  const isSignedIn = Boolean(token);

  return (
    <NavigationContainer theme={navTheme}>
      {isSignedIn ? <MainNavigator /> : <AuthNavigator />}
    </NavigationContainer>
  );
}
