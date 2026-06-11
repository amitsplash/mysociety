import { Ionicons } from '@expo/vector-icons';
import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import { AppIcon } from '../components/AppIcon';
import { GroupAvatar } from '../components/GroupAvatar';
import { Button } from '../components/Button';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList, MainTabParamList } from '../navigation/types';
import { colors, spacing, typography } from '../theme';
import { confirm } from '../utils/confirm';
import { formatCurrency, formatEnumLabel } from '../utils/format';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Group'>,
  NativeStackScreenProps<MainStackParamList>
>;

type HubLink = {
  id: string;
  title: string;
  subtitle: string;
  icon: keyof typeof Ionicons.glyphMap;
  tone: 'indigo' | 'teal' | 'emerald' | 'rose' | 'amber';
  route:
    | 'GroupSettings'
    | 'Members'
    | 'AddMember'
    | 'GroupFunds'
    | 'ContributionReport'
    | 'Ledger'
    | 'AddExpense'
    | 'AssetRegister';
  adminOnly?: boolean;
  fundType?: 'Maintenance' | 'Corpus';
};

export function GroupHubScreen({ navigation }: Props) {
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
    removeGroupFromSession,
  } = useAuth();
  const { showError, showSuccess } = useToast();
  const [deleting, setDeleting] = useState(false);

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

  const handleSignOut = async () => {
    const ok = await confirm('Are you sure you want to sign out?', {
      title: 'Sign out',
      confirmLabel: 'Sign out',
    });
    if (ok) await logout();
  };

  const handleDeleteGroup = async () => {
    if (!token || !activeMemberId || !activeGroupId || !group) return;
    const ok = await confirm(
      `Delete "${group.name}" permanently? All members, contributions, expenses, and ledger history will be removed.`,
      { title: 'Delete group', confirmLabel: 'Delete' },
    );
    if (!ok) return;

    setDeleting(true);
    try {
      await api.deleteGroup(activeGroupId, token, activeMemberId);
      await removeGroupFromSession(activeGroupId);
      showSuccess('Group deleted');
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to delete group');
    } finally {
      setDeleting(false);
    }
  };

  const links: HubLink[] = [
    {
      id: 'group-profile',
      title: 'Group profile',
      subtitle: 'Tagline, logo & appearance',
      icon: 'color-palette',
      tone: 'indigo',
      route: 'GroupSettings',
      adminOnly: true,
    },
    {
      id: 'members',
      title: 'Members',
      subtitle: 'Manage group roster',
      icon: 'people',
      tone: 'indigo',
      route: 'Members',
      adminOnly: true,
    },
    {
      id: 'add-member',
      title: 'Add member',
      subtitle: 'Invite someone to the group',
      icon: 'person-add',
      tone: 'indigo',
      route: 'AddMember',
      adminOnly: true,
    },
    {
      id: 'group-funds',
      title: 'Group funds',
      subtitle: 'Balance & group expenses',
      icon: 'wallet',
      tone: 'emerald',
      route: 'GroupFunds',
      adminOnly: true,
    },
    {
      id: 'contribution-report',
      title: 'Contribution report',
      subtitle: 'Generated vs pending by period',
      icon: 'grid',
      tone: 'teal',
      route: 'ContributionReport',
      adminOnly: true,
    },
    {
      id: 'fund-ledger',
      title: 'Fund ledger',
      subtitle: 'Maintenance & corpus cash flow',
      icon: 'book',
      tone: 'teal',
      route: 'Ledger',
      adminOnly: true,
      fundType: 'Maintenance',
    },
    {
      id: 'my-ledger',
      title: 'My ledger',
      subtitle: 'Your balance & history',
      icon: 'book',
      tone: 'teal',
      route: 'Ledger',
    },
    {
      id: 'member-expense',
      title: 'Member expense',
      subtitle: 'Reimbursement (admin approval)',
      icon: 'receipt',
      tone: 'indigo',
      route: 'AddExpense',
    },
    {
      id: 'asset-register',
      title: 'Asset register',
      subtitle: 'Equipment & preventive maintenance',
      icon: 'construct',
      tone: 'amber',
      route: 'AssetRegister',
    },
  ];

  if (!hasActiveGroup) {
    return (
      <Screen scroll={false}>
        <ScrollView contentContainerStyle={styles.scroll}>
          <Text style={styles.eyebrow}>Community hub</Text>
          <Text style={styles.name}>Hello, {user?.name?.split(' ')[0] ?? 'there'}</Text>
          <SurfaceCard variant="gradient">
            <Text style={styles.emptyTitle}>No group yet</Text>
            <Text style={styles.emptyBody}>
              Create your first group to start managing members, contributions, and expenses. Or
              activate your account with an invite from a group admin.
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
        <View style={styles.profileHeader}>
          <GroupAvatar
            name={activeMembership?.groupName ?? 'Your group'}
            logoUrl={group?.logoUrl}
            size={56}
          />
          <View style={styles.profileText}>
            <Text style={styles.eyebrow}>Community hub</Text>
            <Text style={styles.name}>{activeMembership?.groupName ?? 'Your group'}</Text>
            {group?.tagline ? <Text style={styles.tagline}>{group.tagline}</Text> : null}
            {group ? (
              <Text style={styles.meta}>
                {formatEnumLabel(group.type)} · {formatEnumLabel(group.contributionFrequency)} ·{' '}
                {formatCurrency(group.contributionAmount)}
              </Text>
            ) : null}
          </View>
        </View>

        <SurfaceCard variant="gradient">
          <View style={styles.roleRow}>
            <Text style={styles.cardLabel}>Your role</Text>
            {activeMembership ? <StatusBadge label={activeMembership.role} /> : null}
          </View>
          <Text style={styles.cardBody}>
            Manage members, funds, and expenses here. Use the Minutes tab for meetings and group
            decisions.
          </Text>
        </SurfaceCard>

        {(memberships ?? []).length > 1 ? (
          <View style={styles.section}>
            <SectionHeader title="Switch group" />
            {(memberships ?? []).map((m) => (
              <Button
                key={m.memberId}
                label={m.groupName}
                variant={m.memberId === activeMembership?.memberId ? 'primary' : 'secondary'}
                onPress={() => setActiveMembership(m.memberId, m.groupId)}
              />
            ))}
          </View>
        ) : null}

        <SectionHeader title="Group actions" />
        {links
          .filter((l) => {
            if (l.adminOnly && !isAdmin) return false;
            if (isAdmin && l.id === 'my-ledger') return false;
            if (!isAdmin && l.id === 'fund-ledger') return false;
            return true;
          })
          .map((link) => (
            <Pressable
              key={link.id}
              onPress={() => {
                if (link.route === 'Ledger') {
                  navigation.navigate('Ledger', { fundType: link.fundType });
                  return;
                }
                navigation.navigate(
                  link.route as Exclude<typeof link.route, 'Ledger'>,
                );
              }}
              style={({ pressed }) => [styles.linkRow, pressed && styles.linkPressed]}>
              <AppIcon name={link.icon} tone={link.tone} />
              <View style={styles.linkText}>
                <Text style={styles.linkTitle}>{link.title}</Text>
                <Text style={styles.linkSub}>{link.subtitle}</Text>
              </View>
              <Ionicons name="chevron-forward" size={18} color={colors.textLight} />
            </Pressable>
          ))}

        {isAdmin ? (
          <View style={styles.dangerZone}>
            <Text style={styles.dangerTitle}>Danger zone</Text>
            <Text style={styles.dangerBody}>
              Permanently delete this group and all related data. This cannot be undone.
            </Text>
            <Button
              label="Delete group"
              variant="danger"
              onPress={() => void handleDeleteGroup()}
              loading={deleting}
            />
          </View>
        ) : null}

        <SurfaceCard variant="dashed" style={styles.announcement}>
          <View style={styles.announcementHeader}>
            <Ionicons name="megaphone-outline" size={16} color={colors.primary} />
            <Text style={styles.announcementTitle}>Group notice</Text>
          </View>
          <Text style={styles.announcementHeadline}>
            {group?.tagline ?? 'Contribution reminders'}
          </Text>
          <Text style={styles.announcementBody}>
            {group?.tagline
              ? `This is your space for ${activeMembership?.groupName ?? 'the group'}. Pending contributions are highlighted on the Payments tab.`
              : 'Pending contributions are highlighted on the Payments tab. Admins can generate monthly cycles from the same screen.'}
          </Text>
        </SurfaceCard>

        <Button label="Sign out" variant="danger" onPress={() => void handleSignOut()} />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  profileHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
    marginBottom: spacing.md,
  },
  profileText: { flex: 1 },
  eyebrow: { ...typography.section, color: colors.textMuted },
  name: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  tagline: { fontSize: 13, color: colors.textMuted, marginTop: 4, lineHeight: 18 },
  meta: { fontSize: 11, color: colors.primary, fontWeight: '600', marginTop: 4 },
  section: { marginBottom: spacing.md },
  roleRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  cardLabel: { fontSize: 11, fontWeight: '700', color: colors.textMuted, textTransform: 'uppercase' },
  cardBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
  linkRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  linkPressed: { opacity: 0.92, borderColor: colors.borderFocus },
  linkText: { flex: 1 },
  linkTitle: { fontSize: 13, fontWeight: '700', color: colors.text },
  linkSub: { fontSize: 10, color: colors.textMuted, marginTop: 2 },
  announcement: { marginTop: spacing.sm },
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
  dangerZone: { marginTop: spacing.md, marginBottom: spacing.sm },
  dangerTitle: { fontSize: 11, fontWeight: '800', color: colors.danger, textTransform: 'uppercase' },
  dangerBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18, marginVertical: spacing.sm },
});
