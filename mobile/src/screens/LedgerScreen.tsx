import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useLayoutEffect, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { EmptyState } from '../components/EmptyState';
import { api } from '../api/client';
import type {
  FundBalanceDto,
  LedgerEntryDto,
  GroupFundType,
  FundLedgerLine,
  FundLedgerResponse,
} from '../api/types';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { useAuth, useSession } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatCurrency, formatDateShort } from '../utils/format';
import { filterFundLedgerByFund, prepareFundLedgerLines } from '../utils/fundLedger';

type Props = NativeStackScreenProps<MainStackParamList, 'Ledger'>;

type MemberLedgerData = {
  balance: number;
  entries: LedgerEntryDto[];
};

const FUND_TABS: { value: GroupFundType; label: string }[] = [
  { value: 'Maintenance', label: 'Maintenance' },
  { value: 'Corpus', label: 'Corpus' },
];

export function LedgerScreen({ navigation, route }: Props) {
  const { token, memberId, groupId } = useSession();
  const { isAdmin } = useAuth();
  const viewMemberId = route.params?.memberId ?? memberId;
  const viewMemberName = route.params?.memberName;
  const isGroupLedger = isAdmin && !route.params?.memberId;
  const fromMembers = Boolean(route.params?.memberId);
  const initialFund = route.params?.fundType ?? 'Maintenance';
  const [selectedFund, setSelectedFund] = useState<GroupFundType>(initialFund);

  useLayoutEffect(() => {
    if (route.params?.fundType) {
      setSelectedFund(route.params.fundType);
    }
  }, [route.params?.fundType]);

  useLayoutEffect(() => {
    navigation.setOptions({
      title: isGroupLedger
        ? 'Fund ledger'
        : viewMemberName
          ? `${viewMemberName}'s ledger`
          : 'My ledger',
    });
  }, [navigation, isGroupLedger, viewMemberName, selectedFund]);

  const { data, loading, refreshing, refresh } = useAsyncData<
    FundLedgerResponse | MemberLedgerData
  >(
    useCallback(async () => {
      if (isGroupLedger) {
        return api.getFundLedger(groupId, token, memberId);
      }
      const ledger = await api.getLedger(viewMemberId, token, memberId);
      return { balance: ledger.balance, entries: ledger.entries };
    }, [isGroupLedger, groupId, token, memberId, viewMemberId]),
    [isGroupLedger, groupId, token, memberId, viewMemberId],
    { errorMessage: isGroupLedger ? 'Failed to load group ledger' : 'Failed to load ledger' },
  );

  if (isGroupLedger) {
    const ledger = data as FundLedgerResponse | null;
    const allLines = prepareFundLedgerLines(ledger?.lines ?? []);
    const lines = filterFundLedgerByFund(allLines, selectedFund);
    const fundBalance =
      selectedFund === 'Corpus' ? ledger?.funds.corpus : ledger?.funds.maintenance;

    if (loading) {
      return (
        <Screen scroll={false}>
          <View style={styles.centered}>
            <ActivityIndicator size="large" color={colors.primary} />
          </View>
        </Screen>
      );
    }

    return (
      <Screen scroll={false}>
        <ScrollView
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={refresh}
              tintColor={colors.primary}
              colors={[colors.primary]}
            />
          }>
          <FundTabBar selected={selectedFund} onSelect={setSelectedFund} />
          <FundSummaryStrip fund={fundBalance} label={selectedFund} />
          {lines.length === 0 ? (
            <EmptyState
              title="No transactions"
              message={
                selectedFund === 'Maintenance'
                  ? 'Contribution payments and maintenance expenses will appear here.'
                  : 'Corpus collections and corpus expenses will appear here.'
              }
            />
          ) : (
            <ScrollView horizontal showsHorizontalScrollIndicator>
              <View style={styles.table}>
                <LedgerTableHeader />
                {lines.map((line) => (
                  <LedgerTableRow key={line.id} line={line} />
                ))}
                <LedgerTableTotals fund={fundBalance} />
              </View>
            </ScrollView>
          )}
        </ScrollView>
      </Screen>
    );
  }

  const memberData = data as MemberLedgerData | null;
  const balance = memberData?.balance ?? 0;
  const entries = memberData?.entries ?? [];

  const ListHeader = (
    <View style={styles.detailHeader}>
      <BalanceStrip amount={balance} />
      {fromMembers ? (
        <Text style={styles.hint}>Member balances are listed on the Members screen.</Text>
      ) : null}
      {entries.length > 0 ? (
        <Text style={styles.sectionLabel}>
          {entries.length} transaction{entries.length === 1 ? '' : 's'}
        </Text>
      ) : null}
    </View>
  );

  return (
    <Screen scroll={false}>
      <ListScreen<LedgerEntryDto>
        data={entries}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No ledger entries"
        emptyMessage="Entries appear when contributions, payments, or expenses are recorded."
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => <LedgerEntryRow item={item} />}
      />
    </Screen>
  );
}

