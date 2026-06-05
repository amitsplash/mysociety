import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useEffect } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { GroupFundsResponse, GroupExpenseResponse } from '../api/types';
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
      const [funds, expenses] = await Promise.all([
        api.getGroupFunds(groupId, token, memberId),
        api.getGroupExpenses(groupId, token, memberId),
      ]);
      return { funds, expenses };
    }, [isAdmin, groupId, token, memberId]),
    [isAdmin, groupId, token, memberId],
    { errorMessage: 'Failed to load group funds' },
  );

  const funds = data?.funds;
  const expenses = data?.expenses ?? [];

  const ListHeader = (
    <View style={styles.header}>
      <FundBalanceCard
        label="Maintenance fund"
        balance={funds?.maintenance.balance ?? 0}
        inflows={funds?.maintenance.totalInflows ?? 0}
        outflows={funds?.maintenance.totalOutflows ?? 0}
        hint="Contribution payments minus maintenance expenses."
      />
      <FundBalanceCard
        label="Corpus fund"
        balance={funds?.corpus.balance ?? 0}
        inflows={funds?.corpus.totalInflows ?? 0}
        outflows={funds?.corpus.totalOutflows ?? 0}
        hint="Member corpus collections minus corpus expenses."
      />
      {isAdmin ? (
        <Pressable
          style={({ pressed }) => [styles.addBtn, pressed && styles.addBtnPressed]}
          onPress={() => navigation.navigate('AddGroupExpense')}>
          <Text style={styles.addBtnText}>+ Record group expense</Text>
        </Pressable>
      ) : null}
      <Text style={styles.sectionLabel}>
        {expenses.length} group expense{expenses.length === 1 ? '' : 's'}
      </Text>
    </View>
  );

  return (
    <Screen title="Group funds" subtitle="Maintenance & corpus pools" scroll={false}>
      <ListScreen<GroupExpenseResponse>
        data={expenses}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No group expenses"
        emptyMessage={
          isAdmin
            ? 'Record expenses paid from maintenance or corpus funds.'
            : 'Group expenses will appear here once recorded by an admin.'
        }
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => <GroupExpenseRow item={item} />}
      />
    </Screen>
  );
}

function GroupExpenseRow({ item }: { item: GroupExpenseResponse }) {
  return (
    <View style={styles.row}>
      <View style={styles.rowMain}>
        <Text style={styles.rowTitle} numberOfLines={1}>
          {item.description}
        </Text>
        <StatusBadge label={formatEnumLabel(item.fundType)} compact />
      </View>
      <Text style={styles.rowMeta} numberOfLines={1}>
        {formatDateShort(item.expenseDate)}
      </Text>
      <Text style={styles.rowAmount} numberOfLines={1}>
        −{formatCurrency(item.amount)}
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
  addBtn: {
    alignSelf: 'flex-start',
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    borderRadius: radii.md,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
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
    color: colors.danger,
    flexShrink: 0,
  },
});
