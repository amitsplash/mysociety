import { Ionicons } from '@expo/vector-icons';
import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import {
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { api } from '../api/client';
import { Button } from '../components/Button';
import { GroupAvatar } from '../components/GroupAvatar';
import { QuickActionTile } from '../components/QuickActionTile';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList, MainTabParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';
import { confirm } from '../utils/confirm';
import { formatCurrency, formatEnumLabel } from '../utils/format';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Home'>,
  NativeStackScreenProps<MainStackParamList>
>;

export function DashboardScreen({ navigation }: Props) {
  const {
    user,
    token,
    activeMembership,
    memberships,
    activeMemberId,
    activeGroupId,
    setActiveMembership,
    logout,
    isAdmin,
  } = useAuth();

  const hasActiveGroup = Boolean(token && activeMemberId && activeGroupId);

  const { data: group, refresh, refreshing } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !activeMemberId || !activeGroupId) {
        return Promise.resolve(null);
      }
      return api.getGroup(activeGroupId, token, activeMemberId);
    }, [hasActiveGroup, activeGroupId, token, activeMemberId]),
    [hasActiveGroup, activeGroupId, token, activeMemberId],
    { errorMessage: 'Failed to load group', loadOnFocus: hasActiveGroup },
  );

  const { data: contributions } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !activeMemberId) {
        return Promise.resolve([]);
      }
      return api.getContributions(activeMemberId, token, activeMemberId);
    }, [hasActiveGroup, token, activeMemberId]),
    [hasActiveGroup, token, activeMemberId],
    { errorMessage: 'Failed to load contributions' },
  );

  const { data: expenses } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !activeMemberId || !activeGroupId) {
        return Promise.resolve([]);
      }
      return api.getExpenses(activeGroupId, token, activeMemberId);
    }, [hasActiveGroup, activeGroupId, token, activeMemberId]),
    [hasActiveGroup, activeGroupId, token, activeMemberId],
    { errorMessage: 'Failed to load expenses' },
  );

  const { data: unreadNotifications } = useAsyncData(
    useCallback(() => {
      if (!token) {
        return Promise.resolve({ count: 0 });
      }
      return api.getUnreadNotificationCount(token);
    }, [token]),
    [token],
    { errorMessage: 'Failed to load notifications', loadOnFocus: true },
  );

  const { data: groupFunds } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !isAdmin || !token || !activeMemberId || !activeGroupId) {
        return Promise.resolve(null);
      }
      return api.getGroupFunds(activeGroupId, token, activeMemberId);
    }, [hasActiveGroup, isAdmin, activeGroupId, token, activeMemberId]),
    [hasActiveGroup, isAdmin, activeGroupId, token, activeMemberId],
    { errorMessage: 'Failed to load group funds balance' },
  );

  const { data: assetMaintenanceSummary } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !isAdmin || !token || !activeMemberId || !activeGroupId) {
        return Promise.resolve(null);
      }
      return api.getAssetMaintenanceSummary(activeGroupId, token, activeMemberId);
    }, [hasActiveGroup, isAdmin, activeGroupId, token, activeMemberId]),
    [hasActiveGroup, isAdmin, activeGroupId, token, activeMemberId],
    { errorMessage: 'Failed to load asset maintenance summary' },
  );

  const handleSignOut = async () => {
    const ok = await confirm('Are you sure you want to sign out?', {
      title: 'Sign out',
      confirmLabel: 'Sign out',
    });
    if (ok) await logout();
  };

  const pendingCount =
    contributions?.filter((c) => c.status === 'Pending').length ?? 0;
  const pendingExpenseCount =
    expenses?.filter((e) => e.status === 'Pending').length ?? 0;
  const unreadNotificationCount = unreadNotifications?.count ?? 0;

  if (!hasActiveGroup) {
    return (
      <Screen scroll={false}>
        <ScrollView contentContainerStyle={styles.scroll} showsVerticalScrollIndicator={false}>
          <View style={styles.header}>
            <View>
              <Text style={styles.eyebrow}>Welcome back</Text>
              <Text style={styles.name}>Hello, {user?.name?.split(' ')[0] ?? 'there'}</Text>
            </View>
          </View>
          <SurfaceCard variant="gradient">
            <Text style={styles.emptyTitle}>Get started</Text>
            <Text style={styles.emptyBody}>
              Your account is ready. Create a group to get started, or activate your account with an
              invite from a group admin.
            </Text>
            <Button label="Create group" onPress={() => navigation.navigate('CreateGroup')} />
          </SurfaceCard>
          <Button label="Sign out" variant="danger" onPress={() => void handleSignOut()} />
        </ScrollView>
      </Screen>
    );
  }

  return (
    <Screen scroll={false}>
      <ScrollView
        contentContainerStyle={styles.scroll}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={refresh}
            tintColor={colors.primary}
          />
        }>
        <View style={styles.header}>
          <View style={styles.headerText}>
            <Text style={styles.eyebrow}>Welcome back</Text>
            <Text style={styles.name}>{user?.name ?? 'Member'}</Text>
            <View style={styles.locationRow}>
              <Ionicons name="location-outline" size={12} color={colors.primary} />
              <Text style={styles.location}>{activeMembership?.groupName}</Text>
            </View>
          </View>
          <View style={styles.headerActions}>
            <Pressable style={styles.bell} onPress={() => navigation.navigate('Notifications')}>
              <Ionicons name="notifications-outline" size={18} color={colors.textMuted} />
              {unreadNotificationCount > 0 ? <View style={styles.bellDot} /> : null}
            </Pressable>
            <Pressable
              style={styles.bell}
              onPress={() => void handleSignOut()}
              accessibilityLabel="Sign out">
              <Ionicons name="log-out-outline" size={18} color={colors.danger} />
            </Pressable>
          </View>
        </View>

        <SurfaceCard variant="gradient" style={styles.groupCard}>
          <View style={styles.groupHeader}>
            <GroupAvatar
              name={activeMembership?.groupName ?? 'Group'}
              logoUrl={group?.logoUrl}
              size={52}
            />
            <View style={styles.groupHeaderText}>
              <View style={styles.groupTop}>
                <Text style={styles.groupName}>{activeMembership?.groupName}</Text>
                {activeMembership ? <StatusBadge label={activeMembership.role} /> : null}
              </View>
              {group?.tagline ? (
                <Text style={styles.groupTagline}>{group.tagline}</Text>
              ) : null}
              {group ? (
                <Text style={styles.groupMeta}>
                  {formatEnumLabel(group.contributionFrequency)} ·{' '}
                  {formatCurrency(group.contributionAmount)} / period
                </Text>
              ) : null}
            </View>
          </View>
        </SurfaceCard>

        {(memberships ?? []).length > 1 ? (
          <View style={styles.section}>
            <SectionHeader title="Switch group" />
            <ScrollView horizontal showsHorizontalScrollIndicator={false}>
              {(memberships ?? []).map((m) => {
                const active = m.memberId === activeMembership?.memberId;
                return (
                  <Pressable
                    key={m.memberId}
                    onPress={() => setActiveMembership(m.memberId, m.groupId)}
                    style={[styles.chip, active && styles.chipActive]}>
                    <Text style={[styles.chipText, active && styles.chipTextActive]}>
                      {m.groupName}
                    </Text>
                  </Pressable>
                );
              })}
            </ScrollView>
          </View>
        ) : null}

        <SectionHeader title="Quick actions" />
        <View style={styles.quickGrid}>
          <QuickActionTile
            title="Pay dues"
            metric={pendingCount > 0 ? String(pendingCount) : '0'}
            metricTone={pendingCount > 0 ? 'warning' : 'success'}
            subtitle={pendingCount > 0 ? 'Pending contributions' : 'All caught up'}
            icon="wallet-outline"
            tone="emerald"
            onPress={() => navigation.navigate('Payments')}
          />
          {isAdmin ? (
            <QuickActionTile
              title="Group funds"
              metric={formatCurrency(groupFunds?.maintenance.balance ?? 0)}
              metricTone={(groupFunds?.maintenance.balance ?? 0) < 0 ? 'danger' : 'success'}
              subtitle={
                groupFunds
                  ? `Maint. · Corpus ${formatCurrency(groupFunds.corpus.balance)}`
                  : 'Available balance'
              }
              icon="business-outline"
              tone="indigo"
              onPress={() => navigation.navigate('GroupFunds')}
            />
          ) : null}
          <QuickActionTile
            title={isAdmin ? 'Fund ledger' : 'My ledger'}
            subtitle={
              isAdmin && groupFunds
                ? `Corpus ${formatCurrency(groupFunds.corpus.balance)}`
                : isAdmin
                  ? 'Group cash flow'
                  : 'Balances & history'
            }
            icon="book-outline"
            tone="teal"
            onPress={() =>
              navigation.navigate(
                'Ledger',
                isAdmin ? { fundType: 'Maintenance' } : undefined,
              )
            }
          />
          {isAdmin ? (
            <>
              <QuickActionTile
                title="Members"
                subtitle="View & add members"
                icon="people-outline"
                tone="rose"
                onPress={() => navigation.navigate('Members')}
              />
              <QuickActionTile
                title="Review expenses"
                metric={String(pendingExpenseCount)}
                metricTone={pendingExpenseCount > 0 ? 'warning' : 'default'}
                subtitle={
                  pendingExpenseCount > 0 ? 'Awaiting approval' : 'No pending items'
                }
                icon="receipt-outline"
                tone="amber"
                onPress={() => navigation.navigate('Expenses')}
              />
              <QuickActionTile
                title="Asset maintenance"
                metric={String(
                  (assetMaintenanceSummary?.dueSoonCount ?? 0) +
                    (assetMaintenanceSummary?.overdueCount ?? 0),
                )}
                metricTone={
                  (assetMaintenanceSummary?.overdueCount ?? 0) > 0
                    ? 'danger'
                    : (assetMaintenanceSummary?.dueSoonCount ?? 0) > 0
                      ? 'warning'
                      : 'success'
                }
                subtitle={
                  assetMaintenanceSummary
                    ? `${assetMaintenanceSummary.overdueCount} overdue · ${assetMaintenanceSummary.dueSoonCount} due soon`
                    : 'Preventive maintenance'
                }
                icon="construct-outline"
                tone="amber"
                onPress={() => navigation.navigate('AssetRegister')}
              />
            </>
          ) : (
            <QuickActionTile
              title="Group hub"
              subtitle="Settings & more"
              icon="people-outline"
              tone="rose"
              onPress={() => navigation.navigate('Group')}
            />
          )}
        </View>

        <SurfaceCard variant="dashed">
          <View style={styles.announcementHeader}>
            <Ionicons name="megaphone-outline" size={16} color={colors.primary} />
            <Text style={styles.announcementTitle}>Group announcement</Text>
          </View>
          <Text style={styles.announcementHeadline}>
            {group?.tagline ?? 'Stay on top of contributions'}
          </Text>
          <Text style={styles.announcementBody}>
            {group?.tagline
              ? `Welcome to ${activeMembership?.groupName ?? 'your group'}. Use the Payments tab to settle pending cycles.`
              : 'Use the Payments tab to settle pending cycles. Admins can generate new contribution periods for the whole group.'}
          </Text>
        </SurfaceCard>

        <Button label="Sign out" variant="danger" onPress={() => void handleSignOut()} />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.md,
  },
  headerText: { flex: 1 },
  eyebrow: { ...typography.section, color: colors.textMuted },
  name: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  locationRow: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 4 },
  location: { fontSize: 11, fontWeight: '600', color: colors.primary },
  headerActions: { flexDirection: 'row', gap: spacing.sm },
  bell: {
    width: 40,
    height: 40,
    borderRadius: 14,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
  },
  bellDot: {
    position: 'absolute',
    top: 8,
    right: 8,
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: colors.danger,
  },
  groupCard: { marginBottom: spacing.md },
  groupHeader: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.md },
  groupHeaderText: { flex: 1 },
  groupTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  groupName: { fontSize: 14, fontWeight: '800', color: colors.text, flex: 1 },
  groupTagline: { fontSize: 12, color: colors.textMuted, lineHeight: 17, marginBottom: 4 },
  groupMeta: { fontSize: 12, color: colors.textMuted },
  section: { marginBottom: spacing.md },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    marginRight: 8,
  },
  chipActive: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
  },
  chipText: { fontSize: 12, fontWeight: '600', color: colors.textMuted },
  chipTextActive: { color: colors.primary },
  quickGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  announcementHeader: { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 8 },
  announcementTitle: {
    fontSize: 11,
    fontWeight: '800',
    color: colors.text,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  announcementHeadline: { fontSize: 13, fontWeight: '700', color: colors.text },
  announcementBody: { fontSize: 11, color: colors.textMuted, marginTop: 6, lineHeight: 17 },
  emptyTitle: { fontSize: 15, fontWeight: '800', color: colors.text, marginBottom: 6 },
  emptyBody: { fontSize: 12, color: colors.textMuted, marginBottom: spacing.md, lineHeight: 18 },
});
