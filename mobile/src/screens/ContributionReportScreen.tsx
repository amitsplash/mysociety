import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Ionicons } from '@expo/vector-icons';
import { useCallback, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { ContributionResponse } from '../api/types';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';
import { formatContributionPeriodLabel } from '../utils/contributionMonthRange';
import {
  downloadContributionPeriodCsv,
} from '../utils/contributionReportExport';
import { formatCurrency } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'ContributionReport'>;

type ReportRow = {
  id: string;
  memberName: string;
  generated: number;
  paid: number;
  pending: number;
  statusLabel: 'Paid' | 'Pending' | 'Partial';
};

type PeriodBlock = {
  period: string;
  rows: ReportRow[];
  totalGenerated: number;
  totalPaid: number;
  totalPending: number;
};

function buildReportRows(items: ContributionResponse[]): PeriodBlock[] {
  const byPeriod = new Map<string, ReportRow[]>();

  for (const item of items) {
    const paid = item.paidAmount ?? 0;
    const pending = item.remainingAmount ?? Math.max(0, item.amount - paid);
    const statusLabel: ReportRow['statusLabel'] =
      item.status === 'Paid' ? 'Paid' : paid > 0 ? 'Partial' : 'Pending';

    const row: ReportRow = {
      id: item.id,
      memberName: item.memberName ?? 'Member',
      generated: item.amount,
      paid,
      pending,
      statusLabel,
    };

    const list = byPeriod.get(item.period) ?? [];
    list.push(row);
    byPeriod.set(item.period, list);
  }

  return [...byPeriod.entries()]
    .sort((a, b) => b[0].localeCompare(a[0]))
    .map(([period, rows]) => {
      const sorted = [...rows].sort((a, b) => a.memberName.localeCompare(b.memberName));
      return {
        period,
        rows: sorted,
        totalGenerated: sorted.reduce((sum, r) => sum + r.generated, 0),
        totalPaid: sorted.reduce((sum, r) => sum + r.paid, 0),
        totalPending: sorted.reduce((sum, r) => sum + r.pending, 0),
      };
    });
}

export function ContributionReportScreen({}: Props) {
  const { token, activeMemberId, activeGroupId, isAdmin, activeMembership } = useAuth();
  const { showSuccess, showError } = useToast();
  const [exportingPeriod, setExportingPeriod] = useState<string | null>(null);
  const hasActiveGroup = Boolean(token && activeMemberId && activeGroupId);
  const memberId = activeMemberId ?? '';
  const groupId = activeGroupId ?? '';

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !isAdmin || !token || !memberId || !groupId) {
        return Promise.resolve([]);
      }
      return api.getGroupContributions(groupId, token, memberId);
    }, [hasActiveGroup, isAdmin, groupId, token, memberId]),
    [hasActiveGroup, isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load contribution report', loadOnFocus: true },
  );

  const periods = useMemo(() => buildReportRows(data ?? []), [data]);

  const handleDownload = async (block: PeriodBlock) => {
    setExportingPeriod(block.period);
    try {
      await downloadContributionPeriodCsv(activeMembership?.groupName ?? 'Group', {
        period: block.period,
        rows: block.rows,
        totalGenerated: block.totalGenerated,
        totalPaid: block.totalPaid,
        totalPending: block.totalPending,
      });
      showSuccess(`CSV ready for ${formatContributionPeriodLabel(block.period)}`);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Download failed');
    } finally {
      setExportingPeriod(null);
    }
  };

  if (!isAdmin) {
    return (
      <Screen title="Contribution report" subtitle="Admin access required">
        <SurfaceCard>
          <Text style={styles.emptyBody}>Only group admins can view the contribution report.</Text>
        </SurfaceCard>
      </Screen>
    );
  }

  if (!hasActiveGroup) {
    return (
      <Screen title="Contribution report">
        <SurfaceCard variant="gradient">
          <Text style={styles.emptyTitle}>No group selected</Text>
          <Text style={styles.emptyBody}>Select a group to view generated contributions.</Text>
        </SurfaceCard>
      </Screen>
    );
  }

  return (
    <Screen scroll={false}>
      <ScrollView
        contentContainerStyle={styles.scroll}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={refresh} tintColor={colors.primary} />
        }>
        <Text style={styles.eyebrow}>Admin report</Text>
        <Text style={styles.title}>Contribution report</Text>
        <Text style={styles.subtitle}>
          Generated amounts and pending collections by period and member.
        </Text>

        {loading ? (
          <Text style={styles.loadingText}>Loading…</Text>
        ) : periods.length === 0 ? (
          <SurfaceCard variant="dashed">
            <Text style={styles.emptyTitle}>No contributions yet</Text>
            <Text style={styles.emptyBody}>
              Generate contributions from the Payments tab to see them here.
            </Text>
          </SurfaceCard>
        ) : (
          periods.map((block) => (
            <View key={block.period} style={styles.periodBlock}>
              <SectionHeader
                title={formatContributionPeriodLabel(block.period)}
                action={
                  <Pressable
                    onPress={() => void handleDownload(block)}
                    disabled={exportingPeriod === block.period}
                    style={({ pressed }) => [
                      styles.downloadBtn,
                      pressed && styles.downloadBtnPressed,
                    ]}
                    accessibilityLabel={`Download ${formatContributionPeriodLabel(block.period)} as CSV`}
                    accessibilityRole="button">
                    {exportingPeriod === block.period ? (
                      <ActivityIndicator size="small" color={colors.primary} />
                    ) : (
                      <Ionicons name="download-outline" size={16} color={colors.primary} />
                    )}
                    <Text style={styles.downloadBtnText}>CSV</Text>
                  </Pressable>
                }
              />
              <SurfaceCard style={styles.tableCard}>
                <ScrollView horizontal showsHorizontalScrollIndicator={false}>
                  <View>
                    <ReportTableHeader />
                    {block.rows.map((row) => (
                      <ReportTableRow key={row.id} row={row} />
                    ))}
                    <ReportTableTotals block={block} />
                  </View>
                </ScrollView>
              </SurfaceCard>
              <Pressable
                onPress={() => void handleDownload(block)}
                disabled={exportingPeriod === block.period}
                style={styles.downloadLink}>
                <Text style={styles.downloadLinkText}>
                  {exportingPeriod === block.period ? 'Preparing file…' : 'Download CSV for this period'}
                </Text>
              </Pressable>
            </View>
          ))
        )}
      </ScrollView>
    </Screen>
  );
}