function FundTabBar({
  selected,
  onSelect,
}: {
  selected: GroupFundType;
  onSelect: (fund: GroupFundType) => void;
}) {
  return (
    <View style={styles.tabRow}>
      {FUND_TABS.map((tab) => {
        const active = tab.value === selected;
        return (
          <Pressable
            key={tab.value}
            onPress={() => onSelect(tab.value)}
            style={[styles.tab, active && styles.tabActive]}>
            <Text style={[styles.tabText, active && styles.tabTextActive]}>{tab.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

function FundSummaryStrip({
  fund,
  label,
}: {
  fund?: FundBalanceDto;
  label: GroupFundType;
}) {
  return (
    <View style={styles.summary}>
      <View style={styles.summaryItem}>
        <Text style={styles.summaryLabel}>{label} fund balance</Text>
        <Text
          style={[
            styles.summaryValue,
            (fund?.balance ?? 0) >= 0 ? styles.amountCredit : styles.amountDebit,
          ]}>
          {formatCurrency(fund?.balance ?? 0)}
        </Text>
      </View>
      <View style={styles.summaryRow}>
        <Text style={styles.summaryMeta}>
          In <Text style={styles.summaryMetaBold}>{formatCurrency(fund?.totalInflows ?? 0)}</Text>
        </Text>
        <Text style={styles.summaryDot}>·</Text>
        <Text style={styles.summaryMeta}>
          Out <Text style={styles.summaryMetaBold}>{formatCurrency(fund?.totalOutflows ?? 0)}</Text>
        </Text>
      </View>
    </View>
  );
}

function LedgerTableHeader() {
  return (
    <View style={[styles.tableRow, styles.tableHeaderRow]}>
      <Text style={[styles.cellDate, styles.headerCell]}>Date</Text>
      <Text style={[styles.cellParticulars, styles.headerCell]}>Particulars</Text>
      <Text style={[styles.cellAmount, styles.headerCell, styles.cellIn]}>In (₹)</Text>
      <Text style={[styles.cellAmount, styles.headerCell, styles.cellOut]}>Out (₹)</Text>
      <Text style={[styles.cellAmount, styles.headerCell, styles.cellBalance]}>Balance (₹)</Text>
    </View>
  );
}

function LedgerTableRow({ line }: { line: FundLedgerLine }) {
  return (
    <View style={styles.tableRow}>
      <Text style={styles.cellDate}>{formatDateShort(line.transactionDate)}</Text>
      <Text style={styles.cellParticulars} numberOfLines={2}>
        {line.description}
      </Text>
      <Text style={[styles.cellAmount, styles.cellIn, line.inflow > 0 && styles.amountCredit]}>
        {formatLedgerAmount(line.inflow)}
      </Text>
      <Text style={[styles.cellAmount, styles.cellOut, line.outflow > 0 && styles.amountDebit]}>
        {formatLedgerAmount(line.outflow)}
      </Text>
      <Text
        style={[
          styles.cellAmount,
          styles.cellBalance,
          line.runningBalance >= 0 ? styles.amountCredit : styles.amountDebit,
        ]}>
        {formatCurrency(line.runningBalance)}
      </Text>
    </View>
  );
}

function LedgerTableTotals({ fund }: { fund?: FundBalanceDto }) {
  return (
    <View style={[styles.tableRow, styles.tableFooterRow]}>
      <Text style={[styles.cellDate, styles.footerCell]}>Total</Text>
      <Text style={[styles.cellParticulars, styles.footerCell]} />
      <Text style={[styles.cellAmount, styles.cellIn, styles.footerCell, styles.amountCredit]}>
        {formatCurrency(fund?.totalInflows ?? 0)}
      </Text>
      <Text style={[styles.cellAmount, styles.cellOut, styles.footerCell, styles.amountDebit]}>
        {formatCurrency(fund?.totalOutflows ?? 0)}
      </Text>
      <Text
        style={[
          styles.cellAmount,
          styles.cellBalance,
          styles.footerCell,
          (fund?.balance ?? 0) >= 0 ? styles.amountCredit : styles.amountDebit,
        ]}>
        {formatCurrency(fund?.balance ?? 0)}
      </Text>
    </View>
  );
}

function formatLedgerAmount(amount: number): string {
  return amount > 0 ? formatCurrency(amount) : '—';
}

function BalanceStrip({ amount }: { amount: number }) {
  const isCredit = amount >= 0;
  return (
    <View style={[styles.balanceStrip, isCredit ? styles.stripCredit : styles.stripDebit]}>
      <Text style={styles.stripLabel}>{isCredit ? 'Credit balance' : 'Outstanding'}</Text>
      <Text
        style={[styles.stripAmount, isCredit ? styles.amountCredit : styles.amountDebit]}
        numberOfLines={1}>
        {formatCurrency(amount)}
      </Text>
    </View>
  );
}

function LedgerEntryRow({ item }: { item: LedgerEntryDto }) {
  const isCredit = item.direction === 'Credit';
  return (
    <View style={styles.entryRow}>
      <Text style={styles.entryTitle} numberOfLines={1}>
        {item.type.replace(/([A-Z])/g, ' $1').trim()}
      </Text>
      <Text style={styles.entryMeta} numberOfLines={1}>
        {formatDateShort(item.createdAt)}
      </Text>
      <Text
        style={[styles.entryAmount, isCredit ? styles.amountCredit : styles.amountDebit]}
        numberOfLines={1}>
        {isCredit ? '+' : '−'}
        {formatCurrency(item.amount)}
      </Text>
    </View>
  );
}

const TABLE_WIDTH = 560;

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  tabRow: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.sm,
  },
  tab: {
    flex: 1,
    paddingVertical: spacing.sm,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    alignItems: 'center',
  },
  tabActive: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
  },
  tabText: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
  },
  tabTextActive: {
    color: colors.primary,
  },
  listContent: {
    padding: spacing.md,
    paddingBottom: spacing.xl,
  },
  summary: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  summaryItem: { marginBottom: spacing.xs },
  summaryLabel: {
    fontSize: 11,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.3,
  },
  summaryValue: {
    fontSize: 22,
    fontWeight: '800',
    marginTop: 2,
  },
  summaryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  summaryMeta: { fontSize: 12, color: colors.textMuted },
  summaryMetaBold: { fontWeight: '700', color: colors.text },
  summaryDot: { color: colors.textLight },
  table: {
    width: TABLE_WIDTH,
    marginBottom: 2,
  },
  tableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.xs,
    backgroundColor: colors.surface,
  },
  tableHeaderRow: {
    backgroundColor: colors.surfaceMuted,
    borderTopLeftRadius: radii.md,
    borderTopRightRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border,
    borderBottomWidth: 1,
  },
  tableFooterRow: {
    backgroundColor: colors.surfaceMuted,
    borderWidth: 1,
    borderColor: colors.border,
    borderTopWidth: 2,
    borderTopColor: colors.borderSubtle,
    marginBottom: spacing.md,
  },
  headerCell: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.2,
  },
  footerCell: {
    fontWeight: '800',
    fontSize: 12,
    color: colors.text,
  },
  cellDate: {
    width: 72,
    fontSize: 11,
    color: colors.textMuted,
    paddingRight: 4,
  },
  cellParticulars: {
    width: 200,
    flexGrow: 1,
    fontSize: 12,
    color: colors.text,
    paddingRight: 8,
  },
  cellAmount: {
    width: 76,
    fontSize: 11,
    textAlign: 'right',
    color: colors.textMuted,
  },
  cellIn: {},
  cellOut: {},
  cellBalance: {
    width: 88,
    fontWeight: '600',
  },
  sectionLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    marginBottom: spacing.sm,
  },
  hint: {
    fontSize: 12,
    color: colors.textMuted,
    marginBottom: spacing.sm,
    lineHeight: 18,
  },
  detailHeader: {
    marginBottom: spacing.xs,
  },
  balanceStrip: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderRadius: radii.lg,
    borderWidth: 1,
    paddingVertical: spacing.sm + 2,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
    gap: spacing.sm,
  },
  stripCredit: {
    backgroundColor: colors.successMuted,
    borderColor: colors.successBorder,
  },
  stripDebit: {
    backgroundColor: colors.dangerMuted,
    borderColor: colors.dangerBorder,
  },
  stripLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
  },
  stripAmount: {
    fontSize: 18,
    fontWeight: '800',
  },
  entryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: spacing.sm + 4,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
  },
  entryTitle: {
    flex: 1,
    minWidth: 0,
    fontSize: 14,
    fontWeight: '700',
    color: colors.text,
  },
  entryMeta: {
    fontSize: 12,
    color: colors.textMuted,
  },
  entryAmount: {
    fontSize: 14,
    fontWeight: '700',
  },
  amountCredit: { color: colors.success },
  amountDebit: { color: colors.danger },
});
