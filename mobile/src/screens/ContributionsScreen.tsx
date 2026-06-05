import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useMemo, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { ContributionResponse, PendingContributionItem } from '../api/types';
import { BottomSheet } from '../components/BottomSheet';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { MonthYearSelect } from '../components/MonthYearSelect';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { SpendingChart } from '../components/SpendingChart';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList, MainTabParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';
import {
  buildPeriodKey,
  countInclusiveMonths,
  findOverlappingPeriod,
  formatContributionPeriodLabel,
  getCurrentMonth,
} from '../utils/contributionMonthRange';
import { confirm } from '../utils/confirm';
import { formatCurrency, formatEnumLabel } from '../utils/format';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Payments'>,
  NativeStackScreenProps<MainStackParamList>
>;

type PayTarget = {
  id: string;
  memberId: string;
  memberName?: string;
  period: string;
  amount: number;
  paidAmount: number;
  remainingAmount: number;
};

function toPayTarget(item: ContributionResponse | PendingContributionItem, memberId: string, memberName?: string): PayTarget {
  const paidAmount = item.paidAmount ?? 0;
  const remainingAmount =
    'remainingAmount' in item && item.remainingAmount != null
      ? item.remainingAmount
      : Math.max(0, item.amount - paidAmount);

  return {
    id: item.id,
    memberId,
    memberName,
    period: item.period,
    amount: item.amount,
    paidAmount,
    remainingAmount,
  };
}

