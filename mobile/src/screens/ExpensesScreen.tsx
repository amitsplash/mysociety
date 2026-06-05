import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { ExpenseResponse } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList, MainTabParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';
import { formatCurrency, formatDateShort } from '../utils/format';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Expenses'>,
  NativeStackScreenProps<MainStackParamList>
>;

export function ExpensesScreen({ navigation }: Props) {
  const { token, activeMemberId, activeGroupId, isAdmin } = useAuth();
  const hasActiveGroup = Boolean(token && activeMemberId && activeGroupId);
  const memberId = activeMemberId ?? '';
  const groupId = activeGroupId ?? '';
  const { showSuccess, showError } = useToast();
  const [actionId, setActionId] = useState<string | null>(null);

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => {
      if (!hasActiveGroup || !token || !memberId || !groupId) {
        return Promise.resolve([]);
      }
      return api.getExpenses(groupId, token, memberId);
    }, [hasActiveGroup, groupId, token, memberId]),
    [hasActiveGroup, groupId, token, memberId],
    { errorMessage: 'Failed to load expenses', loadOnFocus: hasActiveGroup },
  );

  const expenses = data ?? [];

  const approve = async (id: string) => {
    if (!token || !memberId) {
      return;
    }
    setActionId(id);
    try {
      await api.approveExpense(id, token, memberId);
      showSuccess('Expense approved');
      await refresh();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Approve failed');
    } finally {
      setActionId(null);
    }
  };

  const reject = async (id: string) => {
    if (!token || !memberId) {
      return;
    }
    setActionId(id);
    try {
      await api.rejectExpense(id, token, memberId);
      showSuccess('Expense rejected');
      await refresh();
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Reject failed');
    } finally {
      setActionId(null);
    }
  };

  const ListHeader = (
    <View style={styles.header}>
      <Text style={styles.eyebrow}>Shared expenses</Text>
      <Text style={styles.title}>Expenses</Text>
      <Text style={styles.subtitle}>
        {isAdmin
          ? 'Review and approve pending submissions from members.'
          : 'Track submitted expenses and approval status.'}
      </Text>
      <Pressable style={styles.addFab} onPress={() => navigation.navigate('AddExpense')}>
        <Text style={styles.addFabText}>+ New expense</Text>
      </Pressable>
      <SectionHeader title="Expense feed" style={styles.feedHeader} />
    </View>
  );

  if (!hasActiveGroup) {
    return (
      <Screen scroll={false}>
        <View style={styles.header}>
          <Text style={styles.eyebrow}>Shared expenses</Text>
          <Text style={styles.title}>Expenses</Text>
          <SurfaceCard>
            <Text style={styles.emptyTitle}>No group selected</Text>
            <Text style={styles.emptyBody}>
              Create or join a group from Home to track shared expenses.
            </Text>
          </SurfaceCard>
        </View>
      </Screen>
    );
  }

  return (
    <Screen scroll={false}>
      <ListScreen<ExpenseResponse>
        data={expenses}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No expenses"
        emptyMessage="Submit an expense using the button above."
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardTop}>
              <Text style={styles.amount}>{formatCurrency(item.amount)}</Text>
              <StatusBadge label={item.status} />
            </View>
            <Text style={styles.desc}>{item.description}</Text>
            <Text style={styles.meta}>
              {formatDateShort(item.expenseDate)} · {item.createdByName}
            </Text>
            {isAdmin && item.status === 'Pending' ? (
              <View style={styles.actions}>
                <Button
                  label="Approve"
                  onPress={() => approve(item.id)}
                  loading={actionId === item.id}
                  style={styles.actionBtn}
                />
                <Button
                  label="Reject"
                  variant="danger"
                  onPress={() => reject(item.id)}
                  disabled={actionId === item.id}
                  style={styles.actionBtn}
                />
              </View>
            ) : null}
          </View>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: { marginBottom: spacing.sm },
  eyebrow: { ...typography.section, color: colors.textMuted },
  title: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  subtitle: { fontSize: 11, color: colors.textMuted, marginTop: 4, marginBottom: spacing.md },
  addFab: {
    alignSelf: 'flex-start',
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: radii.lg,
    marginBottom: spacing.md,
  },
  addFabText: { fontSize: 12, fontWeight: '700', color: colors.primary },
  feedHeader: { marginTop: 0 },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  cardTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  amount: { fontSize: 16, fontWeight: '900', color: colors.text },
  desc: { fontSize: 13, color: colors.text, marginTop: 8, fontWeight: '600' },
  meta: { fontSize: 10, color: colors.textMuted, marginTop: 4 },
  actions: { flexDirection: 'row', marginTop: 4 },
  actionBtn: { flex: 1, marginTop: 8, marginHorizontal: 4 },
  emptyTitle: { fontSize: 15, fontWeight: '800', color: colors.text, marginBottom: 6 },
  emptyBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
});
