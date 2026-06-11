import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useEffect, useMemo } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { GroupExpenseResponse, GroupFundsResponse, GroupIncomeResponse } from '../api/types';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatCurrency, formatDateShort, formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'GroupFunds'>;

type FundsData = {
  funds: GroupFundsResponse;
  expenses: GroupExpenseResponse[];
  incomes: GroupIncomeResponse[];
};

type FundTransaction =
  | { kind: 'income'; id: string; date: string; description: string; amount: number; badge?: string }
  | {
      kind: 'expense';
      id: string;
      date: string;
      description: string;
      amount: number;
      badge: string;
    };

function FundBalanceCard({
  label,
  balance,
  inflows,
  outflows,
  hint,
}: {
  label: string;
  balance: number;
  inflows: number;
  outflows: number;
  hint: string;
}) {
  return (
    <View style={styles.balanceCard}>
      <Text style={styles.balanceLabel}>{label}</Text>
      <Text
        style={[
          styles.balanceAmount,
          balance >= 0 ? styles.amountPositive : styles.amountNegative,
        ]}>
        {formatCurrency(balance)}
      </Text>
      <View style={styles.flowRow}>
        <Text style={styles.flowItem}>
          In <Text style={styles.flowValue}>{formatCurrency(inflows)}</Text>
        </Text>
        <Text style={styles.flowDot}>·</Text>
        <Text style={styles.flowItem}>
          Out <Text style={styles.flowValue}>{formatCurrency(outflows)}</Text>
        </Text>
      </View>
      <Text style={styles.balanceHint}>{hint}</Text>
    </View>
  );
}

export function GroupFundsScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { isAdmin } = useAuth();

  useEffect(() => {
    if (!isAdmin) {
      navigation.goBack();
    }
  }, [isAdmin, navigation]);

  const { data, loading, refreshing, refresh } = useAsyncData<FundsData>(
    useCallback(async () => {
      if (!isAdmin) {
        return null;
      }
      const [funds, expenses, incomes] = await Promise.all([
        api.getGroupFunds(groupId, token, memberId),
        api.getGroupExpenses(groupId, token, memberId),
        api.getGroupIncomes(groupId, token, memberId),
      ]);
      return { funds, expenses, incomes };
    }, [isAdmin, groupId, token, memberId]),
    [isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load group funds' },
  );

  const funds = data?.funds;
  const expenses = data?.expenses ?? [];
  const incomes = data?.incomes ?? [];

  const transactions = useMemo<FundTransaction[]>(() => {
    const incomeRows: FundTransaction[] = incomes.map((item) => ({
      kind: 'income',
      id: item.id,
      date: item.incomeDate,
      description: item.description,
      amount: item.amount,
      badge: 'Income',
    }));
    const expenseRows: FundTransaction[] = expenses.map((item) => ({
      kind: 'expense',
      id: item.id,
      date: item.expenseDate,
      description: item.description,
      amount: item.amount,
      badge: formatEnumLabel(item.fundType),
    }));
    return [...incomeRows, ...expenseRows].sort((a, b) => {
      const dateCompare = new Date(b.date).getTime() - new Date(a.date).getTime();
      if (dateCompare !== 0) return dateCompare;
      return a.kind === 'income' ? -1 : 1;
    });
  }, [expenses, incomes]);

  const ListHeader = (
    <View style={styles.header}>
      <FundBalanceCard
        label="Maintenance fund"
        balance={funds?.maintenance.balance ?? 0}
        inflows={funds?.maintenance.totalInflows ?? 0}
        outflows={funds?.maintenance.totalOutflows ?? 0}
        hint="Contributions and facility income minus maintenance expenses."
      />
      <FundBalanceCard
        label="Corpus fund"
        balance={funds?.corpus.balance ?? 0}
        inflows={funds?.corpus.totalInflows ?? 0}
        outflows={funds?.corpus.totalOutflows ?? 0}
        hint="Member corpus collections minus corpus expenses."
      />
      {isAdmin ? (
        <View style={styles.actionRow}>
          <Pressable
            style={({ pressed }) => [styles.addBtn, pressed && styles.addBtnPressed]}
            onPress={() => navigation.navigate('AddGroupIncome')}>
            <Text style={styles.addBtnText}>+ Record income</Text>
          </Pressable>
          <Pressable
            style={({ pressed }) => [styles.addBtn, pressed && styles.addBtnPressed]}
            onPress={() => navigation.navigate('AddGroupExpense')}>
            <Text style={styles.addBtnText}>+ Record expense</Text>
          </Pressable>
        </View>
      ) : null}
      <Text style={styles.sectionLabel}>
        {transactions.length} transaction{transactions.length === 1 ? '' : 's'}
      </Text>
    </View>
  );

  return (
    <Screen title="Group funds" subtitle="Maintenance & corpus pools" scroll={false}>
      <ListScreen<FundTransaction>
        data={transactions}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No transactions yet"
        emptyMessage={
          isAdmin
            ? 'Record maintenance income (e.g. clubhouse booking) or group expenses.'
            : 'Fund transactions will appear here once recorded by an admin.'
        }
        keyExtractor={(item) => `${item.kind}-${item.id}`}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => <FundTransactionRow item={item} />}
      />
    </Screen>
  );
}

function FundTransactionRow({ item }: { item: FundTransaction }) {
  const isIncome = item.kind === 'income';
  return (
    <View style={styles.row}>
      <View style={styles.rowMain}>
        <Text style={styles.rowTitle} numberOfLines={1}>
          {item.description}
        </Text>
        {item.badge ? <StatusBadge label={item.badge} compact /> : null}
      </View>
      <Text style={styles.rowMeta} numberOfLines={1}>
        {formatDateShort(item.date)}
      </Text>
      <Text
        style={[styles.rowAmount, isIncome ? styles.amountIncome : styles.amountExpense]}
        numberOfLines={1}>
        {isIncome ? '+' : '−'}
        {formatCurrency(item.amount)}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  header: { marginBottom: spacing.xs },
  balanceCard: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  balanceLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.4,
  },
  balanceAmount: {
    fontSize: 28,
    fontWeight: '800',
    letterSpacing: -0.5,
    marginTop: 4,
  },
  amountPositive: { color: colors.success },
  amountNegative: { color: colors.danger },
  flowRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: spacing.sm,
    gap: spacing.sm,
  },
  flowItem: { fontSize: 12, color: colors.textMuted },
  flowValue: { fontWeight: '700', color: colors.text },
  flowDot: { color: colors.textLight },
  balanceHint: {
    fontSize: 11,
    color: colors.textLight,
    marginTop: spacing.sm,
    lineHeight: 16,
  },
  actionRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
    marginBottom: spacing.sm,
  },
  addBtn: {
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    borderRadius: radii.md,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
  },
  addBtnPressed: { opacity: 0.9 },
  addBtnText: { fontSize: 13, fontWeight: '700', color: colors.primary },
  sectionLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    marginBottom: spacing.sm,
  },
  row: {
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
  rowMain: {
    flex: 1,
    minWidth: 0,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  rowTitle: {
    flexShrink: 1,
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
  },
  rowMeta: {
    fontSize: 12,
    color: colors.textMuted,
    flexShrink: 0,
  },
  rowAmount: {
    fontSize: 14,
    fontWeight: '700',
    flexShrink: 0,
  },
  amountIncome: { color: colors.success },
  amountExpense: { color: colors.danger },
});