export function ContributionsScreen({ navigation }: Props) {
  const { token, activeMemberId, activeGroupId, isAdmin } = useAuth();
  const hasActiveGroup = Boolean(token && activeMemberId && activeGroupId);
  const memberId = activeMemberId ?? '';
  const groupId = activeGroupId ?? '';
  const { showSuccess, showError } = useToast();
  const [generateSheetVisible, setGenerateSheetVisible] = useState(false);
  const [draftFromMonth, setDraftFromMonth] = useState(getCurrentMonth);
  const [draftToMonth, setDraftToMonth] = useState(getCurrentMonth);
  const [generating, setGenerating] = useState(false);
  const [payingId, setPayingId] = useState<string | null>(null);
  const [payTarget, setPayTarget] = useState<PayTarget | null>(null);
  const [paymentAmount, setPaymentAmount] = useState('');

  const { data: group } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !memberId || !groupId) {
        return Promise.resolve(null);
      }
      return api.getGroup(groupId, token, memberId);
    }, [hasActiveGroup, groupId, token, memberId]),
    [hasActiveGroup, groupId, token, memberId],
    { errorMessage: 'Failed to load group settings', loadOnFocus: hasActiveGroup },
  );

  const { data: pendingSummary, refresh: refreshPending } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !isAdmin || !token || !memberId || !groupId) {
        return Promise.resolve(null);
      }
      return api.getPendingContributionsSummary(groupId, token, memberId);
    }, [hasActiveGroup, isAdmin, groupId, token, memberId]),
    [hasActiveGroup, isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load pending collections', loadOnFocus: hasActiveGroup && isAdmin },
  );

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !memberId) {
        return Promise.resolve([]);
      }
      if (isAdmin && groupId) {
        return api.getGroupContributions(groupId, token, memberId);
      }
      return api.getContributions(memberId, token, memberId);
    }, [hasActiveGroup, isAdmin, groupId, token, memberId]),
    [hasActiveGroup, isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load contributions', loadOnFocus: hasActiveGroup },
  );

  const refreshAll = useCallback(async () => {
    await Promise.all([refresh(), refreshPending()]);
  }, [refresh, refreshPending]);

  const draftPeriodKey = useMemo(() => {
    if (!draftFromMonth || !draftToMonth) return '';
    return buildPeriodKey(draftFromMonth, draftToMonth);
  }, [draftFromMonth, draftToMonth]);
  const draftMonthCount = useMemo(() => {
    if (!draftFromMonth || !draftToMonth) return 0;
    return countInclusiveMonths(draftFromMonth, draftToMonth);
  }, [draftFromMonth, draftToMonth]);
  const generatedPeriods = useMemo(
    () => [...new Set((data ?? []).map((item) => item.period))],
    [data],
  );
  const overlappingPeriod = useMemo(() => {
    if (!draftFromMonth || !draftToMonth || draftMonthCount < 1) {
      return null;
    }
    return findOverlappingPeriod(draftFromMonth, draftToMonth, generatedPeriods);
  }, [draftFromMonth, draftToMonth, draftMonthCount, generatedPeriods]);
  const draftPreviewAmount = useMemo(() => {
    if (!group || draftMonthCount < 1) return 0;
    return group.contributionAmount * draftMonthCount;
  }, [group, draftMonthCount]);

  const items = data ?? [];

  const myPendingItems = useMemo(() => {
    if (isAdmin) {
      return [];
    }
    return items.filter((item) => {
      const paid = item.paidAmount ?? 0;
      const remaining = item.remainingAmount ?? Math.max(0, item.amount - paid);
      return item.status !== 'Paid' && remaining > 0;
    });
  }, [items, isAdmin]);

  const chartData = useMemo(() => {
    const byPeriod = new Map<string, number>();
    for (const item of items) {
      byPeriod.set(item.period, (byPeriod.get(item.period) ?? 0) + item.amount);
    }
    const sorted = [...byPeriod.entries()].sort((a, b) => a[0].localeCompare(b[0])).slice(-3);
    if (sorted.length === 0) {
      return { total: 0, bars: [{ label: '—', amount: 0 }] };
    }
    const bars = sorted.map(([label, amount]) => ({
      label: formatContributionPeriodLabel(label),
      amount,
    }));
    const total = sorted[sorted.length - 1]?.[1] ?? 0;
    return { total, bars };
  }, [items]);

  const openPaySheet = (target: PayTarget) => {
    setPayTarget(target);
    setPaymentAmount(String(target.remainingAmount));
  };

  const closePaySheet = () => {
    setPayTarget(null);
    setPaymentAmount('');
  };

  const openGenerateSheet = () => {
    const current = getCurrentMonth();
    setDraftFromMonth(current);
    setDraftToMonth(current);
    setGenerateSheetVisible(true);
  };

  const closeGenerateSheet = () => {
    setGenerateSheetVisible(false);
  };

  const handleDraftFromMonthChange = (month: string) => {
    setDraftFromMonth(month);
    if (draftToMonth && countInclusiveMonths(month, draftToMonth) < 1) {
      setDraftToMonth(month);
    }
  };

  const generate = async () => {
    if (!token || !memberId || !groupId || !group) {
      return;
    }
    if (!draftFromMonth || !draftToMonth) {
      showError('Select a month range');
      return;
    }
    if (draftMonthCount < 1) {
      showError('End month must be on or after start month');
      return;
    }
    if (overlappingPeriod) {
      showError(
        `The selected range overlaps with contributions already generated for ${formatContributionPeriodLabel(overlappingPeriod)}`,
      );
      return;
    }

    const amountHint =
      group.contributionModel === 'Fixed'
        ? `${formatCurrency(draftPreviewAmount)} per member`
        : 'amount varies by member square feet';

    const ok = await confirm(
      `Generate contributions for ${formatContributionPeriodLabel(draftPeriodKey)}?\n\n${amountHint}`,
      { title: 'Confirm generation', confirmLabel: 'Generate' },
    );
    if (!ok) {
      return;
    }

    setGenerating(true);
    try {
      const r = await api.generateContributions(
        { groupId, fromMonth: draftFromMonth, toMonth: draftToMonth },
        token,
        memberId,
      );
      showSuccess(
        `Generated ${r.createdCount} contribution(s) for ${formatContributionPeriodLabel(r.period)}`,
      );
      closeGenerateSheet();
      await refreshAll();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Generate failed');
    } finally {
      setGenerating(false);
    }
  };

  const pay = async () => {
    if (!token || !memberId || !payTarget) {
      return;
    }

    const amount = Number(paymentAmount);
    if (!amount || amount <= 0) {
      showError('Enter a valid amount');
      return;
    }
    if (amount > payTarget.remainingAmount) {
      showError(`Amount cannot exceed ${formatCurrency(payTarget.remainingAmount)}`);
      return;
    }

    setPayingId(payTarget.id);
    try {
      await api.recordPayment(
        { memberId: payTarget.memberId, amount, contributionId: payTarget.id },
        token,
        memberId,
      );
      const isFull = amount >= payTarget.remainingAmount;
      showSuccess(
        isAdmin && payTarget.memberId !== memberId
          ? isFull
            ? `Full payment recorded for ${payTarget.memberName ?? 'member'}`
            : `Partial payment of ${formatCurrency(amount)} recorded`
          : isFull
            ? 'Payment recorded'
            : `Partial payment of ${formatCurrency(amount)} recorded`,
      );
      closePaySheet();
      await refreshAll();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Payment failed');
    } finally {
      setPayingId(null);
    }
  };

  const ListHeader = (
    <View style={styles.header}>
      <Text style={styles.eyebrow}>Treasury & bills</Text>
      <Text style={styles.title}>Payments</Text>
      <Text style={styles.subtitle}>
        {isAdmin
          ? 'Generate contributions, record cash received, and open the full report.'
          : 'View and settle your pending contributions.'}
      </Text>

      <SpendingChart
        totalLabel="Contribution activity"
        totalAmount={chartData.total}
        bars={chartData.bars}
      />

      {isAdmin && group ? (
        <SurfaceCard>
          <SectionHeader title="Contributions" />
          <Text style={styles.periodHint}>
            Group rate: {formatCurrency(group.contributionAmount)} / month ·{' '}
            {formatEnumLabel(group.contributionFrequency)} (informational)
          </Text>
          <Button label="Generate contributions" onPress={openGenerateSheet} />
        </SurfaceCard>
      ) : null}

      {isAdmin ? (
        <View style={styles.pendingSection}>
          <SectionHeader title="Pending collections" />
          {pendingSummary ? (
            <Text style={styles.pendingSummaryMeta}>
              {pendingSummary.memberCount} member(s) ·{' '}
              {formatCurrency(pendingSummary.totalOutstanding)} outstanding
            </Text>
          ) : null}
          {!pendingSummary || pendingSummary.members.length === 0 ? (
            <SurfaceCard variant="dashed">
              <Text style={styles.emptyPendingTitle}>All caught up</Text>
              <Text style={styles.emptyPendingBody}>No pending or partially paid contributions.</Text>
            </SurfaceCard>
          ) : (
            pendingSummary.members.map((member) => (
              <SurfaceCard key={member.memberId} style={styles.memberPendingCard}>
                <View style={styles.memberPendingHeader}>
                  <Text style={styles.memberPendingName}>{member.memberName}</Text>
                  <Text style={styles.memberPendingTotal}>
                    {formatCurrency(member.totalOutstanding)} due
                  </Text>
                </View>
                {member.items.map((item) => {
                  const isPartial = item.paidAmount > 0;
                  return (
                    <View key={item.id} style={styles.pendingRow}>
                      <View style={styles.pendingRowLeft}>
                        <Text style={styles.pendingPeriod}>
                          {formatContributionPeriodLabel(item.period)}
                        </Text>
                        <Text style={styles.pendingMeta}>
                          {formatCurrency(item.amount)} billed
                          {isPartial
                            ? ` · ${formatCurrency(item.paidAmount)} paid · ${formatCurrency(item.remainingAmount)} left`
                            : ` · ${formatCurrency(item.remainingAmount)} due`}
                        </Text>
                        {isPartial ? <StatusBadge label="Partial" compact /> : null}
                      </View>
                      <Pressable
                        style={styles.payBtn}
                        onPress={() =>
                          openPaySheet(toPayTarget(item, member.memberId, member.memberName))
                        }
                        disabled={payingId === item.id}>
                        <Text style={styles.payBtnText}>
                          {payingId === item.id ? '…' : 'Record'}
                        </Text>
                      </Pressable>
                    </View>
                  );
                })}
              </SurfaceCard>
            ))
          )}
        </View>
      ) : null}

      {!isAdmin ? (
        <View style={styles.pendingSection}>
          <SectionHeader title="My pending contributions" />
          {myPendingItems.length === 0 ? (
            <SurfaceCard variant="dashed">
              <Text style={styles.emptyPendingTitle}>All caught up</Text>
              <Text style={styles.emptyPendingBody}>No pending contributions right now.</Text>
            </SurfaceCard>
          ) : (
            myPendingItems.map((item) => {
              const paid = item.paidAmount ?? 0;
              const remaining = item.remainingAmount ?? Math.max(0, item.amount - paid);
              const isPartial = paid > 0;
              return (
                <SurfaceCard key={item.id} style={styles.memberPendingCard}>
                  <View style={styles.pendingRow}>
                    <View style={styles.pendingRowLeft}>
                      <Text style={styles.pendingPeriod}>
                        {formatContributionPeriodLabel(item.period)}
                      </Text>
                      <Text style={styles.pendingMeta}>
                        {formatCurrency(item.amount)} billed
                        {isPartial
                          ? ` · ${formatCurrency(paid)} paid · ${formatCurrency(remaining)} left`
                          : ` · ${formatCurrency(remaining)} due`}
                      </Text>
                      {isPartial ? <StatusBadge label="Partial" compact /> : null}
                    </View>
                    <Pressable
                      style={styles.payBtn}
                      onPress={() => openPaySheet(toPayTarget(item, item.memberId))}
                      disabled={payingId === item.id}>
                      <Text style={styles.payBtnText}>
                        {payingId === item.id ? '…' : 'Pay now'}
                      </Text>
                    </Pressable>
                  </View>
                </SurfaceCard>
              );
            })
          )}
        </View>
      ) : null}

      {isAdmin ? (
        <Button
          label="Open contribution report"
          variant="secondary"
          onPress={() => navigation.navigate('ContributionReport')}
        />
      ) : null}
    </View>
  );

  if (!hasActiveGroup) {
    return (
      <Screen scroll={false}>
        <View style={styles.header}>
          <Text style={styles.eyebrow}>Treasury & bills</Text>
          <Text style={styles.title}>Payments</Text>
          <SurfaceCard variant="gradient">
            <Text style={styles.emptyTitle}>No group selected</Text>
            <Text style={styles.emptyBody}>
              Create or join a group from Home to view and manage payments.
            </Text>
          </SurfaceCard>
        </View>
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
            onRefresh={refreshAll}
            tintColor={colors.primary}
          />
        }>
        {ListHeader}
        {loading && !isAdmin ? (
          <Text style={styles.loadingText}>Loading…</Text>
        ) : null}
      </ScrollView>

      <BottomSheet
        visible={generateSheetVisible}
        title="Generate contributions"
        onClose={closeGenerateSheet}>
        {group ? (
          <ScrollView style={styles.generateSheetScroll} showsVerticalScrollIndicator={false}>
            <View style={styles.sheetBody}>
              <Text style={styles.periodHint}>
                Group rate: {formatCurrency(group.contributionAmount)} / month
              </Text>

              <MonthYearSelect
                label="From"
                value={draftFromMonth}
                onChange={handleDraftFromMonthChange}
              />

              <MonthYearSelect
                label="To"
                value={draftToMonth}
                onChange={setDraftToMonth}
              />

              {draftMonthCount > 0 ? (
                <Text style={styles.preview}>
                  {formatContributionPeriodLabel(draftPeriodKey)}
                  {group.contributionModel === 'Fixed'
                    ? ` · approx. ${formatCurrency(draftPreviewAmount)} per member`
                    : ' · amount varies by member square feet'}
                  {overlappingPeriod
                    ? ` · overlaps with ${formatContributionPeriodLabel(overlappingPeriod)}`
                    : ''}
                </Text>
              ) : null}

              <Button label="Generate" onPress={generate} loading={generating} />
              <Button label="Cancel" variant="ghost" onPress={closeGenerateSheet} />
            </View>
          </ScrollView>
        ) : null}
      </BottomSheet>

      <BottomSheet
        visible={Boolean(payTarget)}
        title={
          isAdmin && payTarget?.memberId !== memberId
            ? 'Record cash received'
            : 'Record payment'
        }
        onClose={closePaySheet}>
        {payTarget ? (
          <View style={styles.sheetBody}>
            {payTarget.memberName ? (
              <Text style={styles.sheetMember}>{payTarget.memberName}</Text>
            ) : null}
            <Text style={styles.sheetPeriod}>{formatContributionPeriodLabel(payTarget.period)}</Text>
            <Text style={styles.sheetDueLabel}>Remaining balance</Text>
            <Text style={styles.sheetAmount}>{formatCurrency(payTarget.remainingAmount)}</Text>
            {payTarget.paidAmount > 0 ? (
              <Text style={styles.sheetHint}>
                {formatCurrency(payTarget.paidAmount)} already received of {formatCurrency(payTarget.amount)}.
              </Text>
            ) : null}
            <Input
              label="Amount received (₹)"
              value={paymentAmount}
              onChangeText={setPaymentAmount}
              keyboardType="decimal-pad"
              placeholder={String(payTarget.remainingAmount)}
            />
            <Button
              label={`Receive full ${formatCurrency(payTarget.remainingAmount)}`}
              variant="secondary"
              onPress={() => setPaymentAmount(String(payTarget.remainingAmount))}
            />
            <Button label="Record payment" onPress={pay} loading={payingId === payTarget.id} />
            <Button label="Cancel" variant="ghost" onPress={closePaySheet} />
          </View>
        ) : null}
      </BottomSheet>
    </Screen>
  );
}