function ReportTableHeader() {
  return (
    <View style={[styles.tableRow, styles.tableHeaderRow]}>
      <Text style={[styles.cellMember, styles.headerCell]}>Member</Text>
      <Text style={[styles.cellAmount, styles.headerCell]}>Generated</Text>
      <Text style={[styles.cellAmount, styles.headerCell]}>Paid</Text>
      <Text style={[styles.cellAmount, styles.headerCell]}>Pending</Text>
      <Text style={[styles.cellStatus, styles.headerCell]}>Status</Text>
    </View>
  );
}

function ReportTableRow({ row }: { row: ReportRow }) {
  return (
    <View style={styles.tableRow}>
      <Text style={styles.cellMember} numberOfLines={2}>
        {row.memberName}
      </Text>
      <Text style={styles.cellAmount}>{formatCurrency(row.generated)}</Text>
      <Text style={[styles.cellAmount, styles.amountPaid]}>{formatCurrency(row.paid)}</Text>
      <Text
        style={[
          styles.cellAmount,
          row.pending > 0 ? styles.amountPending : styles.amountPaid,
        ]}>
        {formatCurrency(row.pending)}
      </Text>
      <View style={styles.cellStatus}>
        <StatusBadge label={row.statusLabel} compact />
      </View>
    </View>
  );
}

function ReportTableTotals({ block }: { block: PeriodBlock }) {
  return (
    <View style={[styles.tableRow, styles.tableTotalsRow]}>
      <Text style={[styles.cellMember, styles.totalsLabel]}>Total</Text>
      <Text style={[styles.cellAmount, styles.totalsValue]}>{formatCurrency(block.totalGenerated)}</Text>
      <Text style={[styles.cellAmount, styles.totalsValue, styles.amountPaid]}>
        {formatCurrency(block.totalPaid)}
      </Text>
      <Text style={[styles.cellAmount, styles.totalsValue, styles.amountPending]}>
        {formatCurrency(block.totalPending)}
      </Text>
      <View style={styles.cellStatus} />
    </View>
  );
}

const styles = StyleSheet.create({
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  eyebrow: { ...typography.section, color: colors.textMuted },
  title: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  subtitle: { fontSize: 11, color: colors.textMuted, marginTop: 4, marginBottom: spacing.md, lineHeight: 16 },
  loadingText: { color: colors.textMuted, textAlign: 'center', paddingVertical: spacing.xl },
  periodBlock: { marginBottom: spacing.lg },
  downloadBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
    borderRadius: radii.sm,
    paddingHorizontal: 8,
    paddingVertical: 5,
    minWidth: 56,
    justifyContent: 'center',
  },
  downloadBtnPressed: { opacity: 0.85 },
  downloadBtnText: { fontSize: 10, fontWeight: '800', color: colors.primary },
  downloadLink: { marginTop: spacing.sm, alignSelf: 'flex-start' },
  downloadLinkText: { fontSize: 12, fontWeight: '700', color: colors.primary },
  tableCard: { padding: 0, overflow: 'hidden' },
  tableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 10,
    paddingHorizontal: spacing.sm,
    minWidth: 420,
  },
  tableHeaderRow: { backgroundColor: colors.surfaceMuted },
  tableTotalsRow: { backgroundColor: colors.primaryLight, borderBottomWidth: 0 },
  headerCell: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.3,
  },
  cellMember: { width: 120, fontSize: 12, fontWeight: '600', color: colors.text, paddingRight: 8 },
  cellAmount: { width: 72, fontSize: 11, fontWeight: '700', color: colors.text, textAlign: 'right' },
  cellStatus: { width: 72, alignItems: 'flex-start', paddingLeft: 4 },
  amountPaid: { color: colors.success },
  amountPending: { color: colors.danger },
  totalsLabel: { fontWeight: '800', fontSize: 12 },
  totalsValue: { fontWeight: '900' },
  emptyTitle: { fontSize: 15, fontWeight: '800', color: colors.text, marginBottom: 6 },
  emptyBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
});
