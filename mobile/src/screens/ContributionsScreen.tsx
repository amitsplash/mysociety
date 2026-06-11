import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useMemo, useState } from 'react';
import { Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type {
  ContributionResponse,
  MemberPendingContributions,
  PendingContributionItem,
  PendingPaymentSubmission,
} from '../api/types';
import { BottomSheet } from '../components/BottomSheet';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { MonthYearSelect } from '../components/MonthYearSelect';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
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
import { formatCurrency, formatDateShort, formatEnumLabel } from '../utils/format';
import { computePaymentAllocation, ADVANCE_CREDIT_PERIOD } from '../utils/paymentAllocation';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Payments'>,
  NativeStackScreenProps<MainStackParamList>
>;

type MemberPayTarget = {
  memberId: string;
  memberName?: string;
  totalOutstanding: number;
  items: PendingContributionItem[];
};

function toPendingItems(
  items: (ContributionResponse | PendingContributionItem)[],
): PendingContributionItem[] {
  return items.map((item) => {
    const paidAmount = item.paidAmount ?? 0;
    const remainingAmount =
      'remainingAmount' in item && item.remainingAmount != null
        ? item.remainingAmount
        : Math.max(0, item.amount - paidAmount);

    return {
      id: item.id,
      period: item.period,
      amount: item.amount,
      paidAmount,
      remainingAmount,
      internalRemark: 'internalRemark' in item ? item.internalRemark : null,
    };
  });
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
  const [payingMemberId, setPayingMemberId] = useState<string | null>(null);
  const [payTarget, setPayTarget] = useState<MemberPayTarget | null>(null);
  const [paymentAmount, setPaymentAmount] = useState('');
  const [expandedMemberId, setExpandedMemberId] = useState<string | null>(null);
  const [submissionActionId, setSubmissionActionId] = useState<string | null>(null);

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

  const { data: pendingPaymentSubmissions, refresh: refreshPaymentSubmissions } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !isAdmin || !token || !memberId || !groupId) {
        return Promise.resolve([]);
      }
      return api.getPendingPaymentSubmissions(groupId, token, memberId);
    }, [hasActiveGroup, isAdmin, groupId, token, memberId]),
    [hasActiveGroup, isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load payment approvals', loadOnFocus: hasActiveGroup && isAdmin },
  );

  const { data: myPendingPaymentSubmissions, refresh: refreshMyPaymentSubmissions } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || isAdmin || !token || !memberId) {
        return Promise.resolve([]);
      }
      return api.getMyPendingPaymentSubmissions(token, memberId);
    }, [hasActiveGroup, isAdmin, token, memberId]),
    [hasActiveGroup, isAdmin, token, memberId],
    { errorMessage: 'Failed to load submitted payments', loadOnFocus: hasActiveGroup && !isAdmin },
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
    await Promise.all([
      refresh(),
      refreshPending(),
      refreshPaymentSubmissions(),
      refreshMyPaymentSubmissions(),
    ]);
  }, [refresh, refreshPending, refreshPaymentSubmissions, refreshMyPaymentSubmissions]);

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

  const openPaySheet = (target: MemberPayTarget) => {
    setPayTarget(target);
    setPaymentAmount(String(target.totalOutstanding));
  };

  const toggleMemberDetails = (memberId: string) => {
    setExpandedMemberId((current) => (current === memberId ? null : memberId));
  };

  const paymentPreview = useMemo(() => {
    if (!payTarget) {
      return [];
    }
    const amount = Number(paymentAmount);
    if (!amount || amount <= 0) {
      return [];
    }
    return computePaymentAllocation(payTarget.items, amount);
  }, [payTarget, paymentAmount]);

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

    setPayingMemberId(payTarget.memberId);
    try {
      const result = await api.recordPayment(
        { memberId: payTarget.memberId, amount },
        token,
        memberId,
      );
      const awaitingApproval = result.status === 'PendingApproval';
      const advanceMsg =
        result.advanceAmount > 0
          ? ` · ${formatCurrency(result.advanceAmount)} for upcoming contributions`
          : '';

      if (awaitingApproval) {
        showSuccess(
          `Payment of ${formatCurrency(amount)} submitted for admin approval${advanceMsg}`,
        );
      } else if (isAdmin && payTarget.memberId !== memberId) {
        showSuccess(`Payment of ${formatCurrency(amount)} recorded${advanceMsg}`);
      } else {
        showSuccess(`Payment of ${formatCurrency(amount)} recorded${advanceMsg}`);
      }
      closePaySheet();
      await refreshAll();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Payment failed');
    } finally {
      setPayingMemberId(null);
    }
  };

  const approveSubmission = async (submissionId: string) => {
    if (!token || !memberId) {
      return;
    }
    setSubmissionActionId(submissionId);
    try {
      await api.approvePaymentSubmission(submissionId, token, memberId);
      showSuccess('Payment approved');
      await refreshAll();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Approve failed');
    } finally {
      setSubmissionActionId(null);
    }
  };

  const rejectSubmission = async (submissionId: string) => {
    if (!token || !memberId) {
      return;
    }
    setSubmissionActionId(submissionId);
    try {
      await api.rejectPaymentSubmission(submissionId, token, memberId);
      showSuccess('Payment rejected');
      await refreshAll();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Reject failed');
    } finally {
      setSubmissionActionId(null);
    }
  };

  const renderPaymentSubmissionCard = (submission: PendingPaymentSubmission, showActions: boolean) => (
    <SurfaceCard key={submission.submissionId} style={styles.submissionCard}>
      <View style={styles.submissionHeader}>
        <View style={styles.submissionHeaderLeft}>
          <Text style={styles.submissionMember}>{submission.memberName}</Text>
          <Text style={styles.submissionMeta}>
            {formatDateShort(submission.submittedAt)} · {formatCurrency(submission.totalAmount)}
          </Text>
        </View>
        <StatusBadge label="Pending approval" compact />
      </View>
      {submission.allocations.map((row) => (
        <Text key={row.paymentId} style={styles.submissionAllocation}>
          {row.period === ADVANCE_CREDIT_PERIOD
            ? row.period
            : formatContributionPeriodLabel(row.period)}{' '}
          · {formatCurrency(row.amountApplied)}
        </Text>
      ))}
      {showActions ? (
        <View style={styles.submissionActions}>
          <Button
            label="Approve"
            onPress={() => approveSubmission(submission.submissionId)}
            loading={submissionActionId === submission.submissionId}
            style={styles.submissionActionBtn}
          />
          <Button
            label="Reject"
            variant="danger"
            onPress={() => rejectSubmission(submission.submissionId)}
            disabled={submissionActionId === submission.submissionId}
            style={styles.submissionActionBtn}
          />
        </View>
      ) : null}
    </SurfaceCard>
  );

  const renderPendingItemDetails = (item: PendingContributionItem) => {
    const isPartial = item.paidAmount > 0;
    return (
      <View key={item.id} style={styles.pendingDetailRow}>
        <Text style={styles.pendingPeriod}>{formatContributionPeriodLabel(item.period)}</Text>
        <Text style={styles.pendingMeta}>
          {formatCurrency(item.amount)} billed
          {isPartial
            ? ` · ${formatCurrency(item.paidAmount)} paid · ${formatCurrency(item.remainingAmount)} left`
            : ` · ${formatCurrency(item.remainingAmount)} due`}
        </Text>
        {isPartial ? <StatusBadge label="Partial" compact /> : null}
        {item.internalRemark ? (
          <Text style={styles.internalRemark}>{item.internalRemark}</Text>
        ) : null}
      </View>
    );
  };

  const renderMemberPendingCard = (member: MemberPendingContributions) => {
    const isExpanded = expandedMemberId === member.memberId;
    return (
      <SurfaceCard key={member.memberId} style={styles.memberPendingCard}>
        <Pressable
          style={styles.memberPendingHeader}
          onPress={() => toggleMemberDetails(member.memberId)}>
          <View style={styles.memberPendingHeaderLeft}>
            <Text style={styles.memberPendingName}>{member.memberName}</Text>
            <Text style={styles.memberPendingCount}>
              {member.items.length} pending period{member.items.length === 1 ? '' : 's'}
            </Text>
          </View>
          <View style={styles.memberPendingHeaderRight}>
            <Text style={styles.memberPendingTotal}>
              {formatCurrency(member.totalOutstanding)}
            </Text>
            <Text style={styles.expandHint}>{isExpanded ? 'Hide' : 'Details'}</Text>
          </View>
        </Pressable>
        {isExpanded ? (
          <View style={styles.pendingDetails}>
            {member.items.map(renderPendingItemDetails)}
          </View>
        ) : null}
        <Pressable
          style={styles.memberPayBtn}
          onPress={() =>
            openPaySheet({
              memberId: member.memberId,
              memberName: member.memberName,
              totalOutstanding: member.totalOutstanding,
              items: member.items,
            })
          }
          disabled={payingMemberId === member.memberId}>
          <Text style={styles.memberPayBtnText}>
            {payingMemberId === member.memberId ? 'Recording…' : 'Record payment'}
          </Text>
        </Pressable>
      </SurfaceCard>
    );
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
          <SectionHeader title="Payments awaiting approval" />
          {(pendingPaymentSubmissions ?? []).length === 0 ? (
            <SurfaceCard variant="dashed">
              <Text style={styles.emptyPendingBody}>No member payments waiting for approval.</Text>
            </SurfaceCard>
          ) : (
            (pendingPaymentSubmissions ?? []).map((submission) =>
              renderPaymentSubmissionCard(submission, true),
            )
          )}
        </View>
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
            pendingSummary.members.map(renderMemberPendingCard)
          )}
        </View>
      ) : null}

      {!isAdmin && (myPendingPaymentSubmissions ?? []).length > 0 ? (
        <View style={styles.pendingSection}>
          <SectionHeader title="Awaiting admin approval" />
          {(myPendingPaymentSubmissions ?? []).map((submission) =>
            renderPaymentSubmissionCard(submission, false),
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
            (() => {
              const pendingItems = toPendingItems(myPendingItems);
              const totalOutstanding = pendingItems.reduce(
                (sum, item) => sum + item.remainingAmount,
                0,
              );
              const isExpanded = expandedMemberId === memberId;
              return (
                <SurfaceCard style={styles.memberPendingCard}>
                  <Pressable
                    style={styles.memberPendingHeader}
                    onPress={() => toggleMemberDetails(memberId)}>
                    <View style={styles.memberPendingHeaderLeft}>
                      <Text style={styles.memberPendingName}>Your pending balance</Text>
                      <Text style={styles.memberPendingCount}>
                        {pendingItems.length} pending period{pendingItems.length === 1 ? '' : 's'}
                      </Text>
                    </View>
                    <View style={styles.memberPendingHeaderRight}>
                      <Text style={styles.memberPendingTotal}>
                        {formatCurrency(totalOutstanding)}
                      </Text>
                      <Text style={styles.expandHint}>{isExpanded ? 'Hide' : 'Details'}</Text>
                    </View>
                  </Pressable>
                  {isExpanded ? (
                    <View style={styles.pendingDetails}>
                      {pendingItems.map(renderPendingItemDetails)}
                    </View>
                  ) : null}
                  <Pressable
                    style={styles.memberPayBtn}
                    onPress={() =>
                      openPaySheet({
                        memberId,
                        totalOutstanding,
                        items: pendingItems,
                      })
                    }
                    disabled={payingMemberId === memberId}>
                    <Text style={styles.memberPayBtnText}>
                      {payingMemberId === memberId ? 'Recording…' : 'Pay now'}
                    </Text>
                  </Pressable>
                </SurfaceCard>
              );
            })()
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
            <Text style={styles.sheetDueLabel}>Total pending</Text>
            <Text style={styles.sheetAmount}>{formatCurrency(payTarget.totalOutstanding)}</Text>
            <Text style={styles.sheetHint}>
              {isAdmin
                ? 'Pending amounts are cleared oldest first. Any extra is held as advance for upcoming contributions.'
                : 'Your payment will be submitted for admin approval before it is applied to pending amounts.'}
            </Text>
            <Input
              label="Amount received (₹)"
              value={paymentAmount}
              onChangeText={setPaymentAmount}
              keyboardType="decimal-pad"
              placeholder={String(payTarget.totalOutstanding)}
            />
            <Button
              label={`Receive full ${formatCurrency(payTarget.totalOutstanding)}`}
              variant="secondary"
              onPress={() => setPaymentAmount(String(payTarget.totalOutstanding))}
            />
            {paymentPreview.length > 0 ? (
              <View style={styles.allocationPreview}>
                <Text style={styles.allocationTitle}>Adjustment preview</Text>
                {paymentPreview.map((row) => (
                  <Text key={`${row.period}-${row.amountApplied}`} style={styles.allocationRow}>
                    {row.period === ADVANCE_CREDIT_PERIOD
                      ? row.period
                      : formatContributionPeriodLabel(row.period)}{' '}
                    · {formatCurrency(row.amountApplied)}
                    {row.isAdvance
                      ? ' · for upcoming contributions'
                      : row.remainingAfter > 0
                        ? ` · ${formatCurrency(row.remainingAfter)} left`
                        : ' · cleared'}
                  </Text>
                ))}
              </View>
            ) : null}
            <Button
              label={isAdmin ? 'Record payment' : 'Submit for approval'}
              onPress={pay}
              loading={payingMemberId === payTarget.memberId}
            />
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
    gap: spacing.sm,
  },
  memberPendingHeaderLeft: { flex: 1 },
  memberPendingHeaderRight: { alignItems: 'flex-end' },
  memberPendingName: { fontSize: 14, fontWeight: '800', color: colors.text },
  memberPendingCount: { fontSize: 10, color: colors.textMuted, marginTop: 2 },
  memberPendingTotal: { fontSize: 15, fontWeight: '800', color: colors.danger },
  expandHint: { fontSize: 10, color: colors.primary, fontWeight: '700', marginTop: 2 },
  pendingDetails: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    marginTop: spacing.sm,
    paddingTop: spacing.sm,
    gap: spacing.sm,
  },
  pendingDetailRow: { gap: 4 },
  pendingPeriod: { fontSize: 12, fontWeight: '700', color: colors.text },
  pendingMeta: { fontSize: 10, color: colors.textMuted, lineHeight: 14 },
  internalRemark: {
    fontSize: 10,
    color: colors.textMuted,
    fontStyle: 'italic',
    lineHeight: 14,
    marginTop: 2,
  },
  memberPayBtn: {
    marginTop: spacing.sm,
    backgroundColor: colors.primary,
    borderRadius: radii.sm,
    paddingVertical: 10,
    alignItems: 'center',
  },
  memberPayBtnText: { fontSize: 12, fontWeight: '800', color: '#fff' },
  submissionCard: { marginBottom: spacing.sm },
  submissionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: spacing.sm,
    marginBottom: spacing.xs,
  },
  submissionHeaderLeft: { flex: 1 },
  submissionMember: { fontSize: 13, fontWeight: '800', color: colors.text },
  submissionMeta: { fontSize: 10, color: colors.textMuted, marginTop: 2 },
  submissionAllocation: { fontSize: 10, color: colors.textMuted, lineHeight: 14 },
  submissionActions: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
  submissionActionBtn: { flex: 1 },
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
  sheetBody: { paddingBottom: spacing.md },
  allocationPreview: {
    backgroundColor: colors.primaryLight,
    borderRadius: radii.md,
    padding: spacing.sm,
    marginBottom: spacing.md,
    gap: 4,
  },
  allocationTitle: { fontSize: 11, fontWeight: '800', color: colors.text },
  allocationRow: { fontSize: 10, color: colors.textMuted, lineHeight: 14 },
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