const styles = StyleSheet.create({
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  header: { marginBottom: spacing.sm },
  eyebrow: { ...typography.section, color: colors.textMuted },
  title: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  subtitle: { fontSize: 11, color: colors.textMuted, marginTop: 4, marginBottom: spacing.md },
  loadingText: { color: colors.textMuted, textAlign: 'center', paddingVertical: spacing.md },
  pendingSection: { marginTop: spacing.sm, marginBottom: spacing.sm },
  pendingSummaryMeta: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: -6,
    marginBottom: spacing.sm,
  },
  memberPendingCard: { marginBottom: spacing.sm },
  memberPendingHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  memberPendingName: { fontSize: 14, fontWeight: '800', color: colors.text, flex: 1 },
  memberPendingTotal: { fontSize: 13, fontWeight: '700', color: colors.danger },
  pendingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.sm,
    marginTop: spacing.sm,
    gap: spacing.sm,
  },
  pendingRowLeft: { flex: 1 },
  pendingPeriod: { fontSize: 12, fontWeight: '700', color: colors.text },
  pendingMeta: { fontSize: 10, color: colors.textMuted, marginTop: 4, lineHeight: 14 },
  emptyPendingTitle: { fontSize: 14, fontWeight: '700', color: colors.text },
  emptyPendingBody: { fontSize: 12, color: colors.textMuted, marginTop: 4 },
  periodHint: { fontSize: 12, color: colors.textMuted, lineHeight: 18, marginBottom: spacing.sm },
  generateSheetScroll: { maxHeight: 420 },
  preview: {
    fontSize: 13,
    color: colors.text,
    lineHeight: 18,
    marginBottom: spacing.md,
    backgroundColor: colors.primaryLight,
    borderRadius: radii.md,
    padding: spacing.sm,
  },
  payBtn: {
    backgroundColor: colors.primary,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: radii.sm,
    flexShrink: 0,
  },
  payBtnText: { fontSize: 10, fontWeight: '800', color: '#fff' },
  sheetBody: { paddingBottom: spacing.md },
  sheetMember: {
    fontSize: 14,
    fontWeight: '800',
    color: colors.text,
    textAlign: 'center',
    marginBottom: spacing.xs,
  },
  sheetPeriod: { fontSize: 12, color: colors.textMuted, textAlign: 'center' },
  sheetDueLabel: {
    fontSize: 10,
    color: colors.textMuted,
    textAlign: 'center',
    marginTop: spacing.sm,
    textTransform: 'uppercase',
    fontWeight: '700',
  },
  sheetAmount: {
    fontSize: 28,
    fontWeight: '900',
    color: colors.text,
    textAlign: 'center',
    marginVertical: spacing.xs,
  },
  sheetHint: {
    fontSize: 11,
    color: colors.textMuted,
    textAlign: 'center',
    marginBottom: spacing.md,
    lineHeight: 16,
  },
  emptyTitle: { fontSize: 15, fontWeight: '800', color: colors.text, marginBottom: 6 },
  emptyBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
});
