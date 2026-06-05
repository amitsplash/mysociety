import { Ionicons } from '@expo/vector-icons';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Platform, StyleSheet } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ContributionsScreen } from '../screens/ContributionsScreen';
import { DashboardScreen } from '../screens/DashboardScreen';
import { ExpensesScreen } from '../screens/ExpensesScreen';
import { GovernanceHubScreen } from '../screens/GovernanceHubScreen';
import { GroupHubScreen } from '../screens/GroupHubScreen';
import { colors } from '../theme';
import { MainTabParamList } from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

function tabIcon(name: keyof typeof Ionicons.glyphMap, focused: boolean) {
  return (
    <Ionicons
      name={name}
      size={22}
      color={focused ? colors.primary : colors.textLight}
    />
  );
}

export function MainTabNavigator() {
  const insets = useSafeAreaInsets();
  const bottomInset = Math.max(insets.bottom, Platform.OS === 'android' ? 12 : 8);

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        lazy: true,
        tabBarStyle: [
          styles.tabBar,
          {
            height: 56 + bottomInset,
            paddingBottom: bottomInset,
          },
        ],
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textLight,
        tabBarLabelStyle: styles.tabLabel,
      }}>
      <Tab.Screen
        name="Home"
        component={DashboardScreen}
        options={{
          tabBarLabel: 'Home',
          tabBarIcon: ({ focused }) => tabIcon(focused ? 'home' : 'home-outline', focused),
        }}
      />
      <Tab.Screen
        name="Payments"
        component={ContributionsScreen}
        options={{
          tabBarLabel: 'Payments',
          tabBarIcon: ({ focused }) => tabIcon(focused ? 'wallet' : 'wallet-outline', focused),
        }}
      />
      <Tab.Screen
        name="Expenses"
        component={ExpensesScreen}
        options={{
          tabBarLabel: 'Expenses',
          tabBarIcon: ({ focused }) =>
            tabIcon(focused ? 'receipt' : 'receipt-outline', focused),
        }}
      />
      <Tab.Screen
        name="Minutes"
        component={GovernanceHubScreen}
        options={{
          tabBarLabel: 'Minutes',
          tabBarIcon: ({ focused }) =>
            tabIcon(focused ? 'document-text' : 'document-text-outline', focused),
        }}
      />
      <Tab.Screen
        name="Group"
        component={GroupHubScreen}
        options={{
          tabBarLabel: 'Group',
          tabBarIcon: ({ focused }) => tabIcon(focused ? 'people' : 'people-outline', focused),
        }}
      />
    </Tab.Navigator>
  );
}

const styles = StyleSheet.create({
  tabBar: {
    backgroundColor: colors.tabBar,
    borderTopColor: colors.tabBarBorder,
    borderTopWidth: 1,
    paddingTop: 6,
  },
  tabLabel: {
    fontSize: 10,
    fontWeight: '600',
    marginTop: -2,
  },
});
